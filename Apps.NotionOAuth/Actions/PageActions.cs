using System.Net.Mime;
using System.Text;
using Apps.NotionOAuth.Api;
using Apps.NotionOAuth.Constants;
using Apps.NotionOAuth.Invocables;
using Apps.NotionOAuth.Models.Entities;
using Apps.NotionOAuth.Models.Request;
using Apps.NotionOAuth.Models.Request.Page;
using Apps.NotionOAuth.Models.Request.Page.Properties;
using Apps.NotionOAuth.Models.Request.Page.Properties.Getter;
using Apps.NotionOAuth.Models.Request.Page.Properties.Setter;
using Apps.NotionOAuth.Models.Response;
using Apps.NotionOAuth.Models.Response.Page;
using Apps.NotionOAuth.Models.Response.Page.Properties;
using Apps.NotionOAuth.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using Newtonsoft.Json.Linq;
using RestSharp;
using Apps.NotionOAuth.Extensions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json;

namespace Apps.NotionOAuth.Actions;

[ActionList("Pages")]
public class PageActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : NotionInvocable(invocationContext)
{
    [Action("Search pages", Description = "Search pages based on specified criteria")]
    public async Task<ListPagesResponse> ListPages([ActionParameter] ListRequest input)
    {
        var items = await Client.SearchAll<PageResponse>(Creds, "page");
        var pages = items
            .Select(x => new PageEntity(x))
            .Where(x => x.LastEditedTime > (input.EditedSince ?? default))
            .Where(x => x.CreatedTime > (input.CreatedSince ?? default))
            .ToArray();

        return new(pages);
    }

    [Action("Create page", Description = "Create a new page")]
    public async Task<PageEntity> CreatePage([ActionParameter] CreatePageInput input)
    {
        if (input.PageId is not null && input.DatabaseId is not null)
            throw new PluginMisconfigurationException("Page cannot have two parents, you should specify either parent page or parent database");

        if (input.PageId is null && input.DatabaseId is null)
            throw new PluginMisconfigurationException("Page must have a parent, you should specify either parent page or parent database");

        var request = new NotionRequest(ApiEndpoints.Pages, Method.Post, Creds)
            .WithJsonBody(new CreatePageRequest(input), JsonConfig.Settings);

        var response = await Client.ExecuteWithErrorHandling<PageResponse>(request);
        return new(response);
    }

    [Action("Create page from HTML", Description = "Create a new page from HTML")]
    public async Task<PageEntity> CreatePageFromHtml(
        [ActionParameter] CreatePageInput input,
        [ActionParameter] FileRequest file)
    {
        var page = await CreatePage(input);
        var fileStream = await fileManagementClient.DownloadAsync(file.File);
        var fileBytes = await fileStream.GetByteData();
        var html = Encoding.UTF8.GetString(fileBytes);

        var blocks = NotionHtmlParser.ParseHtml(html);
        await new BlockActions(InvocationContext).AppendBlockChildren(page.Id, blocks);
        return page;
    }

    [Action("Get page as HTML", Description = "Get content of a specific page as HTML")]
    public async Task<FileResponse> GetPageAsHtml(
        [ActionParameter] PageRequest page,
        [ActionParameter] GetPageAsHtmlRequest pageAsHtmlRequest)
    {
        var response = await GetAllBlockChildrenRecursively(page.PageId, pageAsHtmlRequest);
        var html = NotionHtmlParser.ParseBlocks(page.PageId, response.ToArray(), pageAsHtmlRequest);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
        var file = await fileManagementClient.UploadAsync(stream, MediaTypeNames.Text.Html, $"{page.PageId}.html");
        return new() { File = file };
    }

    [Action("Get page as HTML (Debug)", Description = "Get content of a specific page as HTML (Debug)")]
    public async Task<PageContentResponse> GetPageAsHtmlDebug(
        [ActionParameter] PageRequest page)
    {
        var endpoint = $"{ApiEndpoints.Blocks}/{page.PageId}/children";
        var request = new NotionRequest(endpoint, Method.Get, Creds);

        var response = await Client.Paginate<JObject>(request);
        return new() { Json = JsonConvert.SerializeObject(response) };
    }

    [Action("Update page from HTML", Description = "Update specific page from an HTML file")]
    public async Task UpdatePageFromHtml(
        [ActionParameter] PageOptionalRequest page,
        [ActionParameter] FileRequest file)
    {
        var fileStream = await fileManagementClient.DownloadAsync(file.File);
        var fileBytes = await fileStream.GetByteData();
        var html = Encoding.UTF8.GetString(fileBytes);

        var extractedPageId = NotionHtmlParser.ExtractPageId(html);
        var pageId = page.PageId ?? extractedPageId
            ?? throw new("Could not extract page ID from HTML. Please provide a page ID in optional input");

        var actions = new BlockActions(InvocationContext);
        var children = await actions.ListBlockChildren(new()
        {
            BlockId = pageId
        });

        var excludeChildTypes = new[] { "file", "audio" };

        // Can't remove all blocks in parallel, because it can cause rate limiting errors
        foreach (var child in children.Children)
        {
            try
            {
                if (!excludeChildTypes.Contains(child.Type))
                {
                    await actions.DeleteBlock(new()
                    {
                        BlockId = child.Id
                    });
                }
            }
            catch
            {
                // ignored
            }
        }

        var blocks = NotionHtmlParser.ParseHtml(html);
        await actions.AppendBlockChildren(pageId, blocks);
    }

    [Action("Get page", Description = "Get details of a specific page")]
    public async Task<PageEntity> GetPage([ActionParameter] PageRequest input)
    {
        var endpoint = $"{ApiEndpoints.Pages}/{input.PageId}";
        var request = new NotionRequest(endpoint, Method.Get, Creds);

        var response = await Client.ExecuteWithErrorHandling<PageResponse>(request);
        return new(response);
    }

    [Action("Archive page", Description = "Archive a specific page")]
    public Task ArchivePage([ActionParameter] PageRequest input)
    {
        var endpoint = $"{ApiEndpoints.Pages}/{input.PageId}";
        var request = new NotionRequest(endpoint, Method.Patch, Creds)
            .WithJsonBody(new
            {
                archived = true
            });

        return Client.ExecuteWithErrorHandling(request);
    }

    [Action("Add content to page", Description = "Add text content to the bottom of a page")]
    public async Task AppendPageContent(
        [ActionParameter] PageRequest page, [ActionParameter] [Display("Text content")] string text)
    {
        var actions = new BlockActions(InvocationContext);
        var blocks = NotionHtmlParser.ParseText(text);
        await actions.AppendBlockChildren(page.PageId, [blocks]);
    }

    #region Properties

    #region Getters

    [Action("Get page string property", Description = "Get value of a string page property")]
    public async Task<StringPropertyResponse> GetStringProperty([ActionParameter] PageStringPropertyRequest input)
    {
        var response = await GetPageProperty(input.PageId, input.PropertyId);

        var propertyValue = string.Empty;

        if (response["results"]?.Any() == true)
        {
            propertyValue = response["results"]?.First().ToObject<JObject>()?.GetStringValue()
                ?? string.Empty;
        }

        return new()
        {
            PropertyValue = propertyValue
        };
    }

    [Action("Get page number property", Description = "Get value of a number page property")]
    public async Task<NumberPropertyResponse> GetNumberProperty([ActionParameter] PageNumberPropertyRequest input)
    {
        var response = await GetPageProperty(input.PageId, input.PropertyId);

        var value = response["type"]!.ToString() switch
        {
            DatabasePropertyTypes.Number => response["number"]!.ToObject<decimal>(),
            DatabasePropertyTypes.UniqueId => response["unique_id"]!["number"]!.ToObject<decimal>(),
            _ => throw new ArgumentException("Given ID does not stand for a number value property")
        };

        return new()
        {
            PropertyValue = value
        };
    }

    [Action("Get page date property", Description = "Get value of a date page property")]
    public async Task<DatePropertyResponse> GetDateProperty([ActionParameter] PageDatePropertyRequest input)
    {
        var response = await GetPageProperty(input.PageId, input.PropertyId);

        var value = response["type"]!.ToString() switch
        {
            DatabasePropertyTypes.CreatedTime => response["created_time"]!.ToObject<DateTime>(),
            DatabasePropertyTypes.Date => response["date"]!["start"]!.ToObject<DateTime>(),
            DatabasePropertyTypes.LastEditedTime => response["last_edited_time"]!.ToObject<DateTime>(),
            _ => throw new ArgumentException("Given ID does not stand for a date value property")
        };

        return new()
        {
            PropertyValue = value
        };
    }

    [Action("Get page boolean property", Description = "Get value of a boolean page property")]
    public async Task<BooleanPropertyResponse> GetBooleanProperty([ActionParameter] PageBooleanPropertyRequest input)
    {
        var response = await GetPageProperty(input.PageId, input.PropertyId);

        var value = response["type"]!.ToString() switch
        {
            DatabasePropertyTypes.Checkbox => response["checkbox"]!.ToObject<bool>(),
            _ => throw new ArgumentException("Given ID does not stand for a date value property")
        };

        return new()
        {
            PropertyValue = value
        };
    }

    [Action("Get page files property", Description = "Get value of a files page property")]
    public async Task<FilesPropertyResponse> GetFilesProperty([ActionParameter] PageFilesPropertyRequest input)
    {
        var response = await GetPageProperty(input.PageId, input.PropertyId);

        var value = response["type"]!.ToString() switch
        {
            DatabasePropertyTypes.Files => response["files"]!
                .Select(x => x["file"]?["url"]!.ToString() ?? x["external"]!["url"]!.ToString()).ToArray(),
            _ => throw new ArgumentException("Given ID does not stand for a date value property")
        };

        return new()
        {
            PropertyValue = value.Select(x =>
                new FileReference(new(HttpMethod.Get, x), input.PageId, MediaTypeNames.Application.Octet))
        };
    }

    [Action("Get page multiple string property values", Description = "Get multiple string property values of a page")]
    public async Task<MultipleStringPropertyResponse> GetMultipleStringProperty(
        [ActionParameter] PageMultipleStringPropertyRequest input)
    {
        var response = await GetPageProperty(input.PageId, input.PropertyId);

        try
        {
            var propertyType = response["results"]?.FirstOrDefault()?["type"]!.ToString() ??
                               response["type"]!.ToString();

            if (propertyType is "property_item")
                propertyType = response["property_item"]!["type"].ToString();

            var value = propertyType switch
            {
                DatabasePropertyTypes.MultiSelect => response["multi_select"]!.Select(x => x["name"]!.ToString()),
                DatabasePropertyTypes.Relation => response["results"]!.Select(x => x["relation"]!["id"]!.ToString()),
                DatabasePropertyTypes.People => response["results"]!.Select(x => x["people"]!["id"]!.ToString()),
                _ => throw new ArgumentException("Given ID does not stand for a multi select")
            };

            return new()
            {
                PropertyValue = value ?? Enumerable.Empty<string>()
            };
        }
        catch (Exception ex)
        {
            throw new PluginApplicationException($"{ex.Message}. Property: {response}");
        }
    }

    [Action("Get pages related by property", Description = "Get related pages from a page's property")]
    public async Task<ListPagesResponse> GetRelatedPagesFromProperty(
        [ActionParameter] PageRelationPropertyRequest input)
    {
        JObject response = await GetPageProperty(input.PageId, input.PropertyId);

        var relatedPagesTasks = response["results"]?
            .Select(x => x["relation"]?["id"]?.Value<string>() ?? string.Empty)
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => GetPage(new() { PageId = id })) ?? [];

        return new(await Task.WhenAll(relatedPagesTasks));
    }

    #endregion

    #region Setters

    [Action("Set page string property", Description = "Set new value of a string page property")]
    public async Task SetStringProperty([ActionParameter] SetPageStringPropertyRequest input)
    {
        var (name, property) = await GetPagePropertyWithName(input.PageId, input.PropertyId);

        var payload = property["type"]!.ToString() switch
        {
            DatabasePropertyTypes.Url => PagePropertyPayloadFactory.GetUrl(input.Value),
            DatabasePropertyTypes.Title => PagePropertyPayloadFactory.GetTitle(input.Value),
            DatabasePropertyTypes.Email => PagePropertyPayloadFactory.GetEmail(input.Value),
            DatabasePropertyTypes.PhoneNumber => PagePropertyPayloadFactory.GetPhone(input.Value),
            DatabasePropertyTypes.Status => PagePropertyPayloadFactory.GetStatus(input.Value),
            DatabasePropertyTypes.Select => PagePropertyPayloadFactory.GetSelect(input.Value),
            DatabasePropertyTypes.RichText => PagePropertyPayloadFactory.GetRichText(input.Value),
            DatabasePropertyTypes.Relation => PagePropertyPayloadFactory.GetRelation(input.Value),
            _ => throw new ArgumentException("Given ID does not stand for a string value property")
        };

        await UpdatePageProperty(input.PageId, name, payload);
    }

    [Action("Set page number property", Description = "Set new value of a number page property")]
    public async Task SetNumberProperty([ActionParameter] SetPageNumberPropertyRequest input)
    {
        if (string.IsNullOrEmpty(input.PageId))
        {
            throw new PluginMisconfigurationException("Page ID is null or empty. Please provide a valid ID.");
        }
        
        if (string.IsNullOrEmpty(input.PropertyId))
        {
            throw new PluginMisconfigurationException("Property ID is null or empty. Please provide a valid ID.");
        }
        
        var (name, property) = await GetPagePropertyWithName(input.PageId, input.PropertyId);

        var payload = property["type"]!.ToString() switch
        {
            DatabasePropertyTypes.Number => PagePropertyPayloadFactory.GetNumber(input.Value),
            _ => throw new PluginMisconfigurationException("Given ID does not stand for a string value property")
        };

        await UpdatePageProperty(input.PageId, name, payload);
    }

    [Action("Set page boolean property", Description = "Set new value of a boolean page property")]
    public async Task SetBooleanProperty([ActionParameter] SetPageBooleanPropertyRequest input)
    {
        var (name, property) = await GetPagePropertyWithName(input.PageId, input.PropertyId);

        var payload = property["type"]!.ToString() switch
        {
            DatabasePropertyTypes.Checkbox => PagePropertyPayloadFactory.GetCheckbox(input.Value),
            _ => throw new ArgumentException("Given ID does not stand for a string value property")
        };

        await UpdatePageProperty(input.PageId, name, payload);
    }

    [Action("Set page multiple value property", Description = "Set new values of a multiple value page property")]
    public async Task SetMultipleValueProperty([ActionParameter] SetPageMultipleValuePropertyRequest input)
    {
        var (name, property) = await GetPagePropertyWithName(input.PageId, input.PropertyId);

        var newValues = input.Values;

        if (input.AddOnUpdate.HasValue && input.AddOnUpdate.Value)
        {
            var currentValues = await GetMultipleStringProperty(input);
            newValues = newValues.Concat(currentValues.PropertyValue);
        }

        var payload = property["type"]!.ToString() switch
        {
            DatabasePropertyTypes.MultiSelect => PagePropertyPayloadFactory.GetMultiSelect(newValues),
            DatabasePropertyTypes.Relation => PagePropertyPayloadFactory.GetRelation(newValues),
            DatabasePropertyTypes.People => PagePropertyPayloadFactory.GetPeople(newValues),
            _ => throw new ArgumentException("Given ID does not stand for a string value property")
        };

        await UpdatePageProperty(input.PageId, name, payload);
    }

    [Action("Set page files property", Description = "Set new value of a files page property")]
    public async Task SetFilesProperty([ActionParameter] SetPageFilesPropertyRequest input)
    {
        var (name, property) = await GetPagePropertyWithName(input.PageId, input.PropertyId);

        var payload = property["type"]!.ToString() switch
        {
            DatabasePropertyTypes.Files => PagePropertyPayloadFactory.GetFiles(input.Values),
            _ => throw new ArgumentException("Given ID does not stand for a string value property")
        };

        await UpdatePageProperty(input.PageId, name, payload);
    }

    [Action("Set page date property", Description = "Set new value of a date page property")]
    public async Task SetDateProperty([ActionParameter] SetPageDatePropertyRequest input)
    {
        var (name, property) = await GetPagePropertyWithName(input.PageId, input.PropertyId);

        var payload = property["type"]!.ToString() switch
        {
            DatabasePropertyTypes.Date => PagePropertyPayloadFactory.GetDate(input.Date, input.EndDate, input.IncludeTime),
            _ => throw new ArgumentException("Given ID does not stand for a date value property")
        };

        await UpdatePageProperty(input.PageId, name, payload);
    }

    [Action("Set page relation property", Description = "Set new value of a relation page property")]
    public async Task SetRelationProperty([ActionParameter] SetPageRelationPropertyRequest input)
    {
        var (name, property) = await GetPagePropertyWithName(input.PageId, input.PropertyId);

        var payload = property["type"]!.ToString() switch
        {
            DatabasePropertyTypes.Relation => PagePropertyPayloadFactory.GetRelation(input.RelatedPageIds),
            _ => throw new PluginMisconfigurationException("Given field ID does not stand for a relation property.")
        };

        await UpdatePageProperty(input.PageId, name, payload);
    }

    [Action("Set page property as empty", Description = "Remove values in a page property")]
    public async Task SetEmptyValueProperty([ActionParameter] SetPagePropertyNullRequest input)
    {
        var (name, property) = await GetPagePropertyWithName(input.PageId, input.PropertyId);

        var payload = property["type"]!.ToString() switch
        {
            DatabasePropertyTypes.PhoneNumber => new JObject { { "phone_number", null } },
            DatabasePropertyTypes.Email => new JObject { { "email", null } },
            DatabasePropertyTypes.Url => new JObject { { "url", null } },
            DatabasePropertyTypes.Number => new JObject { { "number", null } },
            DatabasePropertyTypes.Status => new JObject { { "status", null } },
            DatabasePropertyTypes.Select => new JObject { { "select", null } },
            DatabasePropertyTypes.Checkbox => new JObject { { "checkbox", "false" } },
            DatabasePropertyTypes.MultiSelect => new JObject { { "multi_select", new JArray() } },
            DatabasePropertyTypes.RichText => new JObject { { "rich_text", new JArray() } },
            DatabasePropertyTypes.Files => new JObject { { "files", new JArray() } },
            DatabasePropertyTypes.Relation => new JObject { { "relation", new JArray() } },
            DatabasePropertyTypes.People => new JObject { { "people", new JArray() } },
            _ => throw new PluginMisconfigurationException("Property type is not recognized")
        };

        await UpdatePageProperty(input.PageId, name, payload);
    }

    #endregion

    #endregion

    #region Utils

    private async Task<List<JObject>> GetAllBlockChildrenRecursively(string blockId,
        GetPageAsHtmlRequest? pageAsHtmlRequest)
    {
        if (string.IsNullOrEmpty(blockId))
        {
            throw new ArgumentException("Block ID cannot be null or empty.", nameof(blockId));
        }

        var endpoint = $"{ApiEndpoints.Blocks}/{blockId}/children";
        var request = new NotionRequest(endpoint, Method.Get, Creds);
        var allBlocks = await Client.Paginate<JObject>(request);
        var childBlocksToAdd = new List<JObject>();

        var includeChildPages = pageAsHtmlRequest?.IncludeChildPages ?? false;
        var includeChildDatabases = pageAsHtmlRequest?.IncludeChildDatabases ?? false;

        allBlocks = FilterBlocks(allBlocks, includeChildPages, includeChildDatabases);

        var databasesAndInputPlaces = new Dictionary<int, JObject>();
        foreach (var block in allBlocks)
        {
            await ProcessBlock(block, pageAsHtmlRequest, childBlocksToAdd, databasesAndInputPlaces, allBlocks);
        }

        InsertDatabases(allBlocks, databasesAndInputPlaces);
        allBlocks.AddRange(childBlocksToAdd);
        allBlocks = allBlocks.Where(x => x["type"]?.ToString() != "child_database").ToList();

        return allBlocks;
    }

    private List<JObject> FilterBlocks(List<JObject> allBlocks, bool includeChildPages, bool includeChildDatabases)
    {
        if (!includeChildPages)
        {
            allBlocks = allBlocks.Where(x => x["type"]?.ToString() != "child_page").ToList();
        }

        if (!includeChildDatabases)
        {
            allBlocks = allBlocks.Where(x => x["type"]?.ToString() != "child_database").ToList();
        }

        return allBlocks;
    }

    private async Task ProcessBlock(JObject block, GetPageAsHtmlRequest? pageAsHtmlRequest,
        List<JObject> childBlocksToAdd, Dictionary<int, JObject> databasesAndInputPlaces, List<JObject> allBlocks)
    {
        var hasChildren = block["has_children"]?.ToObject<bool>() ?? false;

        if (ShouldSkipBlock(block, pageAsHtmlRequest))
        {
            return;
        }

        if (hasChildren)
        {
            var childBlocks = await GetAllBlockChildrenRecursively(block["id"]!.ToString(), pageAsHtmlRequest);
            childBlocksToAdd.AddRange(childBlocks);

            var blockIdValue = block["id"]?.ToString();
            var childBlocksIds = childBlocks.Where(x => x["parent"]!["block_id"]?.ToString() == blockIdValue)
                .Select(x => x["id"]!.ToString());
            if (childBlocksIds.Any())
            {
                block.Add("child_block_ids", JArray.FromObject(childBlocksIds));
            }
        }
        else if (block["type"]?.ToString() == "child_database")
        {
            var databaseId = block["id"]!.ToString();
            var databaseAction = new DatabaseActions(InvocationContext);
            var database = await databaseAction.GetDatabaseAsJson(new() { DatabaseId = databaseId });

            var currentIndex = allBlocks.IndexOf(block);
            databasesAndInputPlaces.Add(currentIndex, database);
            
            var pages = await databaseAction.SearchPagesInDatabaseAsJsonAsync(databaseId);

            foreach (var page in pages)
            {
                var pageAsBlockRequest = new NotionRequest($"{ApiEndpoints.Blocks}/{page.Id}", Method.Get, Creds);
                var pageAsBlock = await Client.ExecuteWithErrorHandling<JObject>(pageAsBlockRequest);
                
                var childPageJObject = JObject.FromObject(page);
                childPageJObject.Add("type", "child_page");
                childPageJObject.Add("child_page", pageAsBlock["child_page"]);
                childBlocksToAdd.Add(childPageJObject);
                
                var pageBlocks = await GetAllBlockChildrenRecursively(page.Id, pageAsHtmlRequest);
                childBlocksToAdd.AddRange(pageBlocks);
            }
            
            database.Add("child_block_ids", JArray.FromObject(pages.Select(x => x.Id)));
        }
    }

    private bool ShouldSkipBlock(JObject block, GetPageAsHtmlRequest? pageAsHtmlRequest)
    {
        var includeChildPages = pageAsHtmlRequest?.IncludeChildPages ?? false;
        var includeChildDatabases = pageAsHtmlRequest?.IncludeChildDatabases ?? false;

        return (block["type"]?.ToString() == "child_page" && !includeChildPages) ||
               (block["type"]?.ToString() == "child_database" && !includeChildDatabases);
    }

    private void InsertDatabases(List<JObject> allBlocks, Dictionary<int, JObject> databasesAndInputPlaces)
    {
        foreach (var (index, database) in databasesAndInputPlaces)
        {
            allBlocks.Insert(index, database);
        }
    }

    private Task<JObject> GetPageProperty(string pageId, string propertyId)
    {
        var endpoint = $"{ApiEndpoints.Pages}/{pageId}/properties/{propertyId}";
        var request = new NotionRequest(endpoint, Method.Get, Creds);

        return Client.ExecuteWithErrorHandling<JObject>(request);
    }

    private async Task<(string name, JObject proerty)> GetPagePropertyWithName(string pageId, string propertyId)
    {
        var endpoint = $"{ApiEndpoints.Pages}/{pageId}";
        var request = new NotionRequest(endpoint, Method.Get, Creds);

        var page = await Client.ExecuteWithErrorHandling<PageResponse>(request);
        var property = page.Properties
            .FirstOrDefault(x => x.Value["id"]!.ToString() == propertyId);

        if (property.Equals(default(KeyValuePair<string, JObject>)))
        {
            throw new PluginMisconfigurationException($"Property with ID '{propertyId}' not found on the page.");
        }

        return (property.Key, property.Value);
    }

    private Task UpdatePageProperty(string pageId, string propertyName, JObject propertyContent)
    {
        var endpoint = $"{ApiEndpoints.Pages}/{pageId}";
        var request = new NotionRequest(endpoint, Method.Patch, Creds)
            .WithJsonBody(new PropertiesRequest()
            {
                Properties = new()
                {
                    [propertyName] = propertyContent
                }
            }, JsonConfig.Settings);

        return Client.ExecuteWithErrorHandling(request);
    }

    #endregion
}