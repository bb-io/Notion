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
        var input = new AddCommentInput
        {
            PageId = "3193f415-6cdc-800a-a050-fa6633ade1d0",
            Text = new string('B', 2500) + Guid.NewGuid().ToString(),
        };

        // Act
        var response = await Actions.AddComment(input);

        // Assert
        PrintJsonResult(response);
        Assert.IsNotNull(response);
    }
}
