using Newtonsoft.Json.Linq;

namespace Apps.NotionOAuth.Utils;

public static class JObjectExtensions
{
    public static string? GetParentPageIdFromObject(this JObject jObject)
    {
        var parent = jObject["parent"];
        
        if(parent?["type"]?.ToString() == "page_id")
        {
            return parent["page_id"]?.ToString();
        }
        
        return null;
    }
    
    public static string? GetStringValue(this JObject jObject, string key)
    {
        return jObject[key]?.ToString();
    }
}