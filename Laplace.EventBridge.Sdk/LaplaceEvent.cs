using System.Text.Json.Serialization;

namespace Laplace.EventBridge.Sdk
{
    /// <summary>
    ///     Base class for all LAPLACE events
    /// </summary>
    public abstract class LaplaceEvent
    {
        /// <summary>
        ///     The type of the event
        /// </summary>
        [JsonPropertyName("type")]
        public abstract string Type { get; }

        /// <summary>
        ///     Timestamp of when the event occurred
        /// </summary>
        [JsonPropertyName("timestamp")]
        public long? Timestamp { get; set; }
    }

    /// <summary>
    ///     Event sent when connection is established
    /// </summary>
    public class EstablishedEvent : LaplaceEvent
    {
        public override string Type => "established";

        [JsonPropertyName("clientId")] public string ClientId { get; set; } = string.Empty;

        [JsonPropertyName("version")] public string Version { get; set; } = string.Empty;

        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Ping event from server to client
    /// </summary>
    public class PingEvent : LaplaceEvent
    {
        public override string Type => "ping";
    }

    /// <summary>
    ///     Pong response from client to server
    /// </summary>
    public class PongEvent : LaplaceEvent
    {
        public override string Type => "pong";

        [JsonPropertyName("respondingTo")] public long? RespondingTo { get; set; }
    }

    /// <summary>
    ///     Message event from the LAPLACE platform
    /// </summary>
    public class MessageEvent : LaplaceEvent
    {
        public override string Type => "message";

        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

        [JsonPropertyName("origin")] public int Origin { get; set; }

        [JsonPropertyName("originIdx")] public int OriginIdx { get; set; }

        [JsonPropertyName("uid")] public long Uid { get; set; }

        [JsonPropertyName("username")] public string Username { get; set; } = string.Empty;

        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;

        [JsonPropertyName("timestampNormalized")]
        public long TimestampNormalized { get; set; }

        [JsonPropertyName("read")] public bool Read { get; set; }
    }

    /// <summary>
    ///     System event from the LAPLACE platform
    /// </summary>
    public class SystemEvent : LaplaceEvent
    {
        public override string Type => "system";

        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;

        [JsonPropertyName("origin")] public int Origin { get; set; }

        [JsonPropertyName("originIdx")] public int OriginIdx { get; set; }

        [JsonPropertyName("uid")] public long Uid { get; set; }

        [JsonPropertyName("username")] public string Username { get; set; } = string.Empty;

        [JsonPropertyName("timestampNormalized")]
        public long TimestampNormalized { get; set; }

        [JsonPropertyName("read")] public bool Read { get; set; }
    }

    /// <summary>
    ///     Gift event from the LAPLACE platform
    /// </summary>
    public class GiftEvent : LaplaceEvent
    {
        public override string Type => "gift";

        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

        [JsonPropertyName("origin")] public int Origin { get; set; }

        [JsonPropertyName("originIdx")] public int OriginIdx { get; set; }

        [JsonPropertyName("uid")] public long Uid { get; set; }

        [JsonPropertyName("username")] public string Username { get; set; } = string.Empty;

        [JsonPropertyName("giftName")] public string GiftName { get; set; } = string.Empty;

        [JsonPropertyName("giftCount")] public int GiftCount { get; set; }

        [JsonPropertyName("timestampNormalized")]
        public long TimestampNormalized { get; set; }

        [JsonPropertyName("read")] public bool Read { get; set; }
    }

    /// <summary>
    ///     Generic event for unknown or custom event types
    /// </summary>
    public class GenericEvent : LaplaceEvent
    {
        private string _type = "unknown";

        public override string Type => _type;

        [JsonExtensionData] public Dictionary<string, object>? AdditionalData { get; set; }

        public void SetType(string type)
        {
            _type = type;
        }
    }
}