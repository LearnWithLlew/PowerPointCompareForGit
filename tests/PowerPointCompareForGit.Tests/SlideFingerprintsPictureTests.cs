using System.IO.Compression;
using DocumentFormat.OpenXml.Packaging;
using static PowerPointCompareForGit.Tests.SlideFingerprintsTests;

namespace PowerPointCompareForGit.Tests;

public class SlideFingerprintsPictureTests
{
    [Fact]
    public void PngIsComparedByPixelsNotBytes()
    {
        using var folder = new TestFolder();
        var png = PngOf(new Rgba(255, 0, 0), folder);

        var fingerprints = SlideFingerprints.Of(Edit(folder, deck => AddPicture(deck, png)));
        var recompressed = SlideFingerprints.Of(Edit(folder, deck => AddPicture(deck, Recompress(png))));
        var otherPixels = SlideFingerprints.Of(Edit(folder, deck => AddPicture(deck, PngOf(new Rgba(0, 0, 255), folder))));

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
}
