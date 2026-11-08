namespace PowerPointCompareForGit;

// One row of a selective compare. A null side is drawn as a blank.
internal sealed record SlideRow(int? Left, int? Right);

// An unchanged slide that changed places relative to the others.
internal sealed record SlideMove(int From, int To);

// What a selective compare shows. All slide numbers are 1-based.
internal sealed record SlideAlignment(IReadOnlyList<SlideRow> Rows, IReadOnlyList<SlideMove> Moves)
{
    private const int MovesListed = 10;

    // What the images can't show. leftName and rightName describe the two versions.
    internal IReadOnlyList<string> Messages(string leftName, string rightName)
    {
        var messages = new List<string>();
        if (Rows.Count == 0)
        {
            messages.Add(Moves.Count == 0 ? $"No visible slide changes between {leftName} and {rightName}." : "Slides were reordered, but none changed.");
        }

        if (Moves.Count > 0)
        {
            var listed = string.Join(", ", Moves.Take(MovesListed).Select(move => $"{move.From}→{move.To}"));
            messages.Add(Moves.Count > MovesListed
                ? $"Slides moved but unchanged: {listed} ...and {Moves.Count - MovesListed} more."
                : $"Slides moved but unchanged: {listed}.");
        }

        return messages;
    }

    internal static SlideAlignment Of(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        var fixedPoints = LongestCommonSubsequence(left, right);
        var moves = FindMoves(left, right, fixedPoints);
        var movedLeft = moves.Select(move => move.From).ToHashSet();
        var movedRight = moves.Select(move => move.To).ToHashSet();
        var rows = Gaps(fixedPoints, left.Count, right.Count)
            .SelectMany(gap => SideBySide(
                gap.Left.Where(slide => !movedLeft.Contains(slide)).ToList(),
                gap.Right.Where(slide => !movedRight.Contains(slide)).ToList()))
            .ToList();
        return new SlideAlignment(rows, moves);
    }

    private static IEnumerable<(IEnumerable<int> Left, IEnumerable<int> Right)> Gaps(List<(int Left, int Right)> fixedPoints, int leftCount, int rightCount)
    {
        List<(int Left, int Right)> bounds = [(0, 0), .. fixedPoints, (leftCount + 1, rightCount + 1)];
        return bounds.Zip(bounds.Skip(1), (from, to) => (Between(from.Left, to.Left), Between(from.Right, to.Right)));
    }

    private static IEnumerable<int> Between(int from, int to) => Enumerable.Range(from + 1, to - from - 1);

    private static IEnumerable<SlideRow> SideBySide(List<int> left, List<int> right)
        => Enumerable.Range(0, Math.Max(left.Count, right.Count))
            .Select(i => new SlideRow(i < left.Count ? left[i] : null, i < right.Count ? right[i] : null));

    // The longest run of identical slides in the same order in both decks, as pairs of 1-based slide numbers.
    private static List<(int Left, int Right)> LongestCommonSubsequence(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        var lengths = CommonLengths(left, right);
        var pairs = new List<(int, int)>();
        var (i, j) = (0, 0);
        while (i < left.Count && j < right.Count)
        {
            if (left[i] == right[j])
            {
                pairs.Add((++i, ++j));
            }
            else if (lengths[i + 1, j] >= lengths[i, j + 1])
            {
                i++;
            }
            else
            {
                j++;
            }
        }

        return pairs;
    }

    private static int[,] CommonLengths(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        var lengths = new int[left.Count + 1, right.Count + 1];
        for (var l = left.Count - 1; l >= 0; l--)
        {
            for (var r = right.Count - 1; r >= 0; r--)
            {
                lengths[l, r] = left[l] == right[r]
                    ? lengths[l + 1, r + 1] + 1
                    : Math.Max(lengths[l + 1, r], lengths[l, r + 1]);
            }
        }

        return lengths;
    }

    // Slides outside the fixed points that still have an identical slide on the other side.
    // Each slide matches only one, so an extra copy of a duplicate is left over and shows as added.
    private static List<SlideMove> FindMoves(IReadOnlyList<string> left, IReadOnlyList<string> right, List<(int Left, int Right)> fixedPoints)
    {
        var fixedRight = fixedPoints.Select(pair => pair.Right).ToHashSet();
        var unmatchedRight = Enumerable.Range(1, right.Count)
            .Where(slide => !fixedRight.Contains(slide))
            .ToLookup(slide => right[slide - 1])
            .ToDictionary(group => group.Key, group => new Queue<int>(group));

        var fixedLeft = fixedPoints.Select(pair => pair.Left).ToHashSet();
        var moves = new List<SlideMove>();
        foreach (var slide in Enumerable.Range(1, left.Count).Where(slide => !fixedLeft.Contains(slide)))
        {
            if (unmatchedRight.TryGetValue(left[slide - 1], out var candidates) && candidates.TryDequeue(out var target))
            {
                moves.Add(new SlideMove(slide, target));
            }
        }

        return moves;
    }
}
