using Apps.NotionOAuth.Api;
using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Extensions;
using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Entities;
using Apps.NotionOAuth.Models.Request.Block;
using Apps.NotionOAuth.Models.Response.Block;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.NotionOAuth.Actions;

[ActionList]
public class BlockActions(InvocationContext invocationContext) : NotionInvocable(invocationContext)
{
    private const int MaxBlocksUploadSize = 100;

    [Action("Get block", Description = "Get details of a specific block")]
    public async Task<BlockEntity> GetBlock([ActionParameter] BlockRequest input)
    {
        var endpoint = $"{ApiEndpoints.Blocks}/{input.BlockId}";
        var request = new NotionRequest(endpoint, Method.Get, Creds);

        var response = await Client.ExecuteWithErrorHandling<BlockResponse>(request);
        var response2 = await Client.ExecuteWithErrorHandling(request);
        return new(response);
    }

    [Action("List block children", Description = "List children of a specific block")]
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
        var blockChunks = new List<List<JObject>>();
        var currentChunk = new List<JObject>();

        foreach (var block in blocks)
        {
            if (block["type"]?.ToString() == "child_page")
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
        
        foreach (var blockChunk in blockChunks)
        {
            var hasChildPage = blockChunk.Any(x => x["type"]?.ToString() == "child_page");

            if (hasChildPage)
            {
                foreach (var page in blockChunk)
                {
                    if (page.TryGetValue("type", out _))
                    {
                        page.Remove("type");
                    }
                    
                    var request = new NotionRequest(ApiEndpoints.Pages, Method.Post, Creds)
                        .WithJsonBody(page, JsonConfig.Settings);

                    await Client.ExecuteWithErrorHandling(request);
                }
            }
            else
            {
                var endpoint = $"{ApiEndpoints.Blocks}/{blockId}/children";
                var request = new NotionRequest(endpoint, Method.Patch, Creds)
                    .WithJsonBody(new ChildrenRequest
                    {
                        Children = blockChunk.ToArray()
                    }, JsonConfig.Settings);

                await Client.ExecuteWithErrorHandling(request);
            }
        }
    }
}