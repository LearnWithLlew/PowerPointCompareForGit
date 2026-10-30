namespace PowerPointCompareForGit;

// A tiny built-in 5x7 bitmap font, so labels render identically on every OS without system fonts.
internal static class PixelFont
{
    private const int Scale = 3;
    private const int GlyphWidth = 5;
    private const int GlyphHeight = 7;

    internal const int Height = GlyphHeight * Scale;

    private static readonly Dictionary<char, string[]> Glyphs = new()
    {
        [' '] = ["     ", "     ", "     ", "     ", "     ", "     ", "     "],
        ['S'] = [" ####", "#    ", "#    ", " ### ", "    #", "    #", "#### "],
        ['l'] = [" ##  ", "  #  ", "  #  ", "  #  ", "  #  ", "  #  ", " ### "],
        ['i'] = ["  #  ", "     ", " ##  ", "  #  ", "  #  ", "  #  ", " ### "],
        ['d'] = ["    #", "    #", " ####", "#   #", "#   #", "#   #", " ####"],
        ['e'] = ["     ", "     ", " ### ", "#   #", "#####", "#    ", " ### "],
        ['0'] = [" ### ", "#   #", "#  ##", "# # #", "##  #", "#   #", " ### "],
        ['1'] = ["  #  ", " ##  ", "  #  ", "  #  ", "  #  ", "  #  ", " ### "],
        ['2'] = [" ### ", "#   #", "    #", "   # ", "  #  ", " #   ", "#####"],
        ['3'] = ["#####", "   # ", "  #  ", "   # ", "    #", "#   #", " ### "],
        ['4'] = ["   # ", "  ## ", " # # ", "#  # ", "#####", "   # ", "   # "],
        ['5'] = ["#####", "#    ", "#### ", "    #", "    #", "#   #", " ### "],
        ['6'] = ["  ## ", " #   ", "#    ", "#### ", "#   #", "#   #", " ### "],
        ['7'] = ["#####", "    #", "   # ", "  #  ", " #   ", " #   ", " #   "],
        ['8'] = [" ### ", "#   #", "#   #", " ### ", "#   #", "#   #", " ### "],
        ['9'] = [" ### ", "#   #", "#   #", " ####", "    #", "   # ", " ##  "]
    };

    internal static void Draw(RgbaImage image, string text, int left, int top, Rgba color)
    {
        for (var c = 0; c < text.Length; c++)
        {
            if (!Glyphs.TryGetValue(text[c], out var glyph))
            {
                throw new ArgumentException($"PixelFont has no glyph for '{text[c]}'.", nameof(text));
            }

            var glyphLeft = left + c * (GlyphWidth + 1) * Scale;
            for (var row = 0; row < GlyphHeight; row++)
            {
                for (var column = 0; column < GlyphWidth; column++)
                {
                    if (glyph[row][column] == '#')
                    {
                        image.Fill(glyphLeft + column * Scale, top + row * Scale, Scale, Scale, color);
                    }
                }
            }
        }
    }
}
