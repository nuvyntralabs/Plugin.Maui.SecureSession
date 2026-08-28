using System.Text.Json.Serialization;

namespace Plugin.Maui.SecureSession;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = false)]
[JsonSerializable(typeof(SessionRecord))]
[JsonSerializable(typeof(Dictionary<string, string>))]
sealed partial class SessionJsonContext : JsonSerializerContext;
