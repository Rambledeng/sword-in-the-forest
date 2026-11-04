namespace SwordInTheForest.Core;

/// <summary>How the game's window is shown (settings.cfg, [display] mode).</summary>
public enum DisplayMode
{
    /// <summary>A window sized to a whole multiple of 480×270.</summary>
    Windowed,
    /// <summary>A borderless window covering the screen.</summary>
    Borderless,
    /// <summary>Exclusive fullscreen.</summary>
    Fullscreen,
}
