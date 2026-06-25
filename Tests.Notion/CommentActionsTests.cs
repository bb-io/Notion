using Tests.Notion.Base;
using Apps.NotionOAuth.Actions;
using Apps.NotionOAuth.Models.Request.Comment;

namespace Tests.Notion;

[TestClass]
public class CommentActionsTests : TestBase
{
    private CommentActions Actions => new(InvocationContext);

    [TestMethod]
    public async Task AddComment_IsSuccess()
    {
        // Arrange
        // Fill in the page ID before running this integration test.
        var input = new AddCommentInput
        {
            PageId = "",
            Text = new string('B', 2500) + Guid.NewGuid().ToString(),
        };

        // Act
        var response = await Actions.AddComment(input);

        // Assert
        PrintJsonResult(response);
        Assert.IsNotNull(response);
    }

    [TestMethod]
    public async Task AddCommentWithMention_IsSuccess()
    {
        // Arrange
        // Fill in the page ID and mentioned user ID before running this integration test.
        var input = new AddCommentInput
        {
            PageId = "",
            Text = "Please review this update",
            MentionedUserIds = new[] { "" }
        };

        // Act
        var response = await Actions.AddComment(input);

        // Assert
        PrintJsonResult(response);
        Assert.IsNotNull(response);
    }
}
