using Apps.NotionOAuth.Models.Request.Comment;

namespace Tests.Notion;

[TestClass]
public class AddCommentRequestTests
{
    [TestMethod]
    public void AddCommentRequest_ParsesInlineMentionToken()
    {
        var request = new AddCommentRequest(new AddCommentInput
        {
            PageId = "page-id",
            Text = "Hello <mention-user url=\"user-id\">Ada Lovelace</mention-user> world"
        });

        Assert.IsNotNull(request.RichText);
        Assert.AreEqual(3, request.RichText.Count);
        Assert.AreEqual("Hello ", request.RichText[0].Text?.Content);
        Assert.AreEqual("mention", request.RichText[1].Type);
        Assert.AreEqual("user", request.RichText[1].Mention?["type"]?.ToString());
        Assert.AreEqual("user-id", request.RichText[1].Mention?["user"]?["id"]?.ToString());
        Assert.AreEqual(" world", request.RichText[2].Text?.Content);
    }
}
