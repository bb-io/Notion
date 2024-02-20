using Apps.NotionOAuth.Models.Entities;
using Newtonsoft.Json.Linq;

namespace Apps.NotionOAuth.Models.Response.Page;

public class PageResponse
{
    public string Id { get; set; }

    public DateTime? CreatedTime { get; set; }

    public DateTime? LastEditedTime { get; set; }

    public ParentEntity? Parent { get; set; }

    public bool? Archived { get; set; }

    public string? Url { get; set; }

    public Dictionary<string, JObject>? Properties { get; set; }

    public JObject? FindPropertyById(string id) => Properties.FirstOrDefault(x => x.Value.ContainsKey("id") ? x.Value["id"].ToString() == id : false).Value;
}