using Avalonia.Controls;

namespace FoturTypingHelper.App;

internal static class IconFactory
{
    internal static WindowIcon Create()
    {
        const int side = 32;
        using var stream = new MemoryStream();
        using (var w = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            var size = 40 + side * side * 4 + side * 4;
            w.Write((ushort)0); w.Write((ushort)1); w.Write((ushort)1);
            w.Write((byte)side); w.Write((byte)side); w.Write((byte)0); w.Write((byte)0);
            w.Write((ushort)1); w.Write((ushort)32); w.Write(size); w.Write(22);
            w.Write(40); w.Write(side); w.Write(side * 2); w.Write((ushort)1); w.Write((ushort)32);
            w.Write(0); w.Write(side * side * 4); w.Write(0); w.Write(0); w.Write(0); w.Write(0);
            for (var y = side - 1; y >= 0; y--)
                for (var x = 0; x < side; x++)
                {
                    var cyan = x is >= 4 and <= 27 && y is >= 4 and <= 27;
                    w.Write((byte)(cyan ? 247 : 8)); w.Write((byte)(cyan ? 255 : 6));
                    w.Write((byte)(cyan ? 98 : 5)); w.Write((byte)255);
                }
            w.Write(new byte[side * 4]);
        }
        stream.Position = 0;
        return new WindowIcon(stream);
    }
}
