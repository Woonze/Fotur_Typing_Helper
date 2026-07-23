using Avalonia.Controls;
using Avalonia.Platform;

namespace FoturTypingHelper.App;

internal static class IconFactory
{
    internal static WindowIcon Create()
    {
        using var stream = AssetLoader.Open(
            new Uri("avares://FoturTypingHelper.App/Assets/FoturTypingHelper.png"));
        return new WindowIcon(stream);
    }
}
