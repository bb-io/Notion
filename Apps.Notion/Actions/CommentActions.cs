using Apps.Notion.Invocables;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Notion.Actions;

[ActionList]
public class CommentActions : NotionInvocable
{
    public CommentActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }
}