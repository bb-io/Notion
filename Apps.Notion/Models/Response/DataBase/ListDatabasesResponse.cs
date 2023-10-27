using Apps.Notion.Models.Entities;

namespace Apps.Notion.Models.Response.DataBase;

public record ListDatabasesResponse(DatabaseEntity[] Databases);