using Apps.NotionOAuth.Api;
using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Invocables;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.SDK.Extensions.FileManagement.Models.FileDataSourceItems;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.NotionOAuth.DataSourceHandlers
{
    public class PickerDataSourceHandler(InvocationContext invocationContext)
        : NotionInvocable(invocationContext), IAsyncFileDataSourceItemHandler
    {
        private const int DefaultPageSize = 50;
        private const int SearchScanPageSize = 100;

        private const string HomeVirtualId = "v:home";
        private const string HomeDisplay = "Notion";

        private const string LoadMorePrefix = "more:";
        private const string CursorMarker = "|c:";

        private const int MaxSearchScansPerRequest = 500;

        public async Task<IEnumerable<FileDataItem>> GetFolderContentAsync(
            FolderContentDataSourceContext context,
            CancellationToken cancellationToken)
        {
            var rawId = string.IsNullOrWhiteSpace(context.FolderId) ? HomeVirtualId : context.FolderId;
            var (containerId, cursor) = SplitCursor(rawId);

            if (containerId == HomeVirtualId)
            {
                var (results, nextCursor) = await SearchTopLevelAsync(cursor, DefaultPageSize, cancellationToken);
                return BuildResultWithLoadMore(containerId, results.Select(ToPickerFolder).WhereNotNull(), nextCursor);
            }

            if (containerId.StartsWith(LoadMorePrefix, StringComparison.Ordinal))
            {
                var target = containerId.Substring(LoadMorePrefix.Length);
                var (targetId, targetCursor) = SplitCursor(target);

                return await GetFolderContentAsync(
                    new FolderContentDataSourceContext
                    {
                        FolderId = $"{targetId}{(targetCursor is null ? "" : $"{CursorMarker}{targetCursor}")}"
                    },
                    cancellationToken);
            }

            var kind = await ResolveObjectTypeAsync(containerId, cancellationToken);

            if (kind == NotionKind.Page)
            {
                var (children, next) = await GetPageChildrenAsync(containerId, cursor, DefaultPageSize, cancellationToken);
                var mapped = children.Select(ToPickerFolder).WhereNotNull();
                return BuildResultWithLoadMore(containerId, mapped, next);
            }

            if (kind == NotionKind.Database)
            {
                var (items, next) = await QueryDatabaseAsync(containerId, cursor, DefaultPageSize, cancellationToken);
                var mapped = items.Select(ToPickerFolder).WhereNotNull();
                return BuildResultWithLoadMore(containerId, mapped, next);
            }

            if (kind == NotionKind.DataSource)
            {
                var (items, next) = await QueryDataSourceAsync(containerId, cursor, DefaultPageSize, cancellationToken);
                var mapped = items.Select(ToPickerFolder).WhereNotNull();
                return BuildResultWithLoadMore(containerId, mapped, next);
            }

            return new List<FileDataItem>
            {
                new Folder { Id = HomeVirtualId, DisplayName = HomeDisplay, IsSelectable = false }
            };
        }

        public async Task<IEnumerable<FolderPathItem>> GetFolderPathAsync(
            FolderPathDataSourceContext context,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(context?.FileDataItemId))
                return new List<FolderPathItem> { new() { DisplayName = HomeDisplay, Id = HomeVirtualId } };

            var (rawId, _) = SplitCursor(context.FileDataItemId);

            if (rawId == HomeVirtualId)
                return new List<FolderPathItem> { new() { DisplayName = HomeDisplay, Id = HomeVirtualId } };

            try
            {
                var path = new List<FolderPathItem> { new() { DisplayName = HomeDisplay, Id = HomeVirtualId } };
                var stack = new Stack<FolderPathItem>();

                var current = rawId;
                var guard = 0;

                while (!string.IsNullOrWhiteSpace(current) && guard++ < 25)
                {
                    if (current == HomeVirtualId)
                        break;

                    var kind = await ResolveObjectTypeAsync(current, cancellationToken);

                    if (kind == NotionKind.Page)
                    {
                        var page = await TryRetrievePageAsync(current, cancellationToken);

                        stack.Push(new FolderPathItem
                        {
                            Id = current,
                            DisplayName = $"{GetTitleFromPage(page)} (Page)"
                        });

                        var parent = page?["parent"] as JObject;
                        var next = ResolveParentToId(parent);
                        if (string.IsNullOrWhiteSpace(next) || next == HomeVirtualId)
                            break;

                        current = next;
                        continue;
                    }

                    if (kind == NotionKind.Database)
                    {
                        var db = await TryRetrieveDatabaseAsync(current, cancellationToken);

                        stack.Push(new FolderPathItem
                        {
                            Id = current,
                            DisplayName = $"{GetTitleFromDatabase(db)} (Database)"
                        });

                        var parent = db?["parent"] as JObject;
                        var next = ResolveParentToId(parent);
                        if (string.IsNullOrWhiteSpace(next) || next == HomeVirtualId)
                            break;

                        current = next;
                        continue;
                    }

                    if (kind == NotionKind.DataSource)
                    {
                        var ds = await TryRetrieveDataSourceAsync(current, cancellationToken);

                        stack.Push(new FolderPathItem
                        {
                            Id = current,
                            DisplayName = $"{GetTitleFromDataSource(ds)} (Data source)"
                        });

                        var parent = ds?["parent"] as JObject;
                        var next = ResolveParentToId(parent);
                        if (string.IsNullOrWhiteSpace(next) || next == HomeVirtualId)
                            break;

                        current = next;
                        continue;
                    }

                    break;
                }

                path.AddRange(stack);
                return path;
            }
            catch
            {
                return new List<FolderPathItem> { new() { DisplayName = HomeDisplay, Id = HomeVirtualId } };
            }
        }

        private async Task<(List<JObject> Items, string? NextCursor)> SearchTopLevelAsync(
            string? startCursor,
            int targetCount,
            CancellationToken ct)
        {
            var pages = await SearchAllByObjectAsync("page", apiVersion: null, ct);
            var dataSources = await SearchAllByObjectAsync("data_source", apiVersion: null, ct);

            var databases = await SearchAllByObjectAsync("database", apiVersion: ApiConstants.NotLatestApiVersion, ct);

            var merged = pages
                .Concat(dataSources)
                .Concat(databases)
                .Where(IsTopLevelWorkspaceItem)
                .GroupBy(x => x.Value<string>("id") ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .Select(g => g.First())
                .OrderByDescending(x => x.Value<DateTime?>("last_edited_time") ?? DateTime.MinValue)
                .Take(targetCount)
                .ToList();

            return (merged, null);
        }

        private async Task<List<JObject>> SearchAllByObjectAsync(
            string objectValue,
            string? apiVersion,
            CancellationToken ct)
        {
            var collected = new List<JObject>();
            string? cursor = null;
            var guard = 0;

            while (guard++ < MaxSearchScansPerRequest)
            {
                var (items, nextCursor) = await SearchPagedAsync(objectValue, cursor, apiVersion, ct);
                collected.AddRange(items);

                if (string.IsNullOrWhiteSpace(nextCursor))
                    break;

                cursor = nextCursor;
            }

            return collected;
        }

        private async Task<(List<JObject> Items, string? NextCursor)> SearchPagedAsync(
            string objectValue,
            string? startCursor,
            string? apiVersion,
            CancellationToken ct)
        {
            var body = new JObject
            {
                ["page_size"] = SearchScanPageSize,
                ["sort"] = new JObject
                {
                    ["direction"] = "descending",
                    ["timestamp"] = "last_edited_time"
                },
                ["filter"] = new JObject
                {
                    ["property"] = "object",
                    ["value"] = objectValue
                }
            };

            if (!string.IsNullOrWhiteSpace(startCursor))
                body["start_cursor"] = startCursor;

            var request = new NotionRequest(ApiEndpoints.Search, Method.Post, Creds, apiVersion)
                .WithJsonBody(body);

            var resp = await Client.ExecuteWithErrorHandling<JObject>(request);
            return ParsePagedResults(resp);
        }

        private static bool IsTopLevelWorkspaceItem(JObject item)
        {
            var parent = item["parent"] as JObject;
            if (parent is null) return false;

            var type = parent.Value<string>("type");
            if (!string.Equals(type, "workspace", StringComparison.OrdinalIgnoreCase))
                return false;

            var ws = parent.Value<bool?>("workspace");
            return ws is null || ws == true;
        }

        private async Task<(List<JObject> Items, string? NextCursor)> GetPageChildrenAsync(
            string pageId,
            string? startCursor,
            int pageSize,
            CancellationToken ct)
        {
            var endpoint = $"{ApiEndpoints.Blocks}/{pageId}/children?page_size={pageSize}";
            if (!string.IsNullOrWhiteSpace(startCursor))
                endpoint += $"&start_cursor={Uri.EscapeDataString(startCursor)}";

            var request = new NotionRequest(endpoint, Method.Get, Creds);
            var resp = await Client.ExecuteWithErrorHandling<JObject>(request);

            var (items, nextCursor) = ParsePagedResults(resp);

            var filtered = items
                .Where(b =>
                {
                    var type = b.Value<string>("type") ?? string.Empty;
                    return type.Equals("child_page", StringComparison.OrdinalIgnoreCase)
                           || type.Equals("child_database", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

            return (filtered, nextCursor);
        }

        private async Task<(List<JObject> Items, string? NextCursor)> QueryDatabaseAsync(
            string databaseId,
            string? startCursor,
            int pageSize,
            CancellationToken ct)
        {
            var endpoint = $"{ApiEndpoints.Databases}/{databaseId}/query";

            var body = new JObject { ["page_size"] = pageSize };
            if (!string.IsNullOrWhiteSpace(startCursor))
                body["start_cursor"] = startCursor;

            var request = new NotionRequest(endpoint, Method.Post, Creds)
                .WithJsonBody(body);

            var resp = await Client.ExecuteWithErrorHandling<JObject>(request);
            return ParsePagedResults(resp);
        }

        private async Task<(List<JObject> Items, string? NextCursor)> QueryDataSourceAsync(
            string dataSourceId,
            string? startCursor,
            int pageSize,
            CancellationToken ct)
        {
            var endpoint = $"{ApiEndpoints.DataSources}/{dataSourceId}/query";

            var body = new JObject { ["page_size"] = pageSize };
            if (!string.IsNullOrWhiteSpace(startCursor))
                body["start_cursor"] = startCursor;

            var request = new NotionRequest(endpoint, Method.Post, Creds)
                .WithJsonBody(body);

            var resp = await Client.ExecuteWithErrorHandling<JObject>(request);
            return ParsePagedResults(resp);
        }

        private async Task<NotionKind> ResolveObjectTypeAsync(string id, CancellationToken ct)
        {
            if (id == HomeVirtualId) return NotionKind.Virtual;
            if (id.StartsWith(LoadMorePrefix, StringComparison.Ordinal)) return NotionKind.Virtual;
            if (id.Length < 16) return NotionKind.Unknown;

            if (await ExistsPageAsync(id, ct)) return NotionKind.Page;
            if (await ExistsDatabaseAsync(id, ct)) return NotionKind.Database;
            if (await ExistsDataSourceAsync(id, ct)) return NotionKind.DataSource;

            return NotionKind.Unknown;
        }

        private async Task<bool> ExistsPageAsync(string id, CancellationToken ct)
        {
            try { return await TryRetrievePageAsync(id, ct) is not null; }
            catch { return false; }
        }

        private async Task<bool> ExistsDatabaseAsync(string id, CancellationToken ct)
        {
            try { return await TryRetrieveDatabaseAsync(id, ct) is not null; }
            catch { return false; }
        }

        private async Task<bool> ExistsDataSourceAsync(string id, CancellationToken ct)
        {
            try { return await TryRetrieveDataSourceAsync(id, ct) is not null; }
            catch { return false; }
        }

        private async Task<JObject?> TryRetrievePageAsync(string pageId, CancellationToken ct)
        {
            var endpoint = $"{ApiEndpoints.Pages}/{pageId}";
            var request = new NotionRequest(endpoint, Method.Get, Creds);
            return await Client.ExecuteWithErrorHandling<JObject>(request);
        }

        private async Task<JObject?> TryRetrieveDatabaseAsync(string databaseId, CancellationToken ct)
        {
            var endpoint = $"{ApiEndpoints.Databases}/{databaseId}";
            var request = new NotionRequest(endpoint, Method.Get, Creds);
            return await Client.ExecuteWithErrorHandling<JObject>(request);
        }

        private async Task<JObject?> TryRetrieveDataSourceAsync(string dataSourceId, CancellationToken ct)
        {
            var endpoint = $"{ApiEndpoints.DataSources}/{dataSourceId}";
            var request = new NotionRequest(endpoint, Method.Get, Creds);
            return await Client.ExecuteWithErrorHandling<JObject>(request);
        }

        private Folder? ToPickerFolder(JObject obj)
        {
            var notionObject = obj.Value<string>("object") ?? string.Empty;

            if (notionObject.Equals("page", StringComparison.OrdinalIgnoreCase))
            {
                var id = obj.Value<string>("id");
                if (string.IsNullOrWhiteSpace(id)) return null;

                return new Folder
                {
                    Id = id,
                    DisplayName = $"{GetTitleFromPage(obj)} (Page)",
                    IsSelectable = true
                };
            }

            if (notionObject.Equals("database", StringComparison.OrdinalIgnoreCase))
            {
                var id = obj.Value<string>("id");
                if (string.IsNullOrWhiteSpace(id)) return null;

                return new Folder
                {
                    Id = id,
                    DisplayName = $"{GetTitleFromDatabase(obj)} (Database)",
                    IsSelectable = true
                };
            }

            if (notionObject.Equals("data_source", StringComparison.OrdinalIgnoreCase))
            {
                var id = obj.Value<string>("id");
                if (string.IsNullOrWhiteSpace(id)) return null;

                return new Folder
                {
                    Id = id,
                    DisplayName = $"{GetTitleFromDataSource(obj)} (Data source)",
                    IsSelectable = true
                };
            }

            if (notionObject.Equals("block", StringComparison.OrdinalIgnoreCase))
            {
                var type = obj.Value<string>("type") ?? string.Empty;
                var blockId = obj.Value<string>("id");
                if (string.IsNullOrWhiteSpace(blockId)) return null;

                if (type.Equals("child_page", StringComparison.OrdinalIgnoreCase))
                {
                    var title = obj.SelectToken("child_page.title")?.Value<string>() ?? "Untitled";
                    return new Folder
                    {
                        Id = blockId,
                        DisplayName = $"{title} (Page)",
                        IsSelectable = true
                    };
                }

                if (type.Equals("child_database", StringComparison.OrdinalIgnoreCase))
                {
                    var title = obj.SelectToken("child_database.title")?.Value<string>() ?? "Untitled";
                    return new Folder
                    {
                        Id = blockId,
                        DisplayName = $"{title} (Database)",
                        IsSelectable = true
                    };
                }
            }

            return null;
        }

        private static string GetTitleFromPage(JObject? page)
        {
            var props = page?["properties"] as JObject;
            if (props is null) return "Untitled";

            foreach (var prop in props.Properties())
            {
                var pObj = prop.Value as JObject;
                var type = pObj?.Value<string>("type");
                if (!"title".Equals(type, StringComparison.OrdinalIgnoreCase)) continue;

                var titleArr = pObj?["title"] as JArray;
                var joined = JoinPlainText(titleArr);
                return string.IsNullOrWhiteSpace(joined) ? "Untitled" : joined;
            }

            return "Untitled";
        }

        private static string GetTitleFromDatabase(JObject? db)
        {
            var titleArr = db?["title"] as JArray;
            var joined = JoinPlainText(titleArr);
            return string.IsNullOrWhiteSpace(joined) ? "Untitled" : joined;
        }

        private static string GetTitleFromDataSource(JObject? ds)
        {
            var name = ds?.Value<string>("name");
            if (!string.IsNullOrWhiteSpace(name)) return name;

            var titleArr = ds?["title"] as JArray;
            var joined = JoinPlainText(titleArr);
            return string.IsNullOrWhiteSpace(joined) ? "Untitled" : joined;
        }

        private static string JoinPlainText(JArray? richTextArray)
        {
            if (richTextArray is null) return string.Empty;

            var parts = richTextArray
                .OfType<JObject>()
                .Select(x => x.Value<string>("plain_text"))
                .Where(s => !string.IsNullOrWhiteSpace(s));

            return string.Concat(parts);
        }

        private static string? ResolveParentToId(JObject? parent)
        {
            if (parent is null) return null;

            var type = parent.Value<string>("type");
            if (string.IsNullOrWhiteSpace(type)) return null;

            if (type.Equals("workspace", StringComparison.OrdinalIgnoreCase))
                return HomeVirtualId;

            if (type.Equals("page_id", StringComparison.OrdinalIgnoreCase))
                return parent.Value<string>("page_id");

            if (type.Equals("database_id", StringComparison.OrdinalIgnoreCase))
                return parent.Value<string>("database_id");

            if (type.Equals("data_source_id", StringComparison.OrdinalIgnoreCase))
                return parent.Value<string>("data_source_id");

            return null;
        }

        private static (List<JObject> Items, string? NextCursor) ParsePagedResults(JObject? resp)
        {
            var items = (resp?["results"] as JArray)?.OfType<JObject>().ToList() ?? new List<JObject>();
            var hasMore = resp?.Value<bool?>("has_more") == true;
            var nextCursor = hasMore ? resp?.Value<string>("next_cursor") : null;
            return (items, nextCursor);
        }

        private static IEnumerable<FileDataItem> BuildResultWithLoadMore(
            string containerId,
            IEnumerable<Folder> items,
            string? nextCursor)
        {
            var list = items.Cast<FileDataItem>().ToList();

            if (!string.IsNullOrWhiteSpace(nextCursor))
            {
                list.Add(new Folder
                {
                    Id = $"{LoadMorePrefix}{containerId}{CursorMarker}{nextCursor}",
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

        private enum NotionKind
        {
            Virtual,
            Page,
            Database,
            DataSource,
            Unknown
        }
    }

    internal static class EnumerableExtensions
    {
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
            => source.Where(x => x is not null)!;
    }
}
