using System.Runtime.InteropServices;
using System.Text;
using Laplace.EventBridge.Sdk;

namespace Laplace.EventBridge.LuaWrapper
{
    /// <summary>
    ///     C exports for Lua FFI interop
    /// </summary>
    public static class LaplaceEventBridgeExports
    {
        private static readonly Dictionary<int, ClientWrapper> Clients = new();
        private static int _nextClientId = 1;
        private static readonly Lock Lock = new();

        #region Configuration

        /// <summary>
        ///     Set maximum queue size
        /// </summary>
        /// <param name="clientId">Client ID</param>
        /// <param name="maxSize">Maximum queue size</param>
        /// <returns>0 on success, negative on error</returns>
        [UnmanagedCallersOnly(EntryPoint = "leb_set_max_queue_size")]
        public static int SetMaxQueueSize(int clientId, int maxSize)
        {
            try
            {
                if (!Clients.TryGetValue(clientId, out var wrapper))
                    return -1;

                wrapper.EventQueue.MaxQueueSize = maxSize;
                return 0;
            }
            catch
            {
                return -2;
            }
        }

        #endregion

        #region Helpers

        private static unsafe void CopyString(IntPtr source, byte* dest, int destSize)
        {
            if (source == IntPtr.Zero || dest == null || destSize <= 0)
            {
                if (dest != null && destSize > 0)
                    *dest = 0;
                return;
            }

            var str = Marshal.PtrToStringUTF8(source) ?? "";
            var bytes = Encoding.UTF8.GetBytes(str);
            var copyLen = Math.Min(bytes.Length, destSize - 1);

            if (copyLen > 0)
                Marshal.Copy(bytes, 0, (IntPtr)dest, copyLen);

            dest[copyLen] = 0; // Null terminator
        }

        #endregion

        #region Client Management

        /// <summary>
        ///     Create a new client instance
        /// </summary>
        /// <param name="url">WebSocket URL (UTF-8 encoded C string)</param>
        /// <param name="token">Authentication token (UTF-8 encoded C string, can be null)</param>
        /// <returns>Client ID (positive integer) or negative on error</returns>
        [UnmanagedCallersOnly(EntryPoint = "leb_create_client")]
        public static unsafe int CreateClient(byte* url, byte* token)
        {
            try
            {
                var urlStr = Marshal.PtrToStringUTF8((IntPtr)url) ?? "ws://localhost:9696";
                var tokenStr = token != null ? Marshal.PtrToStringUTF8((IntPtr)token) : null;

                var options = new ConnectionOptions
                {
                    Url = urlStr,
                    Token = tokenStr,
                    Reconnect = true,
                    ReconnectInterval = 3000,
                    MaxReconnectAttempts = 10,
                };

                var wrapper = new ClientWrapper(options);

                lock (Lock)
                {
                    var clientId = _nextClientId++;
                    Clients[clientId] = wrapper;
                    return clientId;
                }
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        ///     Connect to the Event Bridge server (blocking)
        /// </summary>
        /// <param name="clientId">Client ID returned from leb_create_client</param>
        /// <returns>0 on success, negative on error</returns>
        [UnmanagedCallersOnly(EntryPoint = "leb_connect")]
        public static int Connect(int clientId)
        {
            try
            {
                if (!Clients.TryGetValue(clientId, out var wrapper))
                    return -1;

                wrapper.ConnectAsync().GetAwaiter().GetResult();
                return 0;
            }
            catch
            {
                return -2;
            }
        }

        /// <summary>
        ///     Start connecting to the Event Bridge server (non-blocking)
        /// </summary>
        /// <param name="clientId">Client ID returned from leb_create_client</param>
        /// <returns>0 on success, negative on error. Use leb_get_state to check connection status.</returns>
        [UnmanagedCallersOnly(EntryPoint = "leb_connect_async")]
        public static int ConnectAsync(int clientId)
        {
            try
            {
                if (!Clients.TryGetValue(clientId, out var wrapper))
                    return -1;

                // Fire and forget - don't wait for connection to complete
                _ = wrapper.ConnectAsync();
                return 0;
            }
            catch
            {
                return -2;
            }
        }

        /// <summary>
        ///     Disconnect from the Event Bridge server
        /// </summary>
        /// <param name="clientId">Client ID</param>
        /// <returns>0 on success, negative on error</returns>
        [UnmanagedCallersOnly(EntryPoint = "leb_disconnect")]
        public static int Disconnect(int clientId)
        {
            try
            {
                if (!Clients.TryGetValue(clientId, out var wrapper))
                    return -1;

                wrapper.DisconnectAsync().GetAwaiter().GetResult();
                return 0;
            }
            catch
            {
                return -2;
            }
        }

        /// <summary>
        ///     Destroy a client instance and free resources
        /// </summary>
        /// <param name="clientId">Client ID</param>
        /// <returns>0 on success, negative on error</returns>
        [UnmanagedCallersOnly(EntryPoint = "leb_destroy_client")]
        public static int DestroyClient(int clientId)
        {
            try
            {
                lock (Lock)
                {
                    if (!Clients.TryGetValue(clientId, out var wrapper))
                        return -1;

                    wrapper.Dispose();
                    Clients.Remove(clientId);
                    return 0;
                }
            }
            catch
            {
                return -2;
            }
        }

        #endregion

        #region Connection State

        /// <summary>
        ///     Get current connection state
        /// </summary>
        /// <param name="clientId">Client ID</param>
        /// <returns>0=Disconnected, 1=Connecting, 2=Connected, 3=Reconnecting, negative=Error</returns>
        [UnmanagedCallersOnly(EntryPoint = "leb_get_state")]
        public static int GetState(int clientId)
        {
            try
            {
                if (!Clients.TryGetValue(clientId, out var wrapper))
                    return -1;

                return wrapper.GetConnectionState() switch
                {
                    ConnectionState.Disconnected => 0,
                    ConnectionState.Connecting => 1,
                    ConnectionState.Connected => 2,
                    ConnectionState.Reconnecting => 3,
                    _ => -1,
                };
            }
            catch
            {
                return -2;
            }
        }

        /// <summary>
        ///     Get client ID assigned by server
        /// </summary>
        /// <param name="clientId">Client ID</param>
        /// <param name="buffer">Buffer to write client ID string (UTF-8)</param>
        /// <param name="bufferSize">Buffer size in bytes</param>
        /// <returns>Length of client ID string, or negative on error</returns>
        [UnmanagedCallersOnly(EntryPoint = "leb_get_client_id")]
        public static unsafe int GetClientId(int clientId, byte* buffer, int bufferSize)
        {
            try
            {
                if (!Clients.TryGetValue(clientId, out var wrapper))
                    return -1;

                var serverClientId = wrapper.GetClientId();
                if (string.IsNullOrEmpty(serverClientId))
                    return 0;

                var bytes = Encoding.UTF8.GetBytes(serverClientId);
                if (bytes.Length >= bufferSize)
                    return -3; // Buffer too small

                Marshal.Copy(bytes, 0, (IntPtr)buffer, bytes.Length);
                buffer[bytes.Length] = 0; // Null terminator
                return bytes.Length;
            }
            catch
            {
                return -2;
            }
        }

        #endregion

        #region Event Polling

        /// <summary>
        ///     Poll for next event (non-blocking)
        /// </summary>
        /// <param name="clientId">Client ID</param>
        /// <param name="eventType">Buffer for event type (UTF-8)</param>
        /// <param name="eventTypeSize">Event type buffer size</param>
        /// <param name="id">Buffer for event ID (UTF-8)</param>
        /// <param name="idSize">ID buffer size</param>
        /// <param name="username">Buffer for username (UTF-8)</param>
        /// <param name="usernameSize">Username buffer size</param>
        /// <param name="message">Buffer for message (UTF-8)</param>
        /// <param name="messageSize">Message buffer size</param>
        /// <param name="giftName">Buffer for gift name (UTF-8)</param>
        /// <param name="giftNameSize">Gift name buffer size</param>
        /// <param name="giftCount">Pointer to gift count</param>
        /// <param name="timestamp">Pointer to timestamp</param>
        /// <param name="origin">Pointer to origin</param>
        /// <param name="uid">Pointer to uid</param>
        /// <returns>1 if event retrieved, 0 if no events, negative on error</returns>
        [UnmanagedCallersOnly(EntryPoint = "leb_poll_event")]
        public static unsafe int PollEvent(
            int clientId,
            byte* eventType, int eventTypeSize,
            byte* id, int idSize,
            byte* username, int usernameSize,
            byte* message, int messageSize,
            byte* giftName, int giftNameSize,
            int* giftCount,
            long* timestamp,
            int* origin,
            long* uid)
        {
            try
            {
                if (!Clients.TryGetValue(clientId, out var wrapper))
                    return -1;

                if (!wrapper.EventQueue.TryDequeue(out var eventData)) return 0; // No events
                // Copy strings to buffers
                CopyString(eventData.EventType, eventType, eventTypeSize);
                CopyString(eventData.Id, id, idSize);
                CopyString(eventData.Username, username, usernameSize);
                CopyString(eventData.Message, message, messageSize);
                CopyString(eventData.GiftName, giftName, giftNameSize);

                // Copy primitive values
                if (giftCount != null) *giftCount = eventData.GiftCount;
                if (timestamp != null) *timestamp = eventData.Timestamp;
                if (origin != null) *origin = eventData.Origin;
                if (uid != null) *uid = eventData.Uid;

                // Free managed strings
                if (eventData.EventType != IntPtr.Zero) Marshal.FreeHGlobal(eventData.EventType);
                if (eventData.Id != IntPtr.Zero) Marshal.FreeHGlobal(eventData.Id);
                if (eventData.Username != IntPtr.Zero) Marshal.FreeHGlobal(eventData.Username);
                if (eventData.Message != IntPtr.Zero) Marshal.FreeHGlobal(eventData.Message);
                if (eventData.GiftName != IntPtr.Zero) Marshal.FreeHGlobal(eventData.GiftName);

                return 1; // Event retrieved
            }
            catch
            {
                return -2;
            }
        }

        /// <summary>
        ///     Get number of pending events in queue
        /// </summary>
        /// <param name="clientId">Client ID</param>
        /// <returns>Number of pending events, or negative on error</returns>
        [UnmanagedCallersOnly(EntryPoint = "leb_get_event_count")]
        public static int GetEventCount(int clientId)
        {
            try
            {
                if (!Clients.TryGetValue(clientId, out var wrapper))
                    return -1;

                return wrapper.EventQueue.Count;
            }
            catch
            {
                return -2;
            }
        }

        /// <summary>
        ///     Clear all pending events
        /// </summary>
        /// <param name="clientId">Client ID</param>
        /// <returns>0 on success, negative on error</returns>
        [UnmanagedCallersOnly(EntryPoint = "leb_clear_events")]
        public static int ClearEvents(int clientId)
        {
            try
            {
                if (!Clients.TryGetValue(clientId, out var wrapper))
                    return -1;

                wrapper.EventQueue.Clear();
                return 0;
            }
            catch
            {
                return -2;
            }
        }

        #endregion
    }
}