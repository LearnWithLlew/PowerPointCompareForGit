using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace PowerPointCompareForGit;

internal static partial class CanonicalXml
{
    private static readonly XNamespace Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PresentationMl = "http://schemas.openxmlformats.org/presentationml/2006/main";
    private static readonly XNamespace DrawingMl = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace MarkupCompatibility = "http://schemas.openxmlformats.org/markup-compatibility/2006";

    // Flags PowerPoint rewrites on text it has looked at, without changing how the text draws.
    private static readonly HashSet<string> EditorFlags = ["dirty", "err", "smtClean"];

    internal static string Of(XElement root, Dictionary<string, string> targets)
    {
        root.Elements(PresentationMl + "sldLayoutIdLst").Remove();
        RemoveAnimations(root);
        foreach (var attribute in root.DescendantsAndSelf().Attributes().Where(attribute => attribute.Name.Namespace == Relationships))
        {
            attribute.Value = targets.GetValueOrDefault(attribute.Value, "");
        }

        foreach (var field in root.Descendants(DrawingMl + "fld").Where(IsRecomputedField))
        {
            field.Elements(DrawingMl + "t").Remove();
        }

        var canonical = new StringBuilder();
        AppendCanonical(root, canonical);
        return canonical.ToString();
    }

    // Animations and transitions don't appear in the image. A change to them would otherwise show two identical-looking slides.
    private static void RemoveAnimations(XElement root)
    {
        root.Elements(PresentationMl + "timing").Remove();
        root.Elements(PresentationMl + "transition").Remove();
        root.Elements(MarkupCompatibility + "AlternateContent")
            .Where(content => content.Descendants(PresentationMl + "transition").Any())
            .Remove();
    }

    // PowerPoint stores each date and slide number field's last value, and rewrites it on save. A renderer works it
    // out again, so a stored value that changes doesn't mean the slide changed.
    private static bool IsRecomputedField(XElement field)
        => (string?)field.Attribute("type") is { } type
           && (type.StartsWith("datetime", StringComparison.Ordinal) || type == "slidenum");

    // Written with full names, because PowerPoint moves namespace declarations between elements when it saves.
    private static void AppendCanonical(XElement element, StringBuilder canonical)
    {
        canonical.Append('<').Append(element.Name);
        foreach (var attribute in DrawnAttributes(element))
        {
            canonical.Append(' ').Append(attribute.Name).Append("=\"").Append(Guid().Replace(attribute.Value, "guid")).Append('"');
        }

        canonical.Append('>');
        foreach (var node in element.Nodes())
        {
            switch (node)
            {
                case XElement child:
                    AppendCanonical(child, canonical);
                    break;
                // Space between elements means nothing, but inside a text run it's drawn.
                case XText text when element.Name == DrawingMl + "t" || !string.IsNullOrWhiteSpace(text.Value):
                    canonical.Append(text.Value);
                    break;
            }
        }

        canonical.Append("</>");
    }

    private static IEnumerable<XAttribute> DrawnAttributes(XElement element)
        => element.Attributes()
            .Where(attribute => !attribute.IsNamespaceDeclaration && !EditorFlags.Contains(attribute.Name.LocalName))
            .OrderBy(attribute => attribute.Name.ToString(), StringComparer.Ordinal);

    // Normalizing replaces these too, but not when it fails, and a GUID is never drawn.
    [GeneratedRegex(@"\{?[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\}?")]
    private static partial Regex Guid();
}
