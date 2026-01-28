using Apps.NotionOAuth.Actions.Enums;
using Apps.NotionOAuth.Api;
using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Entities;
using Apps.NotionOAuth.Models.Request.Block;
using Apps.NotionOAuth.Models.Response.Block;
using Apps.NotionOAuth.Models.Response.DataBase;
using Apps.NotionOAuth.Models.Response.Page;
using Apps.NotionOAuth.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.NotionOAuth.Actions;

[ActionList("Blocks")]
public class BlockActions(InvocationContext invocationContext) : NotionInvocable(invocationContext)
{
    private const int MaxBlocksUploadSize = 100;

    [Action("Get block", Description = "Get details of a specific block")]
    public async Task<BlockEntity> GetBlock([ActionParameter] BlockRequest input)
    {
        var endpoint = $"{ApiEndpoints.Blocks}/{input.BlockId}";
        var request = new NotionRequest(endpoint, Method.Get, Creds);

        var response = await Client.ExecuteWithErrorHandling<BlockResponse>(request);
        return new(response);
    }

    [Action("Search block children", Description = "List children of a specific block")]
    public async Task<ListBlockChildrenResponse> ListBlockChildren([ActionParameter] BlockRequest input)
    {
        var endpoint = $"{ApiEndpoints.Blocks}/{input.BlockId}/children";
        var request = new NotionRequest(endpoint, Method.Get, Creds);

        var response = await Client.Paginate<BlockResponse>(request);
        var blocks = response.Select(x => new BlockEntity(x)).ToArray();

        return new(blocks);
    }

    [Action("Delete block", Description = "Delete a specific block")]
    public Task DeleteBlock([ActionParameter] BlockRequest input)
    {
        var endpoint = $"{ApiEndpoints.Blocks}/{input.BlockId}";
        var request = new NotionRequest(endpoint, Method.Delete, Creds);

        return Client.ExecuteWithErrorHandling(request);
    }

    internal Task AppendBlockChildren(string containerId, JObject[] blocks)
        => AppendBlockChildren(containerId, blocks, ContainerType.Page, nearestPageIdForDatabaseCreation: null);

    internal async Task AppendBlockChildren(
        string containerId,
        JObject[] blocks,
        ContainerType kind,
        string? nearestPageIdForDatabaseCreation)
    {
        var blockChunks = ChunkBlocks(blocks);

        foreach (var blockChunk in blockChunks)
        {
            await PromoteNestedPagesAndDatabasesAsync(blockChunk, containerId, kind, nearestPageIdForDatabaseCreation);

            if (blockChunk.Any(x => x["type"]?.ToString() == "child_page"))
            {
                await ProcessChildPages(containerId, kind, blockChunk, nearestPageIdForDatabaseCreation);
            }
            else if (blockChunk.Any(x => x["object"]?.ToString() == "database"))
            {
                await ProcessDatabases(containerId, kind, blockChunk, nearestPageIdForDatabaseCreation);
            }
            else
            {
                await ProcessBlocks(containerId, blockChunk);
            }
        }
    }

    private async Task PromoteNestedPagesAndDatabasesAsync(
        List<JObject> roots,
        string containerId,
        ContainerType containerKind,
        string? nearestPageIdForDatabaseCreation)
    {
        foreach (var root in roots)
            await PromoteInNodeAsync(root, containerId, containerKind, nearestPageIdForDatabaseCreation);
    }

    private static bool IsChildPageOrDatabase(JObject obj)
    {
        var isChildPage = string.Equals(obj["type"]?.ToString(), "child_page", StringComparison.OrdinalIgnoreCase);
        var isDatabase = string.Equals(obj["object"]?.ToString(), "database", StringComparison.OrdinalIgnoreCase);
        return isChildPage || isDatabase;
    }

    private async Task PromoteInNodeAsync(
        JObject node,
        string containerId,
        ContainerType containerKind,
        string? nearestPageIdForDatabaseCreation)
    {
        if (IsChildPageOrDatabase(node))
            return;

        if (node["children"] is JArray directChildren)
        {
            await PromoteInChildrenArrayAsync(directChildren, containerId, containerKind, nearestPageIdForDatabaseCreation);
        }

        var type = node["type"]?.ToString();
        if (!string.IsNullOrEmpty(type) && node[type] is JObject content && content["children"] is JArray typedChildren)
        {
            await PromoteInChildrenArrayAsync(typedChildren, containerId, containerKind, nearestPageIdForDatabaseCreation);
        }
    }

    private async Task PromoteInChildrenArrayAsync(
        JArray children,
        string containerId,
        ContainerType containerKind,
        string? nearestPageIdForDatabaseCreation)
    {
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] is not JObject childObj)
                continue;

            var isChildPage = string.Equals(childObj["type"]?.ToString(), "child_page", StringComparison.OrdinalIgnoreCase);
            var isDatabase = string.Equals(childObj["object"]?.ToString(), "database", StringComparison.OrdinalIgnoreCase);

            if (isChildPage)
            {
                var createdPageId = await CreatePromotedChildPageIdAsync(childObj, containerId, containerKind);
                children[i] = BuildLinkToPageBlock(createdPageId, isDatabase: false);
                continue;
            }

            if (isDatabase)
            {
                var dbParentPageId = containerKind == ContainerType.Page
                    ? containerId
                    : nearestPageIdForDatabaseCreation
                      ?? throw new InvalidOperationException(
                          "Cannot create a database in database context because there is no page parent to promote to.");

                var createdDbId = await CreatePromotedDatabaseIdAsync(childObj, dbParentPageId);
                children[i] = BuildLinkToPageBlock(createdDbId, isDatabase: true);
                continue;
            }

            await PromoteInNodeAsync(childObj, containerId, containerKind, nearestPageIdForDatabaseCreation);
        }
    }

    private static JObject BuildLinkToPageBlock(string id, bool isDatabase)
    {
        if (isDatabase)
        {
            return new JObject
            {
                ["object"] = "block",
                ["type"] = "link_to_page",
                ["link_to_page"] = new JObject
                {
                    ["type"] = "database_id",
                    ["database_id"] = id
                }
            };
        }

        return new JObject
        {
            ["object"] = "block",
            ["type"] = "link_to_page",
            ["link_to_page"] = new JObject
            {
                ["type"] = "page_id",
                ["page_id"] = id
            }
        };
    }

    private async Task<string> CreatePromotedChildPageIdAsync(JObject pageBlock, string containerId, ContainerType containerKind)
    {
        RemoveUnnecessaryProperties(pageBlock, "type", "child_page");

        var children = pageBlock["children"]?.ToObject<JObject[]>() ?? Array.Empty<JObject>();
        pageBlock.Remove("children");

        pageBlock["parent"] = containerKind == ContainerType.Database
            ? new JObject { ["type"] = "database_id", ["database_id"] = containerId }
            : new JObject { ["type"] = "page_id", ["page_id"] = containerId };

        RemoveDisallowedGeneratedProperties(pageBlock);
        RemoveStatusProperties(pageBlock);
        NormalizeSelectAndMultiSelectProperties(pageBlock);

        RemoveDisallowedGeneratedProperties(pageBlock);
        RemoveStatusProperties(pageBlock);

        NotionHtmlParser.RemoveNullsDeep(pageBlock);

        var request = new NotionRequest(ApiEndpoints.Pages, Method.Post, Creds)
            .WithJsonBody(pageBlock, JsonConfig.Settings);

        var created = await Client.ExecuteWithErrorHandling<PageResponse>(request);

        if (children.Length > 0)
        {
            FlattenListItemsDeepInPlace(new JArray(children));
            await AppendBlockChildren(created.Id, children, ContainerType.Page, nearestPageIdForDatabaseCreation: null);
        }

        return created.Id;
    }

    private async Task<string> CreatePromotedDatabaseIdAsync(JObject databaseBlock, string parentPageId)
    {
        var children = databaseBlock["children"]?.ToObject<JObject[]>()
                       ?? throw new InvalidOperationException("Child database must have children");

        databaseBlock.Remove("children");

        databaseBlock["parent"] = new JObject
        {
            ["type"] = "page_id",
            ["page_id"] = parentPageId
        };

        RemoveGroupsFromProperties(databaseBlock);
        FixStatusProperties(databaseBlock);
        NotionHtmlParser.RemoveNullsDeep(databaseBlock);

        var request = new NotionRequest(ApiEndpoints.Databases, Method.Post, Creds, ApiConstants.NotLatestApiVersion)
            .WithJsonBody(databaseBlock, JsonConfig.Settings);

        var created = await Client.ExecuteWithErrorHandling<DatabaseResponse>(request);

        FlattenListItemsDeepInPlace(new JArray(children));
        await AppendBlockChildren(created.Id, children, ContainerType.Database, nearestPageIdForDatabaseCreation: parentPageId);

        return created.Id;
    }

    private List<List<JObject>> ChunkBlocks(JObject[] blocks)
    {
        var blockChunks = new List<List<JObject>>();
        var currentChunk = new List<JObject>();

        foreach (var block in blocks)
        {
            if (block["type"]?.ToString() == "child_page" || block["object"]?.ToString() == "database")
            {
                if (currentChunk.Any())
                {
                    blockChunks.Add(currentChunk);
                    currentChunk = new List<JObject>();
                }

                blockChunks.Add(new List<JObject> { block });
            }
            else
            {
                currentChunk.Add(block);

                if (currentChunk.Count >= MaxBlocksUploadSize)
                {
                    blockChunks.Add(currentChunk);
                    currentChunk = new List<JObject>();
                }
            }
        }

        if (currentChunk.Any())
        {
            blockChunks.Add(currentChunk);
        }

        return blockChunks;
    }

    private async Task ProcessChildPages(string containerId, ContainerType kind, List<JObject> blockChunk, string? nearestPageIdForDatabaseCreation)
    {
        foreach (var page in blockChunk)
        {
            RemoveUnnecessaryProperties(page, "type", "child_page");

            var children = page["children"]?.ToObject<JObject[]>() ?? Array.Empty<JObject>();
            page.Remove("children");

            page["parent"] = kind == ContainerType.Database
                ? new JObject { ["type"] = "database_id", ["database_id"] = containerId }
                : new JObject { ["type"] = "page_id", ["page_id"] = containerId };

            RemoveDisallowedGeneratedProperties(page);
            RemoveStatusProperties(page);
            NormalizeSelectAndMultiSelectProperties(page);

            RemoveDisallowedGeneratedProperties(page);
            RemoveStatusProperties(page);

            NotionHtmlParser.RemoveNullsDeep(page);

            var request = new NotionRequest(ApiEndpoints.Pages, Method.Post, Creds)
                .WithJsonBody(page, JsonConfig.Settings);

            var pageResponse = await Client.ExecuteWithErrorHandling<PageResponse>(request);

            if (children.Length > 0)
            {
                FlattenListItemsDeepInPlace(new JArray(children));
                await AppendBlockChildren(pageResponse.Id, children, ContainerType.Page, nearestPageIdForDatabaseCreation: null);
            }
        }
    }

    private async Task ProcessDatabases(string containerId, ContainerType kind, List<JObject> blockChunk, string? nearestPageIdForDatabaseCreation)
    {
        foreach (var database in blockChunk)
        {
            var children = database["children"]?.ToObject<JObject[]>()
                           ?? throw new InvalidOperationException("Child database must have children");

            database.Remove("children");

            var parentPageId = kind == ContainerType.Page
                ? containerId
                : nearestPageIdForDatabaseCreation
                  ?? throw new InvalidOperationException("Databases must have a page parent to be created.");

            database["parent"] = new JObject
            {
                ["type"] = "page_id",
                ["page_id"] = parentPageId
            };

            RemoveGroupsFromProperties(database);
            FixStatusProperties(database);

            var request = new NotionRequest(ApiEndpoints.Databases, Method.Post, Creds, ApiConstants.NotLatestApiVersion)
                .WithJsonBody(database, JsonConfig.Settings);

            var createdDatabase = await Client.ExecuteWithErrorHandling<DatabaseResponse>(request);

            FlattenListItemsDeepInPlace(new JArray(children));
            await AppendBlockChildren(createdDatabase.Id, children, ContainerType.Database, nearestPageIdForDatabaseCreation: parentPageId);
        }
    }

    private async Task ProcessBlocks(string blockId, List<JObject> blockChunk)
    {
        SanitizeBlocks(blockChunk);
        FlattenListItemsDeepInPlace(new JArray(blockChunk));

        var endpoint = $"{ApiEndpoints.Blocks}/{blockId}/children";
        var request = new NotionRequest(endpoint, Method.Patch, Creds)
            .WithJsonBody(new ChildrenRequest { Children = blockChunk.ToArray() }, JsonConfig.Settings);

        await Client.ExecuteWithErrorHandling(request);
    }

    private static readonly HashSet<string> ListItemTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "bulleted_list_item",
        "numbered_list_item"
    };

    private void FlattenListItemsDeepInPlace(JToken node)
    {
        if (node is JArray arr)
        {
            ProcessChildrenArray(arr);
            foreach (var el in arr)
                FlattenListItemsDeepInPlace(el);
            return;
        }

        if (node is JObject obj)
        {
            var type = obj["type"]?.ToString();
            if (!string.IsNullOrEmpty(type) && obj[type] is JObject content && content["children"] is JArray typedChildren)
            {
                ProcessChildrenArray(typedChildren);
                FlattenListItemsDeepInPlace(typedChildren);
            }

            if (obj["children"] is JArray directChildren)
            {
                ProcessChildrenArray(directChildren);
                FlattenListItemsDeepInPlace(directChildren);
            }

            return;
        }
    }

    private void ProcessChildrenArray(JArray children)
    {
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] is not JObject item) continue;

            var type = item["type"]?.ToString();
            if (type is null) continue;

            if (ListItemTypes.Contains(type) && item[type] is JObject content
                && content.TryGetValue("children", out var chTok) && chTok is JArray liChildren && liChildren.Count > 0)
            {
                content.Remove("children");

                FlattenListItemsDeepInPlace(liChildren);
                var insertAt = i + 1;
                foreach (var child in liChildren.OfType<JObject>())
                {
                    children.Insert(insertAt, child);
                    insertAt++;
                }

                i = insertAt - 1;
            }
        }
    }

    private void SanitizeBlocks(List<JObject> blocks)
    {
        for (int i = blocks.Count - 1; i >= 0; i--)
        {
            var keep = SanitizeBlockDeep(blocks[i]);
            if (!keep)
                blocks.RemoveAt(i);
        }
    }

    private bool SanitizeBlockDeep(JObject block)
    {
        if (string.Equals(block["type"]?.ToString(), "callout", StringComparison.OrdinalIgnoreCase))
        {
            var callout = block["callout"] as JObject;
            if (callout != null && (callout["icon"] == null || callout["icon"].Type == JTokenType.Null))
                callout.Remove("icon");
        }

        if (string.Equals(block["type"]?.ToString(), "video", StringComparison.OrdinalIgnoreCase))
        {
            var ok = NormalizeVideoBlock(block);
            if (!ok)
                return false;
        }

        NotionHtmlParser.RemoveNullsDeep(block);

        if (block["children"] is JArray directChildren)
        {
            for (int i = directChildren.Count - 1; i >= 0; i--)
            {
                if (directChildren[i] is not JObject childObj) continue;
                if (!SanitizeBlockDeep(childObj))
                    directChildren.RemoveAt(i);
            }
        }

        var type = block["type"]?.ToString();
        if (!string.IsNullOrEmpty(type) && block[type] is JObject content && content["children"] is JArray typedChildren)
        {
            for (int i = typedChildren.Count - 1; i >= 0; i--)
            {
                if (typedChildren[i] is not JObject childObj) continue;
                if (!SanitizeBlockDeep(childObj))
                    typedChildren.RemoveAt(i);
            }
        }

        return true;
    }

    private static bool NormalizeVideoBlock(JObject block)
    {
        if (block["video"] is not JObject video)
            return false;

        if (video["external"] is JObject || video["file_upload"] is JObject)
            return true;

        var fileUrl = video["file"]?["url"]?.ToString();
        if (string.IsNullOrWhiteSpace(fileUrl))
            return false;

        video.Remove("file");
        video["external"] = new JObject { ["url"] = fileUrl };
        video["type"] = "external";

        return true;
    }

    private void RemoveUnnecessaryProperties(JObject jObject, params string[] properties)
    {
        foreach (var property in properties)
        {
            if (jObject.TryGetValue(property, out _))
                jObject.Remove(property);
        }
    }

    private void RemoveGroupsFromProperties(JObject database)
    {
        if (database["properties"] is JObject properties)
        {
            foreach (var property in properties.Properties())
            {
                if (property.Value is JObject propObj)
                {
                    RemoveGroups(propObj);
                }
            }
        }
    }

    private void RemoveGroups(JObject jObject)
    {
        if (jObject.ContainsKey("groups"))
        {
            jObject.Remove("groups");
        }

        foreach (var child in jObject.Properties().Select(p => p.Value).OfType<JObject>().ToList())
        {
            RemoveGroups(child);
        }
    }

    // According to Notion documentation: Creating new status database properties is currently not supported.
    private void FixStatusProperties(JObject database)
    {
        if (database["properties"] is JObject properties)
        {
            foreach (var property in properties.Properties())
            {
                if (property.Value is JObject propObj &&
                    string.Equals((string?)propObj["type"], DatabasePropertyTypes.Status, StringComparison.OrdinalIgnoreCase))
                {
                    if (propObj.TryGetValue("status", out _))
                    {
                        propObj.Remove("status");
                        propObj["status"] = new JObject();
                    }
                }
            }
        }
    }

    private void RemoveDisallowedGeneratedProperties(JObject page)
    {
        if (page["properties"] is JObject properties)
        {
            var disallowedTypes = new HashSet<string>
            {
                DatabasePropertyTypes.Rollup,
                DatabasePropertyTypes.CreatedBy,
                DatabasePropertyTypes.CreatedTime,
                DatabasePropertyTypes.LastEditedBy,
                DatabasePropertyTypes.LastEditedTime
            };

            var propsToRemove = properties.Properties()
                .Where(prop => prop.Value is JObject propObj &&
                               propObj.TryGetValue("type", out var typeToken) &&
                               disallowedTypes.Contains((string)typeToken))
                .Select(prop => prop.Name)
                .ToList();

            foreach (var propName in propsToRemove)
            {
                properties.Remove(propName);
            }
        }
    }

    private void RemoveStatusProperties(JObject page)
    {
        if (page["properties"] is JObject properties)
        {
            var propsToRemove = properties.Properties()
                .Where(prop => prop.Value is JObject propObj &&
                               propObj.TryGetValue("type", out var typeToken) &&
                               string.Equals((string)typeToken, DatabasePropertyTypes.Status, StringComparison.OrdinalIgnoreCase))
                .Select(prop => prop.Name)
                .ToList();

            foreach (var propName in propsToRemove)
            {
                properties.Remove(propName);
            }
        }
    }

    private static void NormalizeSelectAndMultiSelectProperties(JObject page)
    {
        if (page["properties"] is not JObject properties)
            return;

        foreach (var prop in properties.Properties().ToList())
        {
            if (prop.Value is not JObject propObj)
                continue;

            var type = propObj["type"]?.ToString();
            if (string.IsNullOrWhiteSpace(type))
                continue;

            if (string.Equals(type, "select", StringComparison.OrdinalIgnoreCase))
            {
                var name = ExtractOptionName(propObj["select"]);
                if (string.IsNullOrWhiteSpace(name))
                {
                    properties.Remove(prop.Name);
                    continue;
                }

                propObj["select"] = new JObject { ["name"] = name };
                continue;
            }

            if (string.Equals(type, "multi_select", StringComparison.OrdinalIgnoreCase))
            {
                var names = ExtractOptionNames(propObj["multi_select"]);
                if (names.Count == 0)
                {
                    properties.Remove(prop.Name);
                    continue;
                }

                propObj["multi_select"] = new JArray(names.Select(n => new JObject { ["name"] = n }));
                continue;
            }
        }
    }

    private static string? ExtractOptionName(JToken? token)
    {
        if (token is null || token.Type == JTokenType.Null)
            return null;

        if (token.Type == JTokenType.String)
            return token.ToString().Trim();

        if (token is JObject obj)
        {
            var name = obj["name"]?.ToString();
            if (!string.IsNullOrWhiteSpace(name))
                return name.Trim();
        }

        return null;
    }

    private static List<string> ExtractOptionNames(JToken? token)
    {
        var result = new List<string>();

        if (token is null || token.Type == JTokenType.Null)
            return result;

        if (token is JArray arr)
        {
            foreach (var item in arr)
            {
                var name = ExtractOptionName(item);
                if (!string.IsNullOrWhiteSpace(name))
                    result.Add(name);
            }

            return result;
        }

        if (token.Type == JTokenType.String)
        {
            var s = token.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(s))
                result.Add(s);
        }

        return result;
    }
}
