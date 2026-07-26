# Third-party notices

Fotur Typing Helper includes or depends on third-party open-source components. This file is a practical notice list for release packages; authoritative license text remains in each upstream project/package.

## Runtime and framework

- .NET Runtime / SDK — MIT license — https://github.com/dotnet/runtime
- Avalonia UI — MIT license — https://github.com/AvaloniaUI/Avalonia
- Avalonia.Fonts.Inter — SIL Open Font License / upstream font license — https://github.com/rsms/inter

## Windows

- NAudio — MIT license — https://github.com/naudio/NAudio

## Dictation

- Whisper.net — MIT license — https://github.com/sandrohanea/whisper.net
- whisper.cpp native runtime used by Whisper.net — MIT license — https://github.com/ggerganov/whisper.cpp

## macOS/Linux helpers

- Platform APIs and command-line tools such as OpenAL, `arecord` and `xdotool` are used when available on the user system; they are not vendored by Fotur unless present in the packaged .NET/native dependency output.
