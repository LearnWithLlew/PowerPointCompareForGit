using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

namespace PowerPointCompareForGit;

internal static class SlideSplitter
{
    // Writes one copy of the deck per slide (slide0001.pptx, slide0002.pptx, ...), each keeping only that slide.
    // Returns the copies in slide order.
    internal static IReadOnlyList<string> Split(string deckPath, string outputFolder)
    {
        int count;
        using (var deck = PresentationDocument.Open(deckPath, false))
        {
            count = SlideIds(deck).Count;
        }

        var copies = new List<string>();
        for (var keep = 0; keep < count; keep++)
        {
            var copy = Path.Combine(outputFolder, $"slide{keep + 1:D4}.pptx");
            File.Copy(deckPath, copy, overwrite: true);
            KeepOnly(copy, keep);
            copies.Add(copy);
        }

        return copies;
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
