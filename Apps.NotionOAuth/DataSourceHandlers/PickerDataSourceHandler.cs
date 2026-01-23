using Apps.NotionOAuth.Api;
using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Invocables;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.SDK.Extensions.FileManagement.Models.FileDataSourceItems;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.NotionOAuth.DataSourceHandlers;

public class PickerDataSourceHandler(InvocationContext ctx)
    : NotionInvocable(ctx), IAsyncFileDataSourceItemHandler
{
    private const string HomeId = "v:home";
    private const string HomeName = "Notion";

    private const string PagesRootId = "v:pages";
    private const string DbRootId = "v:databases";

    private const string PagePrefix = "p:";
    private const string DbPrefix = "db:";
    private const string DsPrefix = "ds:";
    private const string CursorMarker = "|c:";

    private const int PageSize = 50;

    public async Task<IEnumerable<FileDataItem>> GetFolderContentAsync(
        FolderContentDataSourceContext context,
        CancellationToken ct)
    {
        var raw = string.IsNullOrWhiteSpace(context.FolderId) ? HomeId : context.FolderId;
        var (id, cursor) = SplitCursor(raw);

        if (id == HomeId)
        {
            return new FileDataItem[]
            {
                new Folder { Id = PagesRootId, DisplayName = "Pages", IsSelectable = false },
                new Folder { Id = DbRootId, DisplayName = "Databases", IsSelectable = false },
            };
        }

        if (id == PagesRootId)
        {
            var (pages, next) = await SearchAsync("page", cursor, PageSize, ct);
            var topLevel = pages.Where(IsWorkspaceParent);

            var items = topLevel.Select(p => new Folder
            {
                Id = Pack(PagePrefix, p.Value<string>("id")!),
                DisplayName = GetPageTitle(p),
                IsSelectable = true
            });

            return WithLoadMore(PagesRootId, items, next);
        }

        if (id == DbRootId)
        {
            var (dataSources, next) = await SearchAsync("data_source", cursor, PageSize, ct);

            var grouped = dataSources
                .Select(ds => new
                {
                    Ds = ds,
                    DbId = ds.SelectToken("parent.database_id")?.Value<string>()
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.DbId))
                .GroupBy(x => x.DbId!, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.Key)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var items = grouped.Select(dbId => new Folder
            {
                Id = Pack(DbPrefix, dbId),
                DisplayName = $"Database {Short(dbId)}",
                IsSelectable = false
            });

            return WithLoadMore(DbRootId, items, next);
        }

        if (id.StartsWith(DbPrefix, StringComparison.Ordinal))
        {
            var dbId = id.Substring(DbPrefix.Length);
            var db = await RetrieveDatabase(dbId, ct);
            var title = GetDatabaseTitle(db);

            var dsArr = db["data_sources"] as JArray ?? new JArray();
            var dsItems = dsArr
                .OfType<JObject>()
                .Select(x => new Folder
                {
                    Id = Pack(DsPrefix, x.Value<string>("id")!),
                    DisplayName = $"{x.Value<string>("name") ?? "Data source"} ({title})",
                    IsSelectable = true
                });

            return dsItems.Cast<FileDataItem>().ToList();
        }

        if (id.StartsWith(DsPrefix, StringComparison.Ordinal))
        {
            var dsId = id.Substring(DsPrefix.Length);
            var (rows, next) = await QueryDataSource(dsId, cursor, PageSize, ct);

            var items = rows.Select(p => new Folder
            {
                Id = Pack(PagePrefix, p.Value<string>("id")!),
                DisplayName = GetPageTitle(p),
                IsSelectable = true
            });

            return WithLoadMore(Pack(DsPrefix, dsId), items, next);
        }

        if (id.StartsWith(PagePrefix, StringComparison.Ordinal))
        {
            var pageId = id.Substring(PagePrefix.Length);
            var (children, next) = await GetBlockChildren(pageId, cursor, PageSize, ct);

            var folders = children
                .Where(b =>
                {
                    var type = b.Value<string>("type");
                    return string.Equals(type, "child_page", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(type, "child_database", StringComparison.OrdinalIgnoreCase);
                })
                .Select(b =>
                {
                    var type = b.Value<string>("type")!;
                    var childId = b.Value<string>("id")!;
                    if (type.Equals("child_page", StringComparison.OrdinalIgnoreCase))
                        return new Folder
                        {
                            Id = Pack(PagePrefix, childId),
                            DisplayName = b.SelectToken("child_page.title")?.Value<string>() ?? "Untitled",
                            IsSelectable = true
                        };

                    return new Folder
                    {
                        Id = Pack(DbPrefix, childId),
                        DisplayName = b.SelectToken("child_database.title")?.Value<string>() ?? "Untitled database",
                        IsSelectable = false
                    };
                });

            return WithLoadMore(Pack(PagePrefix, pageId), folders, next);
        }

        return new[] { new Folder { Id = HomeId, DisplayName = HomeName, IsSelectable = false } };
    }

    public async Task<IEnumerable<FolderPathItem>> GetFolderPathAsync(
        FolderPathDataSourceContext context,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(context?.FileDataItemId))
            return new[] { new FolderPathItem { Id = HomeId, DisplayName = HomeName } };

        var (raw, _) = SplitCursor(context.FileDataItemId);

        if (raw is HomeId or PagesRootId or DbRootId)
            return new[] { new FolderPathItem { Id = HomeId, DisplayName = HomeName } };

        var path = new List<FolderPathItem> { new() { Id = HomeId, DisplayName = HomeName } };
        var stack = new Stack<FolderPathItem>();

        var current = raw;
        for (var guard = 0; guard < 25 && !string.IsNullOrWhiteSpace(current); guard++)
        {
            if (current.StartsWith(PagePrefix, StringComparison.Ordinal))
            {
                var pageId = current.Substring(PagePrefix.Length);
                var page = await RetrievePage(pageId, ct);
                stack.Push(new FolderPathItem { Id = current, DisplayName = GetPageTitle(page) });

                current = ParentToPickerId(page["parent"] as JObject);
                continue;
            }

            if (current.StartsWith(DsPrefix, StringComparison.Ordinal))
            {
                var dsId = current.Substring(DsPrefix.Length);
                var ds = await RetrieveDataSource(dsId, ct);
                stack.Push(new FolderPathItem { Id = current, DisplayName = ds.Value<string>("name") ?? "Data source" });

                current = ParentToPickerId(ds["parent"] as JObject);
                continue;
            }

            if (current.StartsWith(DbPrefix, StringComparison.Ordinal))
            {
                var dbId = current.Substring(DbPrefix.Length);
                var db = await RetrieveDatabase(dbId, ct);
                stack.Push(new FolderPathItem { Id = current, DisplayName = GetDatabaseTitle(db) });

                current = ParentToPickerId(db["parent"] as JObject);
                continue;
            }

            break;
        }

        path.AddRange(stack);
        return path;
    }

    private async Task<(List<JObject> Items, string? NextCursor)> SearchAsync(
        string objectValue, string? startCursor, int pageSize, CancellationToken ct)
    {
        var body = new JObject
        {
            ["page_size"] = pageSize,
            ["sort"] = new JObject { ["direction"] = "descending", ["timestamp"] = "last_edited_time" },
            ["filter"] = new JObject { ["property"] = "object", ["value"] = objectValue }
        };
        if (!string.IsNullOrWhiteSpace(startCursor)) body["start_cursor"] = startCursor;

        var req = new NotionRequest(ApiEndpoints.Search, Method.Post, Creds, apiVersion: "2025-09-03")
            .WithJsonBody(body);

        var resp = await Client.ExecuteWithErrorHandling<JObject>(req);
        return ParsePaged(resp);
    }

    private async Task<(List<JObject> Items, string? NextCursor)> QueryDataSource(
        string dataSourceId, string? startCursor, int pageSize, CancellationToken ct)
    {
        var endpoint = $"{ApiEndpoints.DataSources}/{dataSourceId}/query?page_size={pageSize}&filter_properties[]=title";
        if (!string.IsNullOrWhiteSpace(startCursor))
            endpoint += $"&start_cursor={Uri.EscapeDataString(startCursor)}";

        var req = new NotionRequest(endpoint, Method.Post, Creds, apiVersion: "2025-09-03")
            .WithJsonBody(new JObject());
        var resp = await Client.ExecuteWithErrorHandling<JObject>(req);
        return ParsePaged(resp);
    }

    private async Task<(List<JObject> Items, string? NextCursor)> GetBlockChildren(
        string blockId, string? startCursor, int pageSize, CancellationToken ct)
    {
        var endpoint = $"{ApiEndpoints.Blocks}/{blockId}/children?page_size={pageSize}";
        if (!string.IsNullOrWhiteSpace(startCursor))
            endpoint += $"&start_cursor={Uri.EscapeDataString(startCursor)}";

        var req = new NotionRequest(endpoint, Method.Get, Creds, apiVersion: "2025-09-03");
        var resp = await Client.ExecuteWithErrorHandling<JObject>(req);
        return ParsePaged(resp);
    }

    private Task<JObject> RetrievePage(string pageId, CancellationToken ct)
        => Client.ExecuteWithErrorHandling<JObject>(
            new NotionRequest($"{ApiEndpoints.Pages}/{pageId}", Method.Get, Creds, apiVersion: "2025-09-03"));

    private Task<JObject> RetrieveDatabase(string dbId, CancellationToken ct)
        => Client.ExecuteWithErrorHandling<JObject>(
            new NotionRequest($"{ApiEndpoints.Databases}/{dbId}", Method.Get, Creds, apiVersion: "2025-09-03"));

    private Task<JObject> RetrieveDataSource(string dsId, CancellationToken ct)
        => Client.ExecuteWithErrorHandling<JObject>(
            new NotionRequest($"{ApiEndpoints.DataSources}/{dsId}", Method.Get, Creds, apiVersion: "2025-09-03"));

    private static (List<JObject> Items, string? NextCursor) ParsePaged(JObject? resp)
    {
        var items = (resp?["results"] as JArray)?.OfType<JObject>().ToList() ?? new();
        var hasMore = resp?.Value<bool?>("has_more") == true;
        var next = hasMore ? resp?.Value<string>("next_cursor") : null;
        return (items, next);
    }

    private static IEnumerable<FileDataItem> WithLoadMore(string containerId, IEnumerable<Folder> items, string? nextCursor)
    {
        var list = items.Cast<FileDataItem>().ToList();
        if (!string.IsNullOrWhiteSpace(nextCursor))
        {
            list.Add(new Folder
            {
                Id = $"{containerId}{CursorMarker}{nextCursor}",
                DisplayName = "Load more…",
                IsSelectable = false
            });
        }
        return list;
    }

    private static (string Id, string? Cursor) SplitCursor(string raw)
    {
        var idx = raw.IndexOf(CursorMarker, StringComparison.Ordinal);
        if (idx < 0) return (raw, null);
        var id = raw[..idx];
        var cursor = raw[(idx + CursorMarker.Length)..];
        return (id, string.IsNullOrWhiteSpace(cursor) ? null : cursor);
    }

    private static string Pack(string prefix, string id) => $"{prefix}{id}";
    private static string Short(string id) => id.Replace("-", "").Length > 6 ? id.Replace("-", "")[..6] : id;

    private static bool IsWorkspaceParent(JObject item)
        => string.Equals(item.SelectToken("parent.type")?.Value<string>(), "workspace", StringComparison.OrdinalIgnoreCase);

    private static string ParentToPickerId(JObject? parent)
    {
        var type = parent?.Value<string>("type");
        if (string.IsNullOrWhiteSpace(type)) return HomeId;

        return type switch
        {
            "workspace" => HomeId,
            "page_id" => Pack(PagePrefix, parent!.Value<string>("page_id")!),
            "database_id" => Pack(DbPrefix, parent!.Value<string>("database_id")!),
            "data_source_id" => Pack(DsPrefix, parent!.Value<string>("data_source_id")!),
            _ => HomeId
        };
    }

    private static string GetDatabaseTitle(JObject? db)
    {
        var titleArr = db?["title"] as JArray;
        return JoinPlain(titleArr) is { Length: > 0 } t ? t : "Untitled database";
    }

    private static string GetPageTitle(JObject? page)
    {
        var props = page?["properties"] as JObject;
        if (props is not null)
        {
            foreach (var prop in props.Properties())
            {
                var pObj = prop.Value as JObject;
                if (!string.Equals(pObj?.Value<string>("type"), "title", StringComparison.OrdinalIgnoreCase))
                    continue;

                var titleArr = pObj["title"] as JArray;
                var joined = JoinPlain(titleArr);
                if (!string.IsNullOrWhiteSpace(joined)) return joined;
            }
        }

        var directTitle = JoinPlain(page?["title"] as JArray);
        return string.IsNullOrWhiteSpace(directTitle) ? "Untitled" : directTitle;
    }

    private static string JoinPlain(JArray? richTextArray)
        => richTextArray is null
            ? string.Empty
            : string.Concat(richTextArray.OfType<JObject>()
                .Select(x => x.Value<string>("plain_text"))
                .Where(s => !string.IsNullOrWhiteSpace(s)));
}
