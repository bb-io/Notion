using Apps.NotionOAuth.Api;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.NotionOAuth.Invocables;

public class NotionInvocable : BaseInvocable
{
    protected AuthenticationCredentialsProvider[] Creds =>
        InvocationContext.AuthenticationCredentialsProviders.ToArray();

    protected NotionClient Client { get; }

    public NotionInvocable(InvocationContext invocationContext) : base(invocationContext)
    {
        Client = new();
    }
}