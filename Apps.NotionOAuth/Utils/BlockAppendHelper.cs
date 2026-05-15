using Newtonsoft.Json.Linq;

namespace Apps.NotionOAuth.Utils;

internal static class BlockAppendHelper
{
    private static readonly HashSet<string> PreserveInlineChildrenTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "table"
    };

    internal sealed class BlockChildrenSnapshot
    {
        public required JObject Block { get; init; }

        public required JObject[] Children { get; init; }
    }

    internal static List<BlockChildrenSnapshot> SnapshotAndDetachChildren(List<JObject> blocks)
    {
        var snapshots = new List<BlockChildrenSnapshot>(blocks.Count);

        foreach (var block in blocks)
        {
            snapshots.Add(new BlockChildrenSnapshot
            {
                Block = block,
                Children = ExtractAndDetachChildren(block)
            });
        }

        return snapshots;
    }

    private static JObject[] ExtractAndDetachChildren(JObject block)
    {
        var children = new List<JObject>();
        var type = block["type"]?.ToString();

        if (!string.IsNullOrWhiteSpace(type) && PreserveInlineChildrenTypes.Contains(type))
        {
            return [];
        }

        if (block["children"] is JArray directChildren)
        {
            children.AddRange(directChildren.OfType<JObject>().Select(x => (JObject)x.DeepClone()));
            block.Remove("children");
        }

        if (!string.IsNullOrEmpty(type) &&
            block[type] is JObject typedContent &&
            typedContent["children"] is JArray typedChildren)
        {
            children.AddRange(typedChildren.OfType<JObject>().Select(x => (JObject)x.DeepClone()));
            typedContent.Remove("children");
        }

        return children.ToArray();
    }
}
