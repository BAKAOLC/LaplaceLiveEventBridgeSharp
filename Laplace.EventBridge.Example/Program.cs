using Laplace.EventBridge.Sdk;

Console.WriteLine("LAPLACE Event Bridge C# SDK Example");
Console.WriteLine("====================================");
Console.WriteLine();

// Parse command line arguments
var url = args.Length > 0 ? args[0] : "ws://localhost:9696";
var token = args.Length > 1 ? args[1] : "";

Console.WriteLine($"Connecting to: {url}");
if (!string.IsNullOrEmpty(token)) Console.WriteLine("Using authentication token");
Console.WriteLine();

// Create client with options
var client = new LaplaceEventBridgeClient(new()
{
    Url = url,
    Token = token,
    Reconnect = true,
    ReconnectInterval = 3000,
    MaxReconnectAttempts = 10,
});

// Handle connection state changes
client.OnConnectionStateChange(state => { Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Connection state: {state}"); });

// Handle message events
client.On<MessageEvent>(evt =>
{
    var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(evt.TimestampNormalized);
    Console.WriteLine($"[{timestamp:HH:mm:ss}] {evt.Username}: {evt.Message}");
});

// Handle system events
client.On<SystemEvent>(evt =>
{
    var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(evt.TimestampNormalized);
    Console.WriteLine($"[{timestamp:HH:mm:ss}] [SYSTEM] {evt.Message}");
});

// Handle gift events
client.On<GiftEvent>(evt =>
{
    var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(evt.TimestampNormalized);
    Console.WriteLine($"[{timestamp:HH:mm:ss}] {evt.Username} sent {evt.GiftCount}x {evt.GiftName}");
});

// Handle all events (optional)
client.OnAny(evt =>
{
    // You can log all events here for debugging
    // Console.WriteLine($"[DEBUG] Event type: {evt.Type}");
});

// Handle Ctrl+C for graceful shutdown
Console.CancelKeyPress += async (sender, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\nShutting down...");
    await client.DisconnectAsync().ConfigureAwait(false);
    Environment.Exit(0);
};

try
{
    // Connect to the bridge
    await client.ConnectAsync().ConfigureAwait(false);
    Console.WriteLine("Connected! Listening for events...");
    Console.WriteLine("Press Ctrl+C to exit");
    Console.WriteLine();

    // Keep the application running
    await Task.Delay(Timeout.Infinite).ConfigureAwait(false);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

return 0;