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
using DocumentFormat.OpenXml.InkML;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.NotionOAuth.Actions;

[ActionList("Blocks")]
public class BlockActions(InvocationContext invocationContext) : NotionInvocable(invocationContext)
{
    private const int MaxBlocksUploadSize = 100;
    public enum ContainerKind
    {
        Page,
        Database
    }

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
     => AppendBlockChildren(containerId, blocks, ContainerKind.Page, nearestPageIdForDatabaseCreation: null);

    internal async Task AppendBlockChildren(
      string containerId,
      JObject[] blocks,
      ContainerKind kind,
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

    //
    private async Task PromoteNestedPagesAndDatabasesAsync(
        List<JObject> roots,
        string containerId,
        ContainerKind containerKind,
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
        ContainerKind containerKind,
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
        ContainerKind containerKind,
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
                var createdPageId = await CreatePromotedChildPageAsync(childObj, containerId, containerKind);

                children[i] = BuildLinkToPageBlock(createdPageId, isDatabase: false);
                continue;
            }

            if (isDatabase)
            {
                var dbParentPageId = containerKind == ContainerKind.Page
                    ? containerId
                    : nearestPageIdForDatabaseCreation
                      ?? throw new InvalidOperationException(
                          "Cannot create a database in database context because there is no page parent to promote to.");

                var createdDbId = await CreatePromotedDatabaseAsync(childObj, dbParentPageId);
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

    private async Task<string> CreatePromotedChildPageAsync(JObject pageBlock, string containerId, ContainerKind containerKind)
    {
        RemoveUnnecessaryProperties(pageBlock, "type", "child_page");

        var children = pageBlock["children"]?.ToObject<JObject[]>() ?? Array.Empty<JObject>();
        pageBlock.Remove("children");

        pageBlock["parent"] = containerKind == ContainerKind.Database
            ? new JObject { ["type"] = "database_id", ["database_id"] = containerId }
            : new JObject { ["type"] = "page_id", ["page_id"] = containerId };

        RemoveDisallowedGeneratedProperties(pageBlock);
        RemoveStatusProperties(pageBlock);
        NotionHtmlParser.RemoveNullsDeep(pageBlock);

        var request = new NotionRequest(ApiEndpoints.Pages, Method.Post, Creds)
            .WithJsonBody(pageBlock, JsonConfig.Settings);

        var created = await Client.ExecuteWithErrorHandling<PageResponse>(request);

        if (children.Length > 0)
        {
            FlattenListItemsDeepInPlace(new JArray(children));
            await AppendBlockChildren(created.Id, children, ContainerKind.Page, nearestPageIdForDatabaseCreation: null);
        }

        return created.Id;
    }

    private async Task<string> CreatePromotedDatabaseAsync(JObject databaseBlock, string parentPageId)
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
        await AppendBlockChildren(created.Id, children, ContainerKind.Database, nearestPageIdForDatabaseCreation: parentPageId);

        return created.Id;
    }
    //

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

    private async Task ProcessChildPages(string containerId, ContainerKind kind, List<JObject> blockChunk, string? nearestPageIdForDatabaseCreation)
    {
        foreach (var page in blockChunk)
        {
            RemoveUnnecessaryProperties(page, "type", "child_page");

            var children = page["children"]?.ToObject<JObject[]>() ?? Array.Empty<JObject>();
            page.Remove("children");

            page["parent"] = kind == ContainerKind.Database
                ? new JObject { ["type"] = "database_id", ["database_id"] = containerId }
                : new JObject { ["type"] = "page_id", ["page_id"] = containerId };

            RemoveDisallowedGeneratedProperties(page);
            RemoveStatusProperties(page);

            var request = new NotionRequest(ApiEndpoints.Pages, Method.Post, Creds)
                .WithJsonBody(page, JsonConfig.Settings);

            var pageResponse = await Client.ExecuteWithErrorHandling<PageResponse>(request);
            if (children.Length > 0)
            {
                FlattenListItemsDeepInPlace(new JArray(children));
                await AppendBlockChildren(pageResponse.Id, children, ContainerKind.Page, nearestPageIdForDatabaseCreation: null);
            }
        }
    }

    private async Task ProcessDatabases(string containerId, ContainerKind kind, List<JObject> blockChunk, string? nearestPageIdForDatabaseCreation)
    {
        foreach (var database in blockChunk)
        {
            var children = database["children"]?.ToObject<JObject[]>()
                           ?? throw new InvalidOperationException("Child database must have children");

            database.Remove("children");

            var parentPageId = kind == ContainerKind.Page
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
            await AppendBlockChildren(createdDatabase.Id, children, ContainerKind.Database, nearestPageIdForDatabaseCreation: parentPageId);
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

    private void FlattenListItemChildren(List<JObject> blocks)
    {
        for (int i = 0; i < blocks.Count; i++)
        {
            var b = blocks[i];
            var type = b["type"]?.ToString();
            if (type is null) continue;

            if (ListItemTypes.Contains(type) && b[type] is JObject content &&
                content.TryGetValue("children", out var childrenTok) && childrenTok is JArray children && children.Count > 0)
            {
                content.Remove("children");

                var childBlocks = children.OfType<JObject>().ToList();

                FlattenListItemChildren(childBlocks);

                blocks.InsertRange(i + 1, childBlocks);
                i += childBlocks.Count;
            }
        }
    }

    private void SanitizeBlocks(IEnumerable<JObject> blocks)
    {
        foreach (var b in blocks)
        {
            if (string.Equals(b["type"]?.ToString(), "callout", StringComparison.OrdinalIgnoreCase))
            {
                var callout = b["callout"] as JObject;
                if (callout != null && (callout["icon"] == null || callout["icon"].Type == JTokenType.Null))
                    callout.Remove("icon");
            }
            NotionHtmlParser.RemoveNullsDeep(b);
        }
    }

    private void RemoveUnnecessaryProperties(JObject jObject, params string[] properties)
    {
        foreach (var property in properties)
        {
            if (jObject.TryGetValue(property, out _))
            {
                jObject.Remove(property);
            }
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
    
    // According to Notion documentation: Creating new status database properties is currently not supported. https://developers.notion.com/reference/create-a-database
    private void FixStatusProperties(JObject database)
    {
        if (database["properties"] is JObject properties)
        {
            foreach (var property in properties.Properties())
            {
                if (property.Value is JObject propObj && (string.Equals((string)propObj["type"], DatabasePropertyTypes.Status,
                        StringComparison.OrdinalIgnoreCase)))
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
            var disallowedTypes = new HashSet<string> { DatabasePropertyTypes.Rollup, DatabasePropertyTypes.CreatedBy, DatabasePropertyTypes.CreatedTime, DatabasePropertyTypes.LastEditedBy, DatabasePropertyTypes.LastEditedTime };
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
}