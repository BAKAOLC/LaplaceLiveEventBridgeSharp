using System.Collections.Concurrent;

namespace Laplace.EventBridge.LuaWrapper
{
    /// <summary>
    ///     Thread-safe event queue for storing received events
    /// </summary>
    internal class EventQueue
    {
        private readonly ConcurrentQueue<LaplaceEventData> _queue = new();

        public int MaxQueueSize { get; set; } = 1000;

        public int Count => _queue.Count;

        public void Enqueue(LaplaceEventData eventData)
        {
            // Limit queue size to prevent memory issues
            if (_queue.Count >= MaxQueueSize) _queue.TryDequeue(out _); // Remove oldest event
            _queue.Enqueue(eventData);
        }

        public bool TryDequeue(out LaplaceEventData eventData)
        {
            return _queue.TryDequeue(out eventData);
        }

        public void Clear()
        {
            _queue.Clear();
        }
    }

    /// <summary>
    ///     Simplified event data structure for C interop
    /// </summary>
    internal struct LaplaceEventData
    {
        public IntPtr EventType; // C string
        public IntPtr Id; // C string
        public IntPtr Username; // C string
        public IntPtr Message; // C string
        public IntPtr GiftName; // C string
        public int GiftCount;
        public long Timestamp;
        public int Origin;
        public long Uid;
    }
}