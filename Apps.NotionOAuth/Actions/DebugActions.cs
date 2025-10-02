using Apps.NotionOAuth.Invocables;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.NotionOAuth.Actions;

[ActionList("Miscellaneous")]
public class DebugActions(InvocationContext invocationContext)
    : NotionInvocable(invocationContext)
{
    [Action("Debug", Description = "Debug action")]
    public List<AuthenticationCredentialsProvider> Debug()
    {
        return Creds.ToList();
    }
}