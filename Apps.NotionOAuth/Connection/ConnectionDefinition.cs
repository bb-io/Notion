using Apps.NotionOAuth.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;

namespace Apps.NotionOAuth.Connection;

public class ConnectionDefinition : IConnectionDefinition
{
    public IEnumerable<ConnectionPropertyGroup> ConnectionPropertyGroups => new List<ConnectionPropertyGroup>
    {
        new()
        {
            Name = "OAuth2",
            AuthenticationType = ConnectionAuthenticationType.OAuth2,
            ConnectionUsage = ConnectionUsage.Actions,
            ConnectionProperties = new List<ConnectionProperty>
            {
            }
        }
    };

    public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(
        Dictionary<string, string> values)
    {
        yield return new(
            AuthenticationCredentialsRequestLocation.None,
            CredsNames.AccessToken,
            values[CredsNames.AccessToken]);
    }
}