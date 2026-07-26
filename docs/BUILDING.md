# Сборка и проверка 1.3.0

## Windows x64

Требуются .NET 8 SDK, Windows 10/11 x64 и Inno Setup 6.

```powershell
dotnet restore
dotnet test FoturTypingHelper.sln -c Release
./scripts/build-release.ps1
```

Результаты:

- `artifacts/installer/FoturTypingHelper-Setup-1.3.0-win-x64.exe`
- `artifacts/FoturTypingHelper-1.3.0-win-x64-portable.zip`
- `artifacts/SHA256SUMS.txt`

Проверка опубликованного stable release:

```powershell
.\scripts\verify-release-feed.ps1 -ExpectedVersion 1.3.0
```

## macOS Apple Silicon и Intel

На соответствующем Mac с .NET 8 SDK:

```bash
./scripts/build-macos.sh osx-arm64
./scripts/build-macos.sh osx-x64
```

Скрипт запускает unit tests, делает self-contained publish, формирует `.app`, проверяет `Info.plist`, `libwhisper.dylib`, архитектуру исполняемых файлов, ad-hoc codesign, затем создаёт ZIP и DMG.

GitHub Actions выполняет сборки раздельно: `macos-15` для arm64 и `macos-15-intel` для x64.

## Linux x64

На Linux с .NET 8 SDK:

```bash
./scripts/build-linux.sh linux-x64
```

Результаты:

- `artifacts/FoturTypingHelper-1.3.0-linux-x64.tar.gz`
- `artifacts/SHA256SUMS-linux-x64.txt`

Linux artifact проверяет unit tests, self-contained publish и наличие `libwhisper.so`. Runtime-зависимости для пользователя: `alsa-utils` для записи (`arecord`) и `xdotool` для вставки текста на X11.

## Smoke-тесты

Браузерный стенд Windows:

```powershell
dotnet run --project tests/FoturTypingHelper.BrowserSmoke -c Release -- artifacts/publish/FoturTypingHelper.App.exe
```

Полный ручной тест локальной диктовки:

```powershell
dotnet run --project tests/FoturTypingHelper.DictationSmoke -c Release
```
