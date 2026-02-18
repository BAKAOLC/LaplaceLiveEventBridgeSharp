using System.Text.Json.Serialization;

namespace Laplace.EventBridge.Sdk
{
    /// <summary>
    ///     JSON serialization context for AOT compatibility
    /// </summary>
    [JsonSerializable(typeof(LaplaceEvent))]
    [JsonSerializable(typeof(EstablishedEvent))]
    [JsonSerializable(typeof(PingEvent))]
    [JsonSerializable(typeof(PongEvent))]
    [JsonSerializable(typeof(MessageEvent))]
    [JsonSerializable(typeof(SystemEvent))]
    [JsonSerializable(typeof(GiftEvent))]
    [JsonSerializable(typeof(GenericEvent))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSourceGenerationOptions(
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false)]
    internal partial class LaplaceJsonContext : JsonSerializerContext
    {
    }
}