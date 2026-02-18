namespace Laplace.EventBridge.Sdk
{
    /// <summary>
    ///     Configuration options for connecting to the LAPLACE Event Bridge
    /// </summary>
    public class ConnectionOptions
    {
        /// <summary>
        ///     The URL of the LAPLACE Event Bridge server
        /// </summary>
        /// <example>ws://localhost:9696</example>
        public string Url { get; set; } = "ws://localhost:9696";

        /// <summary>
        ///     The authentication token for the LAPLACE Event Bridge server
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        ///     Whether to automatically reconnect to the LAPLACE Event Bridge server
        /// </summary>
        public bool Reconnect { get; set; } = true;

        /// <summary>
        ///     The base interval between reconnect attempts in milliseconds.
        ///     With exponential backoff, each attempt multiplies this by 1.5^(attempt-1).
        ///     The maximum interval is capped at 60 seconds.
        /// </summary>
        /// <example>
        ///     With base interval of 3000ms:
        ///     Attempt 1: 3000ms
        ///     Attempt 2: 4500ms
        ///     Attempt 3: 6750ms
        ///     ...
        ///     Capped at: 60000ms
        /// </example>
        public int ReconnectInterval { get; set; } = 3000;

        /// <summary>
        ///     The maximum number of reconnect attempts before giving up
        /// </summary>
        public int MaxReconnectAttempts { get; set; } = 1000;

        /// <summary>
        ///     Timeout in milliseconds for ping monitoring.
        ///     If no ping is received within this time, the connection is considered dead.
        /// </summary>
        public int PingTimeout { get; set; } = 90000; // 90 seconds
    }
}