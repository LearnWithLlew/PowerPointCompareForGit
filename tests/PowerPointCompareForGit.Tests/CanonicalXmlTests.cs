using DocumentFormat.OpenXml.Packaging;
using static PowerPointCompareForGit.Tests.SlideFingerprintsTests;
using A = DocumentFormat.OpenXml.Drawing;

namespace PowerPointCompareForGit.Tests;

public class CanonicalXmlTests
{
    [Fact]
    public void StoredDateInALayoutIsIgnored()
    {
        using var folder = new TestFolder();
        var baseline = Edit(folder, deck => AddField(deck, "datetimeFigureOut", "10/5/26"));
        var fingerprints = SlideFingerprints.Of(baseline);

        var resaved = Edit(folder, deck => SetFieldText(deck, "10/6/26"), baseline);

        Assert.Equal(fingerprints, SlideFingerprints.Of(resaved));
    }

    [Fact]
    public void OtherFieldTextStillCounts()
    {
        using var folder = new TestFolder();
        var baseline = Edit(folder, deck => AddField(deck, "other", "before"));
        var fingerprints = SlideFingerprints.Of(baseline);

        var edited = Edit(folder, deck => SetFieldText(deck, "after"), baseline);

        Assert.NotEqual(fingerprints, SlideFingerprints.Of(edited));
    }

    [Fact]
    public void EditorFlagsAreIgnored()
    {
        using var folder = new TestFolder();
        var original = SlideFingerprints.Of(TestFolder.ThreeSlideDeck);

        var flagged = Edit(folder, deck => SetFirstRunProperties(deck, new A.RunProperties { Dirty = false, SmartTagClean = true }));

        Assert.NotEqual(original[1], original[0]);
        var withoutFlags = Edit(folder, deck => SetFirstRunProperties(deck, new A.RunProperties()), flagged);
        Assert.Equal(SlideFingerprints.Of(withoutFlags), SlideFingerprints.Of(flagged));
    }

    [Fact]
    public void NamespaceDeclarationPlacementIsIgnored()
    {
        using var folder = new TestFolder();
        const string declaredLow = "<p:extLst xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\"><p:ext uri=\"x\"><q:y xmlns:q=\"urn:q\" /></p:ext></p:extLst>";
        const string declaredHigh = "<p:extLst xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\" xmlns:q=\"urn:q\"><p:ext uri=\"x\"><q:y /></p:ext></p:extLst>";

        var low = Edit(folder, deck => AddSlideExtension(deck, declaredLow));
        var high = Edit(folder, deck => AddSlideExtension(deck, declaredHigh));

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

    private static void SetFirstRunProperties(PresentationDocument deck, A.RunProperties properties)
    {
        var slide = Slide(deck, 0);
        slide.Slide!.Descendants<A.Run>().First().RunProperties = properties;
        slide.Slide.Save();
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
}
