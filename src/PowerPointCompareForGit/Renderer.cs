namespace PowerPointCompareForGit;

// What draws the slides, chosen with --renderer.
internal enum Renderer
{
    LibreOffice,
    DeckCraft
}

// Draws the given 1-based slides of a deck, in order. work is an empty temp folder it may use.
internal interface ISlideRenderer
{
    List<RgbaImage> RenderSlides(string deckPath, IReadOnlyList<int> slideNumbers, string work);
}
