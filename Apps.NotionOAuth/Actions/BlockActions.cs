using Apps.NotionOAuth.Api;
using Apps.NotionOAuth.Constants;
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
public class BlockActions : NotionInvocable
{
    private const int MaxBlocksUploadSize = 100;
    
    public BlockActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }

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
        var blockChunks = blocks.Chunk(MaxBlocksUploadSize).ToArray();
        
        foreach (var blockChunk in blockChunks)
        {
            var endpoint = $"{ApiEndpoints.Blocks}/{blockId}/children";
            var request = new NotionRequest(endpoint, Method.Patch, Creds)
                .WithJsonBody(new ChildrenRequest()
                {
                    Children = blockChunk
                }, JsonConfig.Settings);

            await Client.ExecuteWithErrorHandling(request);
        }
    }
}