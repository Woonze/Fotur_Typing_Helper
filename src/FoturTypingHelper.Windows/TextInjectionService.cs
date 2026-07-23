using System.Runtime.InteropServices;
using FoturTypingHelper.Core;

namespace FoturTypingHelper.Windows;

public sealed class TextInjectionService : ITextInjectionService
{
    internal static readonly UIntPtr InjectionMarker = new(0xF07A2026u);

    public bool ActivateWindow(IntPtr window) => window != IntPtr.Zero && NativeMethods.SetForegroundWindow(window);
    public void ReplacePrevious(string original, string replacement, TextLanguage language, IntPtr targetWindow)
    {
        ReplacePreviousCharacters(original.Length, replacement, language, targetWindow);
    }

    public void ReplacePreviousCharacters(int charactersToDelete, string replacement, TextLanguage language, IntPtr targetWindow)
    {
        SendBackspaces(charactersToDelete);
        SendText(replacement);
        if (language != TextLanguage.Unknown) SwitchLayout(language, targetWindow);
    }

    public void SendBackspaces(int count)
    {
        var inputs = new List<NativeMethods.Input>(count * 2);
        for (var i = 0; i < count; i++)
        {
            inputs.Add(Key((ushort)NativeMethods.VkBack, 0));
            inputs.Add(Key((ushort)NativeMethods.VkBack, NativeMethods.KeyeventfKeyup));
        }
        Send(inputs);
    }

    public bool SendText(string text)
    {
        var inputs = new List<NativeMethods.Input>(text.Length * 2);
        foreach (var character in text)
        {
            inputs.Add(Key(0, NativeMethods.KeyeventfUnicode, character));
            inputs.Add(Key(0, NativeMethods.KeyeventfUnicode | NativeMethods.KeyeventfKeyup, character));
        }
        return Send(inputs);
    }

    private static void SwitchLayout(TextLanguage language, IntPtr target)
    {
        var id = language == TextLanguage.Russian ? "00000419" : "00000409";
        var layout = NativeMethods.LoadKeyboardLayout(id, 1);
        if (layout != IntPtr.Zero)
            NativeMethods.PostMessage(target, NativeMethods.WmInputLangChangeRequest, IntPtr.Zero, layout);
    }

    private static NativeMethods.Input Key(ushort vk, uint flags, char scan = '\0') => new()
    {
        Type = NativeMethods.InputKeyboard,
        Union = new NativeMethods.InputUnion
        {
            Keyboard = new NativeMethods.KeyboardInput { Vk = vk, Scan = scan, Flags = flags, ExtraInfo = InjectionMarker }
        }
    };

    private static bool Send(IReadOnlyCollection<NativeMethods.Input> inputs)
    {
        if (inputs.Count == 0) return true;
        var sent = NativeMethods.SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<NativeMethods.Input>());
        if (sent != inputs.Count)
            DiagnosticLog.Write("TextInjection", new InvalidOperationException(
                $"Windows приняла только {sent} из {inputs.Count} событий ввода."));
        return sent == inputs.Count;
    }
}
