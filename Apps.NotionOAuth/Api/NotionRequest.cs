using Apps.NotionOAuth.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using RestSharp;

namespace Apps.NotionOAuth.Api;

public class NotionRequest : BlackBirdRestRequest
{
    public NotionRequest(string resource, Method method, IEnumerable<AuthenticationCredentialsProvider> credentialsProviders, string? apiVersion = null) 
        : base(resource, method, credentialsProviders)
    {
        this.AddHeader("Notion-Version", apiVersion ?? ApiConstants.DefaultApiVersion);
    }
    
    protected override void AddAuth(IEnumerable<AuthenticationCredentialsProvider> creds)
    {
        var token = creds.Get(CredsNames.AccessToken).Value;

        this.AddHeader("Authorization", $"Bearer {token}")
            .AddHeader("Notion-Version", ApiConstants.DefaultApiVersion);
    }
}