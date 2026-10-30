using System.Globalization;

namespace PowerPointCompareForGit;

// A 1-based, inclusive range of slides, from --pages 40 or --pages 2-25.
internal sealed record PageRange(int First, int Last)
{
    internal bool IsSinglePage => First == Last;

    internal IReadOnlyList<int> Pages => Enumerable.Range(First, Last - First + 1).ToList();

    internal static PageRange? Parse(string text)
    {
        var parts = text.Split('-');
        if (parts.Length > 2 || !TryParsePage(parts[0], out var first))
        {
            return null;
        }

        var last = first;
        if (parts.Length == 2 && (!TryParsePage(parts[1], out last) || last < first))
        {
            return null;
        }

        return new PageRange(first, last);
    }

    // Pages past the end of the deck go to the last slide.
    internal PageRange ClampTo(int slideCount) => new(Math.Min(First, slideCount), Math.Min(Last, slideCount));

    private static bool TryParsePage(string text, out int page)
        => int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out page) && page >= 1;
}
