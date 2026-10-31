namespace PowerPointCompareForGit;

// One row of a selective compare. A null side is drawn as a blank.
internal sealed record SlideRow(int? Left, int? Right);

// An unchanged slide that changed places relative to the others.
internal sealed record SlideMove(int From, int To);

// What a selective compare shows. All slide numbers are 1-based.
internal sealed record SlideAlignment(IReadOnlyList<SlideRow> Rows, IReadOnlyList<SlideMove> Moves)
{
    internal static SlideAlignment Of(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        var fixedPoints = LongestCommonSubsequence(left, right);
        var moves = FindMoves(left, right, fixedPoints);
        var movedLeft = moves.Select(move => move.From).ToHashSet();
        var movedRight = moves.Select(move => move.To).ToHashSet();

        var rows = new List<SlideRow>();
        var nextLeft = 1;
        var nextRight = 1;
        foreach (var (fixedLeft, fixedRight) in fixedPoints.Append((left.Count + 1, right.Count + 1)))
        {
            var changedLeft = Enumerable.Range(nextLeft, fixedLeft - nextLeft).Where(slide => !movedLeft.Contains(slide)).ToList();
            var changedRight = Enumerable.Range(nextRight, fixedRight - nextRight).Where(slide => !movedRight.Contains(slide)).ToList();
            for (var i = 0; i < Math.Max(changedLeft.Count, changedRight.Count); i++)
            {
                rows.Add(new SlideRow(i < changedLeft.Count ? changedLeft[i] : null, i < changedRight.Count ? changedRight[i] : null));
            }

            nextLeft = fixedLeft + 1;
            nextRight = fixedRight + 1;
        }

        return new SlideAlignment(rows, moves);
    }

    // The longest run of identical slides in the same order in both decks, as pairs of 1-based slide numbers.
    private static List<(int Left, int Right)> LongestCommonSubsequence(IReadOnlyList<string> left, IReadOnlyList<string> right)
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
