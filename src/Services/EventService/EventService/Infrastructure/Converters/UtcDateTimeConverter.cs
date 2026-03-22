using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventService.Infrastructure.Converters;

/// <summary>
///     JSON 转换器：确保所有 DateTime 序列化为 UTC 格式（带 Z 后缀）
///     解决客户端收到不带时区标记的时间字符串导致的时区解析错误
/// </summary>
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dt = reader.GetDateTime();
        // 确保反序列化后的 DateTime 为 UTC
        return dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // 统一输出 UTC 格式，确保带 Z 后缀
        var utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        writer.WriteStringValue(utc.ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }
}
