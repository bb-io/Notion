using Apps.Notion.Api;
using Apps.Notion.Constants;
using Apps.Notion.Invocables;
using Apps.Notion.Models.Entities;
using Apps.Notion.Models.Request.Block;
using Apps.Notion.Models.Request.Comment;
using Apps.Notion.Models.Response.Comment;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using Blackbird.Applications.Sdk.Utils.Extensions.String;
using RestSharp;

namespace Apps.Notion.Actions;

[ActionList]
public class CommentActions : NotionInvocable
{
    public CommentActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    [Action("List comments", Description = "List all block comments")]
    public async Task<ListCommentsResponse> ListComments([ActionParameter] BlockRequest input)
    {
        var endpoint = ApiEndpoints.Comments.SetQueryParameter("block_id", input.BlockId);
        var request = new NotionRequest(endpoint, Method.Get, Creds);

        var response = await Client.Paginate<CommentResponse>(request);
        var comments = response.Select(x => new CommentEntity(x)).ToArray();

        return new(comments);
    }

    [Action("Add comment", Description = "Add a new comment")]
    public async Task<CommentEntity> AddComment([ActionParameter] AddCommentInput input)
    {
        if (input.PageId is null && input.DiscussionId is null)
            throw new("You must specify one: either page ID or discussion ID");

        var request = new NotionRequest(ApiEndpoints.Comments, Method.Post, Creds)
            .WithJsonBody(new AddCommentRequest(input), JsonConfig.Settings);

        var response = await Client.ExecuteWithErrorHandling<CommentResponse>(request);
        return new(response);
    }
}