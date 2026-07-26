# Архитектура 1.3.0

Fotur Typing Helper состоит из переносимого ядра, Avalonia UI и платформенных адаптеров.

## Проекты

- `FoturTypingHelper.Core` — настройки, scoring раскладки, конвертер EN↔RU, обработка диктовки, контракты платформ.
- `FoturTypingHelper.App` — Avalonia UI, tray, runtime orchestration, updater.
- `FoturTypingHelper.Windows` — low-level keyboard hook, SendInput, NAudio, Windows autostart.
- `FoturTypingHelper.Mac` — CGEvent tap/post, OpenAL capture, macOS privacy helpers, CoreML/CPU Whisper.
- `FoturTypingHelper.Linux` — experimental services: `arecord`, `xdotool`, Linux Whisper CPU runtime.

## Автообновление

Updater читает `/releases/latest`, сравнивает SemVer, выбирает asset текущей платформы и сверяет SHA-256:

- Windows x64: `win-x64.exe`
- macOS ARM64: `macos-arm64.zip`
- macOS Intel: `macos-x64.zip`
- Linux x64: `linux-x64.tar.gz`

Windows запускает installer. macOS заменяет установленный `.app` через helper-скрипт. Linux пока только скачивает и проверяет архив, потому что схема установки tar.gz зависит от выбранной пользователем директории.

## Упаковка

Build scripts после `dotnet publish` удаляют runtime-каталоги нецелевых платформ и выносят `.pdb` в отдельные symbols-артефакты. Пользовательские пакеты включают `LICENSE` и `THIRD_PARTY_NOTICES.md`.

Версия берётся из `Directory.Build.props`. Inno Setup получает её через `/DAppVersion=...`, чтобы installer не расходился с assembly/package version.
