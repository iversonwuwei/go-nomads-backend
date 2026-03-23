using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UserService.Infrastructure.Converters;

/// <summary>
///     将 Supabase/PostgREST 返回的 JSONB（原生 JSON 数组/对象）
///     与 C# string 属性之间互相转换。
///     读取时：JSON token → 压缩字符串；写入时：字符串 → 原生 JSON。
/// </summary>
public class JsonbStringConverter : JsonConverter<string>
{
    public override string? ReadJson(JsonReader reader, Type objectType,
        string? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;
        if (reader.TokenType == JsonToken.String) return (string?)reader.Value;

        var token = JToken.Load(reader);
        return token.ToString(Formatting.None);
    }

    public override void WriteJson(JsonWriter writer, string? value, JsonSerializer serializer)
    {
        if (string.IsNullOrEmpty(value))
        {
            writer.WriteRawValue("[]");
            return;
        }

        writer.WriteRawValue(value);
    }
}
