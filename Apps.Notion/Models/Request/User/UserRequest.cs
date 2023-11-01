using Apps.Notion.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Notion.Models.Request.User;

public class UserRequest
{
    [Display("User ID")]
    [DataSource(typeof(UserDataHandler))]
    public string UserId { get; set; }
}