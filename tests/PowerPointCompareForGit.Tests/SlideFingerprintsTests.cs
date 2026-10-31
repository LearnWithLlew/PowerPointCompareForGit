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
    public void StoredDateInALayoutIsIgnored()
    {
        using var folder = new TestFolder();
        var baseline = Edit(folder, deck => AddField(deck, "datetimeFigureOut", "10/5/26"));
        var fingerprints = SlideFingerprints.Of(baseline);

        var resaved = Edit(folder, deck => SetFieldText(deck, "10/6/26"), baseline, "resaved.pptx");

        Assert.Equal(fingerprints, SlideFingerprints.Of(resaved));
    }

    [Fact]
    public void OtherFieldTextStillCounts()
    {
        using var folder = new TestFolder();
        var baseline = Edit(folder, deck => AddField(deck, "other", "before"));
        var fingerprints = SlideFingerprints.Of(baseline);

        var edited = Edit(folder, deck => SetFieldText(deck, "after"), baseline, "edited2.pptx");

        Assert.NotEqual(fingerprints, SlideFingerprints.Of(edited));
    }

    [Fact]
    public void EditorFlagsAreIgnored()
    {
        using var folder = new TestFolder();
        var original = SlideFingerprints.Of(TestFolder.ThreeSlideDeck);

        var flagged = Edit(folder, deck =>
        {
            var slide = Slide(deck, 0);
            var run = slide.Slide!.Descendants<A.Run>().First();
            run.RunProperties = new A.RunProperties { Dirty = false, SmartTagClean = true };
            slide.Slide.Save();
        });

        Assert.NotEqual(original[1], original[0]);
        var withoutFlags = Edit(folder, deck =>
        {
            var slide = Slide(deck, 0);
            slide.Slide!.Descendants<A.Run>().First().RunProperties = new A.RunProperties();
            slide.Slide.Save();
        }, flagged, "cleared.pptx");
        Assert.Equal(SlideFingerprints.Of(withoutFlags), SlideFingerprints.Of(flagged));
    }

    [Fact]
    public void NamespaceDeclarationPlacementIsIgnored()
    {
        using var folder = new TestFolder();
        const string declaredLow = "<p:extLst xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\"><p:ext uri=\"x\"><q:y xmlns:q=\"urn:q\" /></p:ext></p:extLst>";
        const string declaredHigh = "<p:extLst xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\" xmlns:q=\"urn:q\"><p:ext uri=\"x\"><q:y /></p:ext></p:extLst>";

        var low = Edit(folder, deck => AddSlideExtension(deck, declaredLow));
        var high = Edit(folder, deck => AddSlideExtension(deck, declaredHigh), TestFolder.ThreeSlideDeck, "high.pptx");

        Assert.Equal(SlideFingerprints.Of(low), SlideFingerprints.Of(high));
    }

    [Fact]
    public void AnimationsAndTransitionsAreIgnored()
    {
        using var folder = new TestFolder();
        const string transition = "<p:transition xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\" spd=\"slow\" />";
        const string timing = "<p:timing xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\" />";
        const string wrapped = "<mc:AlternateContent xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\"><mc:Fallback><p:transition xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\" /></mc:Fallback></mc:AlternateContent>";

        var animated = Edit(folder, deck => AddSlideExtension(deck, $"{transition}{wrapped}{timing}"));

        Assert.Equal(SlideFingerprints.Of(TestFolder.ThreeSlideDeck), SlideFingerprints.Of(animated));
    }

    [Fact]
    public void PngIsComparedByPixelsNotBytes()
    {
        using var folder = new TestFolder();
        var png = PngOf(new Rgba(255, 0, 0), folder);

        var fingerprints = SlideFingerprints.Of(Edit(folder, deck => AddPicture(deck, png)));
        var recompressed = SlideFingerprints.Of(Edit(folder, deck => AddPicture(deck, Recompress(png)), TestFolder.ThreeSlideDeck, "recompressed.pptx"));
        var otherPixels = SlideFingerprints.Of(Edit(folder, deck => AddPicture(deck, PngOf(new Rgba(0, 0, 255), folder)), TestFolder.ThreeSlideDeck, "other.pptx"));

        Assert.Equal(fingerprints, recompressed);
        Assert.NotEqual(fingerprints[0], otherPixels[0]);
    }

    [Fact]
    public void PictureThatCantBeNormalizedStillFingerprints()
    {
        using var folder = new TestFolder();
        var original = SlideFingerprints.Of(TestFolder.ThreeSlideDeck);
        byte[] cutOffPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, 0x49, 0x48, 0x44, 0x52, 0, 0];

        var withPicture = Edit(folder, deck => AddPicture(deck, cutOffPng));

        var fingerprints = SlideFingerprints.Of(withPicture);
        Assert.NotEqual(original[0], fingerprints[0]);
        Assert.Equal(original[1], fingerprints[1]);
        Assert.Equal(original[2], fingerprints[2]);
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

    private static string Edit(TestFolder folder, Action<PresentationDocument> edit, string? source = null, string name = "edited.pptx")
    {
        var path = folder.Combine(name);
        File.Copy(source ?? TestFolder.ThreeSlideDeck, path);
        using (var deck = PresentationDocument.Open(path, true))
        {
            edit(deck);
        }

        return path;
    }

    private static void AddPicture(PresentationDocument deck, byte[] png)
    {
        using var stream = new MemoryStream(png);
        Slide(deck, 0).AddImagePart("image/png").FeedData(stream);
    }

    private static byte[] PngOf(Rgba color, TestFolder folder)
    {
        var path = folder.Combine("picture.png");
        new RgbaImage(4, 4, color).SavePng(path);
        return File.ReadAllBytes(path);
    }

    // The same pixels with different compressed bytes. Assumes one IDAT chunk. The chunk's checksum is left empty.
    private static byte[] Recompress(byte[] png)
    {
        var start = png.AsSpan().IndexOf("IDAT"u8) - 4;
        var length = (png[start] << 24) | (png[start + 1] << 16) | (png[start + 2] << 8) | png[start + 3];
        using var inflated = new MemoryStream();
        using (var zlib = new ZLibStream(new MemoryStream(png, start + 8, length), CompressionMode.Decompress))
        {
            zlib.CopyTo(inflated);
        }

        using var deflated = new MemoryStream();
        using (var zlib = new ZLibStream(deflated, CompressionLevel.NoCompression, leaveOpen: true))
        {
            zlib.Write(inflated.ToArray());
        }

        var data = deflated.ToArray();
        byte[] header = [(byte)(data.Length >> 24), (byte)(data.Length >> 16), (byte)(data.Length >> 8), (byte)data.Length, .. "IDAT"u8];
        return [.. png.AsSpan(0, start), .. header, .. data, 0, 0, 0, 0, .. png.AsSpan(start + 8 + length + 4)];
    }

    private static void AddField(PresentationDocument deck, string type, string text)
    {
        var layout = Slide(deck, 0).SlideLayoutPart!.SlideLayout!;
        layout.Descendants<A.Paragraph>().First().AppendChild(new A.Field(new A.Text(text)) { Id = "{B6F15528-21DE-4FAA-801E-634DDDAF4B2B}", Type = type });
        layout.Save();
    }

    private static void SetFieldText(PresentationDocument deck, string text)
    {
        var layout = Slide(deck, 0).SlideLayoutPart!.SlideLayout!;
        layout.Descendants<A.Field>().First().GetFirstChild<A.Text>()!.Text = text;
        layout.Save();
    }

    private static void AddSlideExtension(PresentationDocument deck, string xml)
    {
        var slide = Slide(deck, 0);
        using var stream = slide.GetStream(FileMode.Open, FileAccess.Read);
        var content = new StreamReader(stream).ReadToEnd();
        stream.Close();
        var patched = content.Replace("</p:sld>", $"{xml}</p:sld>");
        using var target = slide.GetStream(FileMode.Create, FileAccess.Write);
        using var writer = new StreamWriter(target);
        writer.Write(patched);
    }

    private static SlidePart Slide(PresentationDocument deck, int index)
    {
        var presentationPart = deck.PresentationPart!;
        var slideId = presentationPart.Presentation!.SlideIdList!.Elements<SlideId>().ElementAt(index);
        return (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
    }
}
