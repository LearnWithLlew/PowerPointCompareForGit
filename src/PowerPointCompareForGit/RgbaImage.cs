using StbImageSharp;
using StbImageWriteSharp;

namespace PowerPointCompareForGit;

// A plain RGBA pixel buffer: 4 bytes per pixel, rows top to bottom.
internal sealed class RgbaImage
{
    internal RgbaImage(int width, int height, Rgba background)
    {
        Width = width;
        Height = height;
        Pixels = new byte[width * height * 4];
        Fill(0, 0, width, height, background);
    }

    private RgbaImage(int width, int height, byte[] pixels)
    {
        Width = width;
        Height = height;
        Pixels = pixels;
    }

    internal int Width { get; }
    internal int Height { get; }
    internal byte[] Pixels { get; }

    internal static RgbaImage LoadPng(string path)
    {
        var result = ImageResult.FromMemory(File.ReadAllBytes(path), StbImageSharp.ColorComponents.RedGreenBlueAlpha);
        return new RgbaImage(result.Width, result.Height, result.Data);
    }

    internal void SavePng(string path)
    {
        using var stream = File.Create(path);
        new ImageWriter().WritePng(Pixels, Width, Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, stream);
    }

    internal Rgba this[int x, int y]
    {
        get
        {
            var i = (y * Width + x) * 4;
            return new Rgba(Pixels[i], Pixels[i + 1], Pixels[i + 2], Pixels[i + 3]);
        }
    }

    // Fills the rectangle, clipped to the image.
    internal void Fill(int left, int top, int width, int height, Rgba color)
    {
        for (var y = Math.Max(0, top); y < Math.Min(Height, top + height); y++)
        {
            for (var x = Math.Max(0, left); x < Math.Min(Width, left + width); x++)
            {
                var i = (y * Width + x) * 4;
                Pixels[i] = color.R;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.B;
                Pixels[i + 3] = color.A;
            }
        }
    }

    // Copies source into this image with its top-left corner at (left, top). The source must fit.
    internal void Paste(RgbaImage source, int left, int top)
    {
        for (var y = 0; y < source.Height; y++)
        {
            Array.Copy(source.Pixels, y * source.Width * 4, Pixels, ((top + y) * Width + left) * 4, source.Width * 4);
        }
    }
}

internal readonly record struct Rgba(byte R, byte G, byte B, byte A = 255);
