using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Laplace.EventBridge.Sdk
{
    /// <summary>
    ///     Client for connecting to the LAPLACE Event Bridge
    /// </summary>
    public class LaplaceEventBridgeClient : IDisposable
    {
        private readonly List<Action<LaplaceEvent>> _anyEventHandlers = [];
        private readonly List<Action<ConnectionState>> _connectionStateHandlers = [];

        private readonly Dictionary<string, List<Action<LaplaceEvent>>> _eventHandlers = [];
        private readonly ConnectionOptions _options;
        private string? _clientId;
        private CancellationTokenSource? _connectionCts;
        private ConnectionState _connectionState = ConnectionState.Disconnected;
        private DateTime? _lastPingTime;
        private Timer? _pingMonitorTimer;
        private int _reconnectAttempts;
        private Timer? _reconnectTimer;
        private string? _serverVersion;
        private ClientWebSocket? _webSocket;

        /// <summary>
        ///     Creates a new instance of the LAPLACE Event Bridge client
        /// </summary>
        /// <param name="options">Connection options</param>
        public LaplaceEventBridgeClient(ConnectionOptions? options = null)
        {
            _options = options ?? new ConnectionOptions();
        }

        public void Dispose()
        {
            DisconnectAsync().GetAwaiter().GetResult();
            _connectionCts?.Dispose();
            _reconnectTimer?.Dispose();
            _pingMonitorTimer?.Dispose();

            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Connect to the LAPLACE Event Bridge
        /// </summary>
        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            _webSocket?.Dispose();

            SetConnectionState(ConnectionState.Connecting);
            _connectionCts = new();

            try
            {
                var uri = new Uri(_options.Url);

                // Add token to query string if provided
                if (!string.IsNullOrEmpty(_options.Token))
                {
                    var uriBuilder = new UriBuilder(uri);
                    var query = uriBuilder.Query;
                    if (query.Length > 1) query = query[1..]; // Remove leading '?'

                    if (query.Length > 0)
                        query += "&";
                    query += $"token={Uri.EscapeDataString(_options.Token)}";

                    uriBuilder.Query = query;
                    uri = uriBuilder.Uri;
                }

                _webSocket = new();

                // Add authentication via subprotocol if token is provided
                if (!string.IsNullOrEmpty(_options.Token))
                {
                    _webSocket.Options.AddSubProtocol("laplace-event-bridge-role-client");
                    _webSocket.Options.AddSubProtocol(_options.Token);
                }

                await _webSocket.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
                SetConnectionState(ConnectionState.Connected);
                _reconnectAttempts = 0;

                // Start receiving messages
                _ = Task.Run(() => ReceiveLoopAsync(_connectionCts.Token), _connectionCts.Token);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"WebSocket connection error: {ex.Message}").ConfigureAwait(false);
                SetConnectionState(ConnectionState.Disconnected);

                if (_options.Reconnect && _reconnectAttempts < _options.MaxReconnectAttempts)
                    await ScheduleReconnectAsync().ConfigureAwait(false);
                else
                    throw;
            }
        }

        /// <summary>
        ///     Disconnect from the LAPLACE Event Bridge
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (_reconnectTimer != null) await _reconnectTimer.DisposeAsync().ConfigureAwait(false);

            _reconnectTimer = null;

            StopPingMonitoring();

            if (_connectionCts != null) await _connectionCts.CancelAsync().ConfigureAwait(false);

            if (_webSocket != null)
                try
                {
                    if (_webSocket.State == WebSocketState.Open)
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect",
                            CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                    // Ignore errors during close
                }
                finally
                {
                    _webSocket.Dispose();
                    _webSocket = null;
                }

            SetConnectionState(ConnectionState.Disconnected);
            _clientId = null;
            _serverVersion = null;
            _lastPingTime = null;
        }

        /// <summary>
        ///     Register an event handler for a specific event type
        /// </summary>
        public void On<T>(Action<T> handler) where T : LaplaceEvent
        {
            var eventType = GetEventTypeName<T>();
            if (!_eventHandlers.ContainsKey(eventType)) _eventHandlers[eventType] = [];
            _eventHandlers[eventType].Add(evt => handler((T)evt));
        }

        /// <summary>
        ///     Register a handler for all events
        /// </summary>
        public void OnAny(Action<LaplaceEvent> handler)
        {
            _anyEventHandlers.Add(handler);
        }

        /// <summary>
        ///     Register a handler for connection state changes
        /// </summary>
        public void OnConnectionStateChange(Action<ConnectionState> handler)
        {
            _connectionStateHandlers.Add(handler);
            // Immediately call with current state
            handler(_connectionState);
        }

        /// <summary>
        ///     Remove an event handler for a specific event type
        /// </summary>
        public void Off<T>(Action<T> handler) where T : LaplaceEvent
        {
            var eventType = GetEventTypeName<T>();
            if (!_eventHandlers.TryGetValue(eventType, out var eventHandler)) return;
            eventHandler.RemoveAll(h => h == (Action<LaplaceEvent>)(evt => handler((T)evt)));
            if (_eventHandlers[eventType].Count == 0) _eventHandlers.Remove(eventType);
        }

        /// <summary>
        ///     Remove a handler for all events
        /// </summary>
        public void OffAny(Action<LaplaceEvent> handler)
        {
            _anyEventHandlers.Remove(handler);
        }

        /// <summary>
        ///     Remove a connection state change handler
        /// </summary>
        public void OffConnectionStateChange(Action<ConnectionState> handler)
        {
            _connectionStateHandlers.Remove(handler);
        }

        /// <summary>
        ///     Get the current connection state
        /// </summary>
        public ConnectionState GetConnectionState()
        {
            return _connectionState;
        }

        /// <summary>
        ///     Get the client ID assigned by the bridge
        /// </summary>
        public string? GetClientId()
        {
            return _clientId;
        }

        /// <summary>
        ///     Send an event to the bridge
        /// </summary>
        public Task SendAsync(LaplaceEvent evt, CancellationToken cancellationToken = default)
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open)
                throw new InvalidOperationException("Not connected to LAPLACE Event Bridge");

            var json = evt switch
            {
                EstablishedEvent e => JsonSerializer.Serialize(e, LaplaceJsonContext.Default.EstablishedEvent),
                PingEvent e => JsonSerializer.Serialize(e, LaplaceJsonContext.Default.PingEvent),
                PongEvent e => JsonSerializer.Serialize(e, LaplaceJsonContext.Default.PongEvent),
                MessageEvent e => JsonSerializer.Serialize(e, LaplaceJsonContext.Default.MessageEvent),
                SystemEvent e => JsonSerializer.Serialize(e, LaplaceJsonContext.Default.SystemEvent),
                GiftEvent e => JsonSerializer.Serialize(e, LaplaceJsonContext.Default.GiftEvent),
                GenericEvent e => JsonSerializer.Serialize(e, LaplaceJsonContext.Default.GenericEvent),
                _ => JsonSerializer.Serialize(evt, LaplaceJsonContext.Default.LaplaceEvent),
            };

            var bytes = Encoding.UTF8.GetBytes(json);
            return _webSocket.SendAsync(new(bytes), WebSocketMessageType.Text, true, cancellationToken);
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[1024 * 16]; // 16KB buffer
            var messageBuffer = new List<byte>();

            try
            {
                while (_webSocket is { State: WebSocketState.Open } &&
                       !cancellationToken.IsCancellationRequested)
                {
                    messageBuffer.Clear();
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await _webSocket.ReceiveAsync(new(buffer), cancellationToken).ConfigureAwait(false);
                        messageBuffer.AddRange(buffer.Take(result.Count));
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var json = Encoding.UTF8.GetString(messageBuffer.ToArray());
                        await ProcessMessageAsync(json).ConfigureAwait(false);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("Disconnected from LAPLACE Event Bridge");
                        StopPingMonitoring();
                        _lastPingTime = null;

                        if (_options.Reconnect && _reconnectAttempts < _options.MaxReconnectAttempts)
                        {
                            _reconnectAttempts++;
                            SetConnectionState(ConnectionState.Reconnecting);
                            await ScheduleReconnectAsync().ConfigureAwait(false);
                        }
                        else
                        {
                            SetConnectionState(ConnectionState.Disconnected);
                        }

                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error in receive loop: {ex.Message}").ConfigureAwait(false);
                SetConnectionState(ConnectionState.Disconnected);
            }
        }

        private async Task ProcessMessageAsync(string json)
        {
            try
            {
                var jsonNode = JsonNode.Parse(json);
                if (jsonNode == null) return;

                var typeValue = jsonNode["type"]?.GetValue<string>();
                if (string.IsNullOrEmpty(typeValue)) return;

                LaplaceEvent? evt = null;

                switch (typeValue)
                {
                    case "ping":
                        _lastPingTime = DateTime.UtcNow;
                        // Respond with pong
                        var pong = new PongEvent
                        {
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            RespondingTo = jsonNode["timestamp"]?.GetValue<long>(),
                        };
                        await SendAsync(pong).ConfigureAwait(false);
                        return; // Don't process ping as regular event

                    case "established":
                        evt = JsonSerializer.Deserialize(json, LaplaceJsonContext.Default.EstablishedEvent);
                        if (evt is EstablishedEvent established)
                        {
                            _clientId = established.ClientId;
                            _serverVersion = established.Version;

                            // Mask token in URL for display
                            var displayUrl = _options.Url;
                            if (!string.IsNullOrEmpty(_options.Token) && displayUrl.Contains("token="))
                                displayUrl = Regex.Replace(displayUrl, @"token=[^&]+", "token=***");

                            Console.WriteLine(
                                $"Welcome to LAPLACE Event Bridge v{_serverVersion}: {displayUrl} with client ID {_clientId}");

                            // Start ping monitoring if server version supports it
                            if (ShouldMonitorPing()) StartPingMonitoring();
                        }

                        break;

                    case "message":
                        evt = JsonSerializer.Deserialize(json, LaplaceJsonContext.Default.MessageEvent);
                        break;

                    case "system":
                        evt = JsonSerializer.Deserialize(json, LaplaceJsonContext.Default.SystemEvent);
                        break;

                    case "gift":
                        evt = JsonSerializer.Deserialize(json, LaplaceJsonContext.Default.GiftEvent);
                        break;

                    default:
                        // Unknown event type, use generic event
                        var generic = JsonSerializer.Deserialize(json, LaplaceJsonContext.Default.GenericEvent);
                        if (generic != null)
                        {
                            generic.SetType(typeValue);
                            evt = generic;
                        }

                        break;
                }

                if (evt != null) ProcessEvent(evt);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Failed to parse event data: {ex.Message}").ConfigureAwait(false);
            }
        }

        private void ProcessEvent(LaplaceEvent evt)
        {
            // Call specific event handlers
            if (_eventHandlers.TryGetValue(evt.Type, out var handlers))
                foreach (var handler in handlers.ToList()) // ToList to avoid modification during iteration
                    try
                    {
                        handler(evt);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error in event handler for type {evt.Type}: {ex.Message}");
                    }

            // Call any event handlers
            foreach (var handler in _anyEventHandlers.ToList())
                try
                {
                    handler(evt);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error in any event handler: {ex.Message}");
                }
        }

        private void SetConnectionState(ConnectionState state)
        {
            if (_connectionState == state) return;
            _connectionState = state;
            foreach (var handler in _connectionStateHandlers.ToList())
                try
                {
                    handler(state);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error in connection state change handler: {ex.Message}");
                }
        }

        private async Task ScheduleReconnectAsync()
        {
            const double backoffMultiplier = 1.5;
            const int maxInterval = 60000; // 60 seconds

            var calculatedDelay = Math.Min(
                _options.ReconnectInterval * Math.Pow(backoffMultiplier, _reconnectAttempts - 1),
                maxInterval
            );
            var delay = (int)Math.Round(calculatedDelay);

            Console.WriteLine(
                $"Attempting to reconnect ({_reconnectAttempts}/{_options.MaxReconnectAttempts}) in {delay}ms...");

            if (_reconnectTimer != null)
                await _reconnectTimer.DisposeAsync().ConfigureAwait(false);
            _reconnectTimer = new(async void (_) =>
            {
                try
                {
                    await ConnectAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await Console.Error.WriteLineAsync($"Reconnection failed: {ex.Message}").ConfigureAwait(false);
                }
            }, null, delay, Timeout.Infinite);
        }

        private bool ShouldMonitorPing()
        {
            if (string.IsNullOrEmpty(_serverVersion)) return false;

            // Parse version and check if >= 4.0.2
            var parts = _serverVersion.Split('.');
            if (parts.Length >= 3 &&
                int.TryParse(parts[0], out var major) &&
                int.TryParse(parts[1], out var minor) &&
                int.TryParse(parts[2], out var patch))
                return major > 4 || (major == 4 && minor > 0) || (major == 4 && minor == 0 && patch >= 2);

            return false;
        }

        private void StartPingMonitoring()
        {
            StopPingMonitoring();

            _lastPingTime = DateTime.UtcNow;
            _pingMonitorTimer = new(_ =>
            {
                if (!_lastPingTime.HasValue) return;
                var elapsed = (DateTime.UtcNow - _lastPingTime.Value).TotalMilliseconds;
                if (!(elapsed > _options.PingTimeout)) return;
                Console.WriteLine($"No ping received for {elapsed}ms, connection may be dead");
                Task.Run(DisconnectAsync);
            }, null, _options.PingTimeout, _options.PingTimeout);
        }

        private void StopPingMonitoring()
        {
            _pingMonitorTimer?.Dispose();
            _pingMonitorTimer = null;
        }

        private static string GetEventTypeName<T>() where T : LaplaceEvent
        {
            // Map types to their event type names without using reflection
            var type = typeof(T);

            if (type == typeof(EstablishedEvent)) return "established";
            if (type == typeof(PingEvent)) return "ping";
            if (type == typeof(PongEvent)) return "pong";
            if (type == typeof(MessageEvent)) return "message";
            if (type == typeof(SystemEvent)) return "system";
            if (type == typeof(GiftEvent)) return "gift";
            return type == typeof(GenericEvent)
                ? "unknown"
                : throw
                    // Fallback for unknown types
                    new NotSupportedException($"Event type {type.Name} is not supported");
        }
    }
}