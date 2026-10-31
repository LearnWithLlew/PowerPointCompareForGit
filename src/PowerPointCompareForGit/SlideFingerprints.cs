using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DeterministicIoPackaging;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using StbImageSharp;

namespace PowerPointCompareForGit;

// Two slides with the same fingerprint draw the same, wherever they sit in their decks.
internal static partial class SlideFingerprints
{
    private static readonly XNamespace Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PresentationMl = "http://schemas.openxmlformats.org/presentationml/2006/main";
    private static readonly XNamespace DrawingMl = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace MarkupCompatibility = "http://schemas.openxmlformats.org/markup-compatibility/2006";

    // Flags PowerPoint rewrites on text it has looked at, without changing how the text draws.
    private static readonly HashSet<string> EditorFlags = ["dirty", "err", "smtClean"];

    // One fingerprint per slide, in slide order.
    internal static IReadOnlyList<string> Of(string deckPath)
    {
        using var normalized = Normalize(deckPath);
        using var deck = PresentationDocument.Open(normalized, false);
        var presentationPart = deck.PresentationPart!;
        var presentation = presentationPart.Presentation!;
        var deckWide = $"{presentation.SlideSize?.OuterXml}{presentation.DefaultTextStyle?.OuterXml}";
        var known = new Dictionary<Uri, string>();
        return presentation.SlideIdList?.Elements<SlideId>()
            .Select(slideId => Hash($"{deckWide}{Fingerprint(presentationPart.GetPartById(slideId.RelationshipId!), known)}"))
            .ToList() ?? [];
    }

    // Normalizing fails on some pictures that PowerPoint opens fine, such as a PNG it can't read to the end. The
    // fingerprint already ignores part names, relationship ids and compression, so the deck as it is still works.
    private static MemoryStream Normalize(string deckPath)
    {
        using var source = File.OpenRead(deckPath);
        try
        {
            return DeterministicPackage.Convert(source);
        }
        catch (Exception ex) when (ex is EndOfStreamException or InvalidDataException)
        {
            return new MemoryStream(File.ReadAllBytes(deckPath));
        }
    }

    // The part's content, with each relationship id replaced by what it points to, plus everything else it draws from.
    // Part names and relationship ids can change without the slide changing, so neither is part of the fingerprint.
    private static string Fingerprint(OpenXmlPart part, Dictionary<Uri, string> known)
    {
        if (known.TryGetValue(part.Uri, out var fingerprint))
        {
            return fingerprint;
        }

        // A part can lead back to itself, for example a chart's parts that refer to each other.
        known[part.Uri] = "";

        var targets = new Dictionary<string, string>();
        foreach (var child in part.Parts.Where(child => Draws(part, child.OpenXmlPart)))
        {
            targets[child.RelationshipId] = Fingerprint(child.OpenXmlPart, known);
        }

        foreach (var reference in part.ExternalRelationships.Concat<ReferenceRelationship>(part.HyperlinkRelationships))
        {
            targets[reference.Id] = reference.Uri.ToString();
        }

        foreach (var reference in part.DataPartReferenceRelationships)
        {
            using var data = reference.DataPart.GetStream(FileMode.Open, FileAccess.Read);
            targets[reference.Id] = Hash(data);
        }

        var drawnFrom = string.Join(",", targets.Values.Order(StringComparer.Ordinal));
        fingerprint = Hash($"{Content(part, targets)}|{drawnFrom}");
        known[part.Uri] = fingerprint;
        return fingerprint;
    }

    // Speaker notes and comments don't appear in the image, a link to another slide doesn't draw that slide,
    // and a master lists all its layouts even though a slide only draws from its own.
    private static bool Draws(OpenXmlPart parent, OpenXmlPart child)
        => child is not (NotesSlidePart or SlideCommentsPart or PowerPointCommentPart or SlidePart)
           && !(parent is SlideMasterPart && child is SlideLayoutPart);

    private static string Content(OpenXmlPart part, Dictionary<string, string> targets)
    {
        using var stream = part.GetStream(FileMode.Open, FileAccess.Read);
        if (part.ContentType == "image/png")
        {
            return HashPng(stream);
        }

        if (!part.ContentType.EndsWith("xml", StringComparison.OrdinalIgnoreCase))
        {
            return Hash(stream);
        }

        var root = XElement.Load(stream, LoadOptions.PreserveWhitespace);
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
        var attributes = element.Attributes()
            .Where(attribute => !attribute.IsNamespaceDeclaration && !EditorFlags.Contains(attribute.Name.LocalName))
            .OrderBy(attribute => attribute.Name.ToString(), StringComparer.Ordinal);
        foreach (var attribute in attributes)
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

    // Normalizing replaces these too, but not when it fails, and a GUID is never drawn.
    [GeneratedRegex(@"\{?[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\}?")]
    private static partial Regex Guid();

    // By pixels, because normalizing recompresses PNGs, so the same picture has different bytes when it fails.
    // A PNG that can't be decoded falls back to its bytes.
    private static string HashPng(Stream stream)
    {
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        try
        {
            var image = ImageResult.FromMemory(bytes.ToArray(), ColorComponents.RedGreenBlueAlpha);
            return Hash($"{image.Width}x{image.Height}{Convert.ToHexString(SHA256.HashData(image.Data))}");
        }
        catch (Exception)
        {
            return Convert.ToHexString(SHA256.HashData(bytes.ToArray()));
        }
    }

    private static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    private static string Hash(Stream stream) => Convert.ToHexString(SHA256.HashData(stream));
}
