using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AllocServer.Converters
{
    /// <summary>
    /// Custom JsonConverter giúp giải mã linh hoạt các kiểu dữ liệu đại diện cho boolean (string "true", "1", bool, v.v.)
    /// </summary>
    public class FlexibleBoolConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.True) return true;
            if (reader.TokenType == JsonTokenType.False) return false;

            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (bool.TryParse(stringValue, out var boolResult))
                {
                    return boolResult;
                }

                if (stringValue == "1") return true;
                if (stringValue == "0") return false;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt32(out var intValue))
                {
                    return intValue != 0;
                }
            }

            throw new JsonException($"Không thể chuyển đổi giá trị JSON thành kiểu boolean.");
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }
    }
}
