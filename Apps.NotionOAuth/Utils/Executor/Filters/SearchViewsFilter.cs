namespace Apps.NotionOAuth.Utils.Executor.Filters;

public record SearchViewsFilter
{
    public string? DatabaseId { get; set; }

    public string? DataSourceId { get; set; }

    public string? ViewNameContains { get; set; }
}
