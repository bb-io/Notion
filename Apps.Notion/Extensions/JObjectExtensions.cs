using Apps.Notion.Constants;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.Notion.Extensions;

public static class JObjectExtensions
{
    public static JObject ToJObject(this object obj)
        => JObject.FromObject(obj, JsonSerializer.Create(JsonConfig.Settings));
}