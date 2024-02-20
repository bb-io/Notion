using Apps.NotionOAuth.Extensions;
using Apps.NotionOAuth.Models;
using Blackbird.Applications.Sdk.Common.Files;
using Newtonsoft.Json.Linq;

namespace Apps.NotionOAuth.Utils;

public static class PagePropertyPayloadFactory
{
    public static JObject GetUrl(string value)
        => new
        {
            url = value
        }.ToJObject();

    public static JObject GetTitle(string value)
        => new
        {
            title = new TitleModel[]
            {
                new()
                {
                    Text = new()
                    {
                        Content = value
                    }
                }
            }
        }.ToJObject();

    public static JObject GetEmail(string value)
        => new
        {
            email = value
        }.ToJObject();

    public static JObject GetPhone(string value)
        => new
        {
            phone_number = value
        }.ToJObject();

    public static JObject GetStatus(string value)
        => new
        {
            status = new
            {
                name = value
            }
        }.ToJObject();

    public static JObject GetSelect(string value)
        => new
        {
            select = new
            {
                name = value
            }
        }.ToJObject();

    public static JObject GetRichText(string value)
        => new
        {
            rich_text = new TitleModel[]
            {
                new()
                {
                    Text = new()
                    {
                        Content = value
                    }
                }
            }
        }.ToJObject();

    public static JObject GetNumber(decimal value)
        => new
        {
            number = value
        }.ToJObject();

    public static JObject GetCheckbox(bool value)
        => new
        {
            checkbox = value
        }.ToJObject();

    public static JObject GetMultiSelect(IEnumerable<string> values)
        => new
        {
            multi_select = values.Select(x => new
            {
                name = x
            })
        }.ToJObject();

    public static JObject GetRelation(IEnumerable<string> values)
        => new
        {
            relation = values.Select(x => new
            {
                id = x
            })
        }.ToJObject();

    public static JObject GetPeople(IEnumerable<string> values)
        => new
        {
            people = values.Select(x => new
            {
                id = x
            })
        }.ToJObject();

    public static JObject GetFiles(IEnumerable<FileReference> values)
        => new
        {
            files = values.Select(x => x.Url.StartsWith("https://prod-files-secure.s3")
                ? new
                {
                    name = x.Name,
                    file = new
                    {
                        url = x.Url
                    }
                }
                : (object)new
                {
                    name = x.Name,
                    external = new
                    {
                        url = x.Url
                    }
                })
        }.ToJObject();

    public static JObject GetDate(DateTime startDate, DateTime? endDate, bool? inputIncludeTime)
        => new
        {
            date = new
            {
                start = inputIncludeTime != true
                    ? startDate.ToString("yyyy-MM-dd")
                    : startDate.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
                end = endDate is not null && inputIncludeTime != true
                    ? endDate.Value.ToString("yyyy-MM-dd")
                    : endDate?.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
            }
        }.ToJObject();
}