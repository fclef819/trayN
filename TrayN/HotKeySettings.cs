using System.Text.Json;
using System.Text.Json.Serialization;

namespace TrayN;

internal sealed class HotKeySettings
{
    [JsonPropertyName("control")]
    public bool Control { get; set; }

    [JsonPropertyName("alt")]
    public bool Alt { get; set; }

    [JsonPropertyName("shift")]
    public bool Shift { get; set; }

    [JsonPropertyName("win")]
    public bool Win { get; set; }

    [JsonConverter(typeof(KeysJsonConverter))]
    [JsonPropertyName("key")]
    public Keys Key { get; set; } = Keys.M;

    public static HotKeySettings Default() => new()
    {
        Control = true,
        Alt = true,
        Shift = false,
        Win = false,
        Key = Keys.M
    };

    public HotKeySettings Clone() => new()
    {
        Control = Control,
        Alt = Alt,
        Shift = Shift,
        Win = Win,
        Key = Key
    };
}

internal sealed class KeysJsonConverter : JsonConverter<Keys>
{
    public override Keys Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<Keys>(value, ignoreCase: true, out var key))
            {
                return key;
            }

            return Keys.None;
        }

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var number))
        {
            return Enum.IsDefined(typeof(Keys), number) ? (Keys)number : Keys.None;
        }

        return Keys.None;
    }

    public override void Write(Utf8JsonWriter writer, Keys value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
