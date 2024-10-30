using Apps.NotionOAuth.Invocables;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.NotionOAuth.Actions;

[ActionList]
public class DebugActions(InvocationContext invocationContext) : NotionInvocable(invocationContext)
{
    [Action("Debug", Description = "Debug")]
    public List<AuthenticationCredentialsProvider> Debug()
    {
        return Creds.ToList();
    }
}