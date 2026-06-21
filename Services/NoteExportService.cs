using FocusFlowFinal.Models.Notes;
using Markdig;
using System.IO;
using System.Net;
using System.Text;

namespace FocusFlowFinal.Services;

public class NoteExportService
{
    private static readonly MarkdownPipeline _pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public void ExportMarkdown(Note note, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {note.Title}");
        sb.AppendLine();
        sb.AppendLine($"Дата: {note.Date:yyyy-MM-dd}");
        if (note.Tags.Count > 0)
            sb.AppendLine($"Теги: {string.Join(", ", note.Tags)}");
        sb.AppendLine();
        sb.AppendLine(note.MarkdownContent);
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    public void ExportHtml(Note note, string path)
    {
        var body    = Markdown.ToHtml(note.MarkdownContent, _pipeline);
        var tagsHtml = note.Tags.Count > 0
            ? $"<p><strong>Теги:</strong> {WebUtility.HtmlEncode(string.Join(", ", note.Tags))}</p>"
            : "";

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset=\"utf-8\">");
        html.AppendLine($"  <title>{WebUtility.HtmlEncode(note.Title)}</title>");
        html.AppendLine("  <style>");
        html.AppendLine("    body { font-family: -apple-system, BlinkMacSystemFont, sans-serif; max-width: 800px; margin: 40px auto; padding: 0 20px; line-height: 1.6; }");
        html.AppendLine("    h1 { border-bottom: 2px solid #e5e7eb; padding-bottom: 8px; }");
        html.AppendLine("    pre { background: #f4f4f4; padding: 12px; border-radius: 4px; overflow-x: auto; }");
        html.AppendLine("    code { background: #f4f4f4; padding: 2px 4px; border-radius: 3px; }");
        html.AppendLine("    blockquote { border-left: 4px solid #d1d5db; margin: 0; padding-left: 16px; color: #6b7280; }");
        html.AppendLine("  </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine($"  <h1>{WebUtility.HtmlEncode(note.Title)}</h1>");
        html.AppendLine($"  <p><em>{note.Date:yyyy-MM-dd}</em></p>");
        html.AppendLine(tagsHtml);
        html.AppendLine("  <hr>");
        html.AppendLine(body);
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        File.WriteAllText(path, html.ToString(), Encoding.UTF8);
    }

    public void ExportText(Note note, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine(note.Title);
        sb.AppendLine(new string('=', note.Title.Length));
        sb.AppendLine();
        sb.AppendLine($"Дата: {note.Date:yyyy-MM-dd}");
        if (note.Tags.Count > 0)
            sb.AppendLine($"Теги: {string.Join(", ", note.Tags)}");
        sb.AppendLine();
        sb.AppendLine(note.MarkdownContent);
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }
}
