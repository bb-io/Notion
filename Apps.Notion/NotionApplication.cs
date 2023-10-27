using Blackbird.Applications.Sdk.Common;

namespace Apps.Notion;

public class NotionApplication : IApplication
{
    public string Name
    {
        get => "Notion";
        set { }
    }

    public T GetInstance<T>()
    {
        throw new NotImplementedException();
    }
}