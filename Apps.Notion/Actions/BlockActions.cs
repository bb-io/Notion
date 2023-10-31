using Apps.Notion.Invocables;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Notion.Actions;

[ActionList]
public class BlockActions : NotionInvocable
{
    public BlockActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }
}