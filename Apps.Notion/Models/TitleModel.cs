namespace Apps.Notion.Models;

public class TitleModel
{
    public string Type { get; set; }
    public TextModel Text { get; set; }
    public AnnotationsModel? Annotations { get; set; }
    public string PlainText { get; set; }
}