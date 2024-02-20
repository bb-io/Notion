using Apps.NotionOAuth.Models.Entities;

namespace Apps.NotionOAuth.Models.Response.DataBase;

public record ListDatabasesResponse(DatabaseEntity[] Databases);