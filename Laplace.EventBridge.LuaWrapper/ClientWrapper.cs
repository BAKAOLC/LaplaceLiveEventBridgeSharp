using System.Runtime.InteropServices;
using System.Text;
using Laplace.EventBridge.Sdk;

namespace Laplace.EventBridge.LuaWrapper
{
    /// <summary>
    ///     Wrapper around LaplaceEventBridgeClient for managing events
    /// </summary>
    internal class ClientWrapper : IDisposable
    {
        private readonly LaplaceEventBridgeClient _client;

        public ClientWrapper(ConnectionOptions options)
        {
            _client = new(options);
            EventQueue = new();

            // Subscribe to all events and add them to queue
            _client.OnAny(OnEventReceived);
        }

        public EventQueue EventQueue { get; }

        public void Dispose()
        {
            _client.DisconnectAsync().GetAwaiter().GetResult();
            _client.Dispose();
            EventQueue.Clear();
        }

        private void OnEventReceived(LaplaceEvent evt)
        {
            var eventData = new LaplaceEventData
            {
                EventType = StringToHGlobal(evt.Type),
                Timestamp = evt.Timestamp ?? 0,
            };

            // Populate fields based on event type
            switch (evt)
            {
                case MessageEvent msg:
                    eventData.Id = StringToHGlobal(msg.Id);
                    eventData.Username = StringToHGlobal(msg.Username);
                    eventData.Message = StringToHGlobal(msg.Message);
                    eventData.Origin = msg.Origin;
                    eventData.Uid = msg.Uid;
                    eventData.Timestamp = msg.TimestampNormalized;
                    break;

                case SystemEvent sys:
                    eventData.Id = StringToHGlobal(sys.Id);
                    eventData.Username = StringToHGlobal(sys.Username);
                    eventData.Message = StringToHGlobal(sys.Message);
                    eventData.Origin = sys.Origin;
                    eventData.Uid = sys.Uid;
                    eventData.Timestamp = sys.TimestampNormalized;
                    break;

                case GiftEvent gift:
                    eventData.Id = StringToHGlobal(gift.Id);
                    eventData.Username = StringToHGlobal(gift.Username);
                    eventData.GiftName = StringToHGlobal(gift.GiftName);
                    eventData.GiftCount = gift.GiftCount;
                    eventData.Origin = gift.Origin;
                    eventData.Uid = gift.Uid;
                    eventData.Timestamp = gift.TimestampNormalized;
                    break;

                case EstablishedEvent est:
                    eventData.Id = StringToHGlobal(est.ClientId);
                    eventData.Message = StringToHGlobal(est.Message);
                    break;
            }

            EventQueue.Enqueue(eventData);
        }

        private static IntPtr StringToHGlobal(string? str)
        {
            if (string.IsNullOrEmpty(str))
                return IntPtr.Zero;

            var bytes = Encoding.UTF8.GetBytes(str);
            var ptr = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            Marshal.WriteByte(ptr, bytes.Length, 0); // Null terminator
            return ptr;
        }

        public Task ConnectAsync()
        {
            return _client.ConnectAsync();
        }

        public Task DisconnectAsync()
        {
            return _client.DisconnectAsync();
        }

        public ConnectionState GetConnectionState()
        {
            return _client.GetConnectionState();
        }

        public string? GetClientId()
        {
            return _client.GetClientId();
        }
    }
}