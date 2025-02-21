using Apps.NotionOAuth.Constants;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication.OAuth2;
using Blackbird.Applications.Sdk.Common.Invocation;
using Microsoft.AspNetCore.WebUtilities;

namespace Apps.NotionOAuth.Connections.OAuth2;

public class OAuth2AuthorizationService(InvocationContext invocationContext)
    : BaseInvocable(invocationContext), IOAuth2AuthorizeService
{
    public string GetAuthorizationUrl(Dictionary<string, string> values)
    {
        string bridgeOauthUrl = $"{InvocationContext.UriInfo.BridgeServiceUrl.ToString().TrimEnd('/')}/oauth";
        var parameters = new Dictionary<string, string>
        {
            { "owner", ApiConstants.Owner },
            { "client_id", ApplicationConstants.ClientId},
            { "response_type", "code"},
            { "redirect_uri", $"{InvocationContext.UriInfo.BridgeServiceUrl.ToString().TrimEnd('/')}/AuthorizationCode" },
            { "state", values["state"] },
            { "authorization_url", Urls.OAuth},
            { "actual_redirect_uri", InvocationContext.UriInfo.AuthorizationCodeRedirectUri.ToString() },
        };

        return QueryHelpers.AddQueryString(bridgeOauthUrl, parameters);
    }
}