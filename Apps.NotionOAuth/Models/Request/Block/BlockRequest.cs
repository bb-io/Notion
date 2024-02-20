using Blackbird.Applications.Sdk.Common;

namespace Apps.NotionOAuth.Models.Request.Block;

public class BlockRequest
{
    [Display("Block ID")]
    public string BlockId { get; set; }
}