using System.Net.Mime;
using System.Text;
using Apps.Notion.Api;
using Apps.Notion.Constants;
using Apps.Notion.Invocables;
using Apps.Notion.Models;
using Apps.Notion.Models.Entities;
using Apps.Notion.Models.Request;
using Apps.Notion.Models.Request.Page;
using Apps.Notion.Models.Request.Page.Properties;
using Apps.Notion.Models.Request.Page.Properties.Getter;
using Apps.Notion.Models.Request.Page.Properties.Setter;
using Apps.Notion.Models.Response;
using Apps.Notion.Models.Response.Page;
using Apps.Notion.Models.Response.Page.Properties;
using Apps.Notion.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.Notion.Actions;

[ActionList]
public class PageActions : NotionInvocable
{
    public PageActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }

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

        var blocks = NotionHtmlParser.ParseHtml(file.File.Bytes);
        await new BlockActions(InvocationContext).AppendBlockChildren(page.Id, blocks);

        return page;
    }

    [Action("Get page as HTML", Description = "Get content of a specific page as HTML")]
    public async Task<FileResponse> GetPageAsHtml(
        [ActionParameter] PageRequest page)
    {
        var endpoint = $"{ApiEndpoints.Blocks}/{page.PageId}/children";
        var request = new NotionRequest(endpoint, Method.Get, Creds);

        var response = await Client.Paginate<JObject>(request);
        var html = NotionHtmlParser.ParseBlocks(response.ToArray());

        return new()
        {
            File = new(Encoding.UTF8.GetBytes(html))
            {
                Name = $"{page.PageId}.html",
                ContentType = MediaTypeNames.Text.Html
            }
        };
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

        // Can't remove all blocks in parallel, because it can cause rate limiting errors
        foreach (var child in children.Children)
        {
            try
            {
                await actions.DeleteBlock(new()
                {
                    BlockId = child.Id
                });
            }
            catch
            {
                // ignored
            }
        }

        var blocks = NotionHtmlParser.ParseHtml(file.File.Bytes);
        await actions.AppendBlockChildren(page.PageId, blocks);
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

    #region Properties

    #region Getters

    [Action("Get page string property", Description = "Get value of a string page property")]
    public async Task<StringPropertyResponse> GetStringProperty([ActionParameter] PageStringPropertyRequest input)
    {
        var response = await GetPageProperty(input.PageId, input.PropertyId);

        if (response["results"] is not null)
            response = response["results"]!.First().ToObject<JObject>()!;

        var value = response["type"]!.ToString() switch
        {
            "url" => response["url"]!.ToString(),
            "title" => response["title"]!.ToObject<TitleModel>()!.PlainText,
            "email" => response["email"]!.ToString(),
            "phone_number" => response["phone_number"]!.ToString(),
            "status" => response["status"]!["name"]!.ToString(),
            "created_by" => response["created_by"]!["id"]!.ToString(),
            "last_edited_by" => response["last_edited_by"]!["id"]!.ToString(),
            "select" => response["select"]!["name"]!.ToString(),
            "rich_text" => response["rich_text"]!.ToObject<TitleModel>()!.PlainText,
            _ => throw new ArgumentException("Given ID does not stand for a string value property")
        };

        return new()
        {
            PropertyValue = value
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
            PropertyValue = value.Select(x => new FileReference(new(HttpMethod.Get, x)))
        };
    }

    [Action("Get page multiple string property values", Description = "Get multiple string property values of a page")]
    public async Task<MultipleStringPropertyResponse> GetMultipleStringProperty(
        [ActionParameter] PageMultipleStringPropertyRequest input)
    {
        var response = await GetPageProperty(input.PageId, input.PropertyId);

        var propertyType = response["results"]?.First()["type"]!.ToString() ?? response["type"]!.ToString();
        var value = propertyType switch
        {
            "multi_select" => response["multi_select"]!.Select(x => x["name"]!.ToString()),
            "relation" => response["results"]!.Select(x => x["relation"]!["id"]!.ToString()),
            "people" => response["results"]!.Select(x => x["people"]!["id"]!.ToString()),
            _ => throw new ArgumentException("Given ID does not stand for a date value property")
        };

        return new()
        {
            PropertyValue = value
        };
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

        var payload = property["type"]!.ToString() switch
        {
            "multi_select" => PagePropertyPayloadFactory.GetMultiSelect(input.Values),
            "relation" => PagePropertyPayloadFactory.GetRelation(input.Values),
            "people" => PagePropertyPayloadFactory.GetPeople(input.Values),
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
            "date" => PagePropertyPayloadFactory.GetDate(input.Date, input.EndDate),
            _ => throw new ArgumentException("Given ID does not stand for a date value property")
        };

        await UpdatePageProperty(input.PageId, name, payload);
    }

    #endregion

    #endregion

    #region Utils

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