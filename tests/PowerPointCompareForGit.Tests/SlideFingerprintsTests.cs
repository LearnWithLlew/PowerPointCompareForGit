using System.IO.Compression;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;

namespace PowerPointCompareForGit.Tests;

public class SlideFingerprintsTests
{
    [Fact]
    public void DifferentSlidesHaveDifferentFingerprints()
    {
        Assert.Equal(3, SlideFingerprints.Of(TestFolder.ThreeSlideDeck).Distinct().Count());
    }

    [Fact]
    public void CompressionAndEntryOrderDontMatter()
    {
        using var folder = new TestFolder();
        var rezipped = folder.Combine("rezipped.pptx");
        using (var source = ZipFile.OpenRead(TestFolder.ThreeSlideDeck))
        using (var target = ZipFile.Open(rezipped, ZipArchiveMode.Create))
        {
            foreach (var entry in source.Entries.Reverse())
            {
                using var from = entry.Open();
                using var to = target.CreateEntry(entry.FullName, CompressionLevel.NoCompression).Open();
                from.CopyTo(to);
            }
        }

        Assert.Equal(SlideFingerprints.Of(TestFolder.ThreeSlideDeck), SlideFingerprints.Of(rezipped));
    }

    [Fact]
    public void MovedSlideKeepsItsFingerprint()
    {
        using var folder = new TestFolder();
        var original = SlideFingerprints.Of(TestFolder.ThreeSlideDeck);

        var moved = Edit(folder, deck =>
        {
            var list = deck.PresentationPart!.Presentation!.SlideIdList!;
            var last = list.Elements<SlideId>().Last();
            last.Remove();
            list.PrependChild(last);
        });

        Assert.Equal([original[2], original[0], original[1]], SlideFingerprints.Of(moved));
    }

    [Fact]
    public void EditChangesOnlyThatSlide()
    {
        using var folder = new TestFolder();
        var original = SlideFingerprints.Of(TestFolder.ThreeSlideDeck);

        var edited = Edit(folder, deck =>
        {
            var slide = Slide(deck, 1);
            slide.Slide!.Descendants<A.Text>().First().Text = "Edited";
            slide.Slide.Save();
        });

        var fingerprints = SlideFingerprints.Of(edited);
        Assert.Equal(original[0], fingerprints[0]);
        Assert.NotEqual(original[1], fingerprints[1]);
        Assert.Equal(original[2], fingerprints[2]);
    }

    [Fact]
    public void SpeakerNotesAreIgnored()
    {
        using var folder = new TestFolder();

        var withNotes = Edit(folder, deck =>
        {
            var notes = Slide(deck, 0).AddNewPart<NotesSlidePart>();
            notes.NotesSlide = new NotesSlide(new CommonSlideData(new ShapeTree()));
            notes.NotesSlide.Save();
        });

        Assert.Equal(SlideFingerprints.Of(TestFolder.ThreeSlideDeck), SlideFingerprints.Of(withNotes));
    }

    [Fact]
    public void MasterEditChangesEverySlide()
    {
        using var folder = new TestFolder();
        var original = SlideFingerprints.Of(TestFolder.ThreeSlideDeck);

        var edited = Edit(folder, deck =>
        {
            var master = deck.PresentationPart!.SlideMasterParts.First().SlideMaster!;
            master.CommonSlideData!.Name = "Edited";
            master.Save();
        });

        Assert.All(SlideFingerprints.Of(edited).Zip(original), pair => Assert.NotEqual(pair.Second, pair.First));
    }

    [Fact]
    public void SlideSizeChangesEverySlide()
    {
        using var folder = new TestFolder();
        var original = SlideFingerprints.Of(TestFolder.ThreeSlideDeck);

        var resized = Edit(folder, deck =>
        {
            var presentation = deck.PresentationPart!.Presentation!;
            presentation.SlideSize!.Cx = presentation.SlideSize.Cx! + 12700;
            presentation.Save();
        });

        Assert.All(SlideFingerprints.Of(resized).Zip(original), pair => Assert.NotEqual(pair.Second, pair.First));
    }

    private static string Edit(TestFolder folder, Action<PresentationDocument> edit)
    {
        var path = folder.Combine("edited.pptx");
        File.Copy(TestFolder.ThreeSlideDeck, path);
        using (var deck = PresentationDocument.Open(path, true))
        {
            edit(deck);
        }

        return path;
    }

    private static SlidePart Slide(PresentationDocument deck, int index)
    {
        var presentationPart = deck.PresentationPart!;
        var slideId = presentationPart.Presentation!.SlideIdList!.Elements<SlideId>().ElementAt(index);
        return (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
    }
}
