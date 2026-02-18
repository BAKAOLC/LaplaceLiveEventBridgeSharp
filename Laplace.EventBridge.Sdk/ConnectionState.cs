namespace Laplace.EventBridge.Sdk
{
    /// <summary>
    ///     Represents the current state of the connection to the LAPLACE Event Bridge
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>
        ///     Not connected to the bridge
        /// </summary>
        Disconnected,

        /// <summary>
        ///     Establishing connection to the bridge
        /// </summary>
        Connecting,

        /// <summary>
        ///     Connected to the bridge
        /// </summary>
        Connected,

        /// <summary>
        ///     Attempting to reconnect after a disconnection
        /// </summary>
        Reconnecting,
    }
}