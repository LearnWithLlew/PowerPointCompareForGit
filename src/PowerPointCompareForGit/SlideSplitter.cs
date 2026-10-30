using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

namespace PowerPointCompareForGit;

internal static class SlideSplitter
{
    // Writes one copy of the deck per slide (slide0001.pptx, slide0002.pptx, ...), each keeping only that slide.
    // Returns the copies in slide order.
    internal static IReadOnlyList<string> Split(string deckPath, string outputFolder)
        => Split(deckPath, outputFolder, Enumerable.Range(1, CountSlides(deckPath)).ToList());

    // Same, for only the given 1-based slide numbers.
    internal static IReadOnlyList<string> Split(string deckPath, string outputFolder, IReadOnlyList<int> slideNumbers)
    {
        var copies = new List<string>();
        foreach (var slideNumber in slideNumbers)
        {
            var copy = Path.Combine(outputFolder, $"slide{slideNumber:D4}.pptx");
            File.Copy(deckPath, copy, overwrite: true);
            KeepOnly(copy, slideNumber - 1);
            copies.Add(copy);
        }

        return copies;
    }

    internal static int CountSlides(string deckPath)
    {
        using var deck = PresentationDocument.Open(deckPath, false);
        return SlideIds(deck).Count;
    }

    private static void KeepOnly(string path, int keep)
    {
        using var deck = PresentationDocument.Open(path, true);
        var presentationPart = deck.PresentationPart!;
        var slideIds = SlideIds(deck);
        for (var i = 0; i < slideIds.Count; i++)
        {
            if (i == keep)
            {
                continue;
            }

            var slidePart = presentationPart.GetPartById(slideIds[i].RelationshipId!);
            slideIds[i].Remove();
            presentationPart.DeletePart(slidePart);
        }
    }

    private static List<SlideId> SlideIds(PresentationDocument deck)
        => deck.PresentationPart?.Presentation?.SlideIdList?.Elements<SlideId>().ToList() ?? [];
}
