using Apps.NotionOAuth.Api;
using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Entities;
using Apps.NotionOAuth.Models.Request.Block;
using Apps.NotionOAuth.Models.Response.Block;
using Apps.NotionOAuth.Models.Response.DataBase;
using Apps.NotionOAuth.Models.Response.Page;
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

    internal async Task AppendBlockChildren(string blockId, JObject[] blocks)
    {
        var blockChunks = ChunkBlocks(blocks);

        foreach (var blockChunk in blockChunks)
        {
            if (blockChunk.Any(x => x["type"]?.ToString() == "child_page"))
            {
                await ProcessChildPages(blockId, blockChunk);
            }
            else if (blockChunk.Any(x => x["object"]?.ToString() == "database"))
            {
                await ProcessDatabases(blockId, blockChunk);
            }
            else
            {
                await ProcessBlocks(blockId, blockChunk);
            }
        }
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

    private async Task ProcessChildPages(string blockId, List<JObject> blockChunk)
    {
        foreach (var page in blockChunk)
        {
            RemoveUnnecessaryProperties(page, "type", "child_page");

            var children = page["children"]?.ToObject<JObject[]>()
                           ?? Array.Empty<JObject>();
            page.Remove("children");

            var parent = page["parent"]?.ToObject<JObject>()
                         ?? throw new InvalidOperationException($"Child page [{blockId}] must have a parent");

            if (parent.TryGetValue("page_id", out _))
            {
                parent["page_id"] = blockId;
                page["parent"] = parent;
            }
            else if (parent.TryGetValue("database_id", out _))
            {
                parent["database_id"] = blockId;
                page["parent"] = parent;
            }

            RemoveDisallowedGeneratedProperties(page);
            RemoveStatusProperties(page);
            
            var request = new NotionRequest(ApiEndpoints.Pages, Method.Post, Creds)
                .WithJsonBody(page, JsonConfig.Settings);
            
            var pageResponse = await Client.ExecuteWithErrorHandling<PageResponse>(request);
            if (children.Length > 0)
            {
                await AppendBlockChildren(pageResponse.Id, children.ToArray());
            }
        }
    }

    private async Task ProcessDatabases(string blockId, List<JObject> blockChunk)
    {
        foreach (var database in blockChunk)
        {
            var parent = database["parent"]?.ToObject<JObject>()
                         ?? throw new InvalidOperationException("Databases must have a parent");

            if (parent.TryGetValue("page_id", out _))
            {
                parent["page_id"] = blockId;
                database["parent"] = parent;
            }
            
            var children = database["children"]?.ToObject<JObject[]>()
                           ?? throw new InvalidOperationException("Child database must have children");
            
            database.Remove("children");
            
            RemoveGroupsFromProperties(database);
            FixStatusProperties(database);

            var request = new NotionRequest(ApiEndpoints.Databases, Method.Post, Creds)
                .WithJsonBody(database, JsonConfig.Settings);
            var createdDatabase = await Client.ExecuteWithErrorHandling<DatabaseResponse>(request);
            await AppendBlockChildren(createdDatabase.Id, children.ToArray());
        }
    }

    private async Task ProcessBlocks(string blockId, List<JObject> blockChunk)
    {
        var endpoint = $"{ApiEndpoints.Blocks}/{blockId}/children";
        var request = new NotionRequest(endpoint, Method.Patch, Creds)
            .WithJsonBody(new ChildrenRequest
            {
                Children = blockChunk.ToArray()
            }, JsonConfig.Settings);

        await Client.ExecuteWithErrorHandling(request);
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
                if (property.Value is JObject propObj && (string.Equals((string)propObj["type"], "status",
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
            var disallowedTypes = new HashSet<string> { "rollup", "created_by", "created_time", "last_edited_by", "last_edited_time" };
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
                               string.Equals((string)typeToken, "status", StringComparison.OrdinalIgnoreCase))
                .Select(prop => prop.Name)
                .ToList();
            foreach (var propName in propsToRemove)
            {
                properties.Remove(propName);
            }
        }
    }
}