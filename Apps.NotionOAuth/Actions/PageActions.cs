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
using Newtonsoft.Json;

namespace Apps.NotionOAuth.Actions;

[ActionList]
public class PageActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : NotionInvocable(invocationContext)
{
    [Action("List pages", Description = "List all pages")]
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
            throw new("Page cannot have two parents, you should specify either parent page or parent database");

        if (input.PageId is null && input.DatabaseId is null)
            throw new("Page must have a parent, you should specify either parent page or parent database");

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

        var blocks = NotionHtmlParser.ParseHtml(fileBytes);
        await new BlockActions(InvocationContext).AppendBlockChildren(page.Id, blocks);

        return page;
    }

    [Action("Get page as HTML", Description = "Get content of a specific page as HTML")]
    public async Task<FileResponse> GetPageAsHtml(
        [ActionParameter] PageRequest page)
    {
        var response = await GetAllBlockChildrenRecursively(page.PageId);
        var html = NotionHtmlParser.ParseBlocks(response.ToArray());

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
        [ActionParameter] PageRequest page,
        [ActionParameter] FileRequest file)
    {
        var actions = new BlockActions(InvocationContext);
        var children = await actions.ListBlockChildren(new()
        {
            BlockId = page.PageId
        });
        
        var fileStream = await fileManagementClient.DownloadAsync(file.File);
        var fileBytes = await fileStream.GetByteData();
        
        var blocks = NotionHtmlParser.ParseHtml(fileBytes);
        await actions.AppendBlockChildren(page.PageId, blocks);
        
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
    [ActionParameter] PageRequest page, [ActionParameter][Display("Text content")] string text)
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

        if (response["results"] is not null)
            response = response["results"]!.First().ToObject<JObject>()!;

        return new()
        {
            PropertyValue = response.GetStringValue()
    };
    }

    [Action("Get page number property", Description = "Get value of a number page property")]
    public async Task<NumberPropertyResponse> GetNumberProperty([ActionParameter] PageNumberPropertyRequest input)
    {
        var response = await GetPageProperty(input.PageId, input.PropertyId);

        var value = response["type"]!.ToString() switch
        {
            "number" => response["number"]!.ToObject<decimal>(),
            "unique_id" => response["unique_id"]!["number"]!.ToObject<decimal>(),
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
            "created_time" => response["created_time"]!.ToObject<DateTime>(),
            "date" => response["date"]!["start"]!.ToObject<DateTime>(),
            "last_edited_time" => response["last_edited_time"]!.ToObject<DateTime>(),
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
            "checkbox" => response["checkbox"]!.ToObject<bool>(),
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
            "files" => response["files"]!
                .Select(x => x["file"]?["url"]!.ToString() ?? x["external"]!["url"]!.ToString()).ToArray(),
            _ => throw new ArgumentException("Given ID does not stand for a date value property")
        };

        return new()
        {
            PropertyValue = value.Select(x => new FileReference(new(HttpMethod.Get, x), input.PageId, MediaTypeNames.Application.Octet))
        };
    }

    [Action("Get page multiple string property values", Description = "Get multiple string property values of a page")]
    public async Task<MultipleStringPropertyResponse> GetMultipleStringProperty(
        [ActionParameter] PageMultipleStringPropertyRequest input)
    {
        var response = await GetPageProperty(input.PageId, input.PropertyId);

        try
        {
            var propertyType = response["results"]?.FirstOrDefault()?["type"]!.ToString() ?? response["type"]!.ToString();

            if (propertyType is "property_item")
                propertyType = response["property_item"]!["type"].ToString();
            
            var value = propertyType switch
            {
                "multi_select" => response["multi_select"]!.Select(x => x["name"]!.ToString()),
                "relation" => response["results"]!.Select(x => x["relation"]!["id"]!.ToString()),
                "people" => response["results"]!.Select(x => x["people"]!["id"]!.ToString()),
                _ => throw new ArgumentException("Given ID does not stand for a multi select")
            };

            return new()
            {
                PropertyValue = value ?? Enumerable.Empty<string>()
            };
        }
        catch (Exception ex)
        {
            throw new($"{ex.Message}. Property: {response}");
        }
    }

    #endregion

    #region Setters

    [Action("Set page string property", Description = "Set new value of a string page property")]
    public async Task SetStringProperty([ActionParameter] SetPageStringPropertyRequest input)
    {
        var (name, property) = await GetPagePropertyWithName(input.PageId, input.PropertyId);

        var payload = property["type"]!.ToString() switch
        {
            "url" => PagePropertyPayloadFactory.GetUrl(input.Value),
            "title" => PagePropertyPayloadFactory.GetTitle(input.Value),
            "email" => PagePropertyPayloadFactory.GetEmail(input.Value),
            "phone_number" => PagePropertyPayloadFactory.GetPhone(input.Value),
            "status" => PagePropertyPayloadFactory.GetStatus(input.Value),
            "select" => PagePropertyPayloadFactory.GetSelect(input.Value),
            "rich_text" => PagePropertyPayloadFactory.GetRichText(input.Value),
            "relation" => PagePropertyPayloadFactory.GetRelation(input.Value),
            _ => throw new ArgumentException("Given ID does not stand for a string value property")
        };

        await UpdatePageProperty(input.PageId, name, payload);
    }

    [Action("Set page number property", Description = "Set new value of a number page property")]
    public async Task SetNumberProperty([ActionParameter] SetPageNumberPropertyRequest input)
    {
        var (name, property) = await GetPagePropertyWithName(input.PageId, input.PropertyId);

        var payload = property["type"]!.ToString() switch
        {
            "number" => PagePropertyPayloadFactory.GetNumber(input.Value),
            _ => throw new ArgumentException("Given ID does not stand for a string value property")
        };

        await UpdatePageProperty(input.PageId, name, payload);
    }

    [Action("Set page boolean property", Description = "Set new value of a boolean page property")]
    public async Task SetBooleanProperty([ActionParameter] SetPageBooleanPropertyRequest input)
    {
        var (name, property) = await GetPagePropertyWithName(input.PageId, input.PropertyId);

        var payload = property["type"]!.ToString() switch
        {
            "checkbox" => PagePropertyPayloadFactory.GetCheckbox(input.Value),
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
            "multi_select" => PagePropertyPayloadFactory.GetMultiSelect(newValues),
            "relation" => PagePropertyPayloadFactory.GetRelation(newValues),
            "people" => PagePropertyPayloadFactory.GetPeople(newValues),
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
            "files" => PagePropertyPayloadFactory.GetFiles(input.Values),
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
            "date" => PagePropertyPayloadFactory.GetDate(input.Date, input.EndDate, input.IncludeTime),
            _ => throw new ArgumentException("Given ID does not stand for a date value property")
        };

        await UpdatePageProperty(input.PageId, name, payload);
    }

    #endregion

    #endregion

    #region Utils

    private async Task<List<JObject>> GetAllBlockChildrenRecursively(string blockId)
    {
        if (string.IsNullOrEmpty(blockId))
        {
            throw new ArgumentException("Block ID cannot be null or empty.", nameof(blockId));
        }
        
        var endpoint = $"{ApiEndpoints.Blocks}/{blockId}/children";
        var request = new NotionRequest(endpoint, Method.Get, Creds);
        var allBlocks = await Client.Paginate<JObject>(request);
        var childBlocksToAdd = new List<JObject>();
        
        foreach (var block in allBlocks)
        {
            var hasChildren = block["has_children"]?.ToObject<bool>() ?? false;
            if (hasChildren)
            {
                var childBlocks = await GetAllBlockChildrenRecursively(block["id"]!.ToString());
                childBlocksToAdd.AddRange(childBlocks);
                
                var blockIdValue = block["id"]?.ToString();
                var childBlocksIds = childBlocks.Where(x => x["parent"]!["block_id"]?.ToString() == blockIdValue).Select(x => x["id"]!.ToString());
                if (childBlocksIds.Any())
                {
                    block.Add("child_block_ids", JArray.FromObject(childBlocksIds));
                }
            }
        }

        allBlocks.AddRange(childBlocksToAdd);
        return allBlocks;
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
            .First(x => x.Value["id"]!.ToString() == propertyId);

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