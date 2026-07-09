using System.Text.Json;

namespace Dsw2026Tpi.Data.Options;

public class JsonOptions
{
    public static JsonSerializerOptions JsonSerializerOptions => new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
}
