using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using DeterministicIoPackaging;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

namespace PowerPointCompareForGit;

// Two slides with the same fingerprint draw the same, wherever they sit in their decks.
internal static class SlideFingerprints
{
    private static readonly XNamespace Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PresentationMl = "http://schemas.openxmlformats.org/presentationml/2006/main";

    // One fingerprint per slide, in slide order.
    internal static IReadOnlyList<string> Of(string deckPath)
    {
        using var source = File.OpenRead(deckPath);
        using var normalized = DeterministicPackage.Convert(source);
        using var deck = PresentationDocument.Open(normalized, false);
        var presentationPart = deck.PresentationPart!;
        var presentation = presentationPart.Presentation!;
        var deckWide = $"{presentation.SlideSize?.OuterXml}{presentation.DefaultTextStyle?.OuterXml}";
        var known = new Dictionary<Uri, string>();
        return presentation.SlideIdList?.Elements<SlideId>()
            .Select(slideId => Hash($"{deckWide}{Fingerprint(presentationPart.GetPartById(slideId.RelationshipId!), known)}"))
            .ToList() ?? [];
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
        if (!part.ContentType.EndsWith("xml", StringComparison.OrdinalIgnoreCase))
        {
            return Hash(stream);
        }

        var root = XElement.Load(stream, LoadOptions.PreserveWhitespace);
        root.Elements(PresentationMl + "sldLayoutIdLst").Remove();
        foreach (var attribute in root.DescendantsAndSelf().Attributes().Where(attribute => attribute.Name.Namespace == Relationships))
        {
            attribute.Value = targets.GetValueOrDefault(attribute.Value, "");
        }

        return root.ToString(SaveOptions.DisableFormatting);
    }

    private static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    private static string Hash(Stream stream) => Convert.ToHexString(SHA256.HashData(stream));
}
