# Сборка и проверка 1.0.0

## Windows x64

Требуются .NET 8 SDK, Windows 10/11 x64 и Inno Setup 6.

```powershell
dotnet restore
dotnet test FoturTypingHelper.sln -c Release
./scripts/build-release.ps1
```

Результаты:

- `artifacts/installer/FoturTypingHelper-Setup-1.0.0-win-x64.exe`
- `artifacts/FoturTypingHelper-1.0.0-win-x64-portable.zip`
- `artifacts/SHA256SUMS.txt`

Реальный браузерный стенд:

```powershell
dotnet run --project tests/FoturTypingHelper.BrowserSmoke -c Release -- artifacts/publish/FoturTypingHelper.App.exe
```

Он вводит 150 русских и английских фраз с интервалом 35 мс. Перед каждой фразой через `GetKeyboardLayout` подтверждается противоположная раскладка, затем результат читается из настоящего Chrome input. Фраза завершается пробелом, потому что надёжная коррекция запускается на границе слова.

Полный ручной тест локальной диктовки:

```powershell
dotnet run --project tests/FoturTypingHelper.DictationSmoke -c Release
```

## macOS Apple Silicon и Intel

На соответствующем Mac с .NET 8 SDK:

```bash
./scripts/build-macos.sh osx-arm64
./scripts/build-macos.sh osx-x64
```

Скрипт запускает unit tests, делает self-contained publish, формирует `.app`, проверяет `Info.plist`, наличие `libwhisper.dylib`, архитектуру исполняемых файлов и ad-hoc codesign, затем создаёт ZIP и DMG с ярлыком Applications.

GitHub Actions выполняет эти команды раздельно: `macos-15` для arm64 и `macos-15-intel` для x64. Это проверяет сборку на обеих архитектурах, но не заменяет ручной тест микрофона, Accessibility/Input Monitoring, горячих клавиш, tray и автообновления на физическом Mac пользователя.

Перед повторным тестом проблемного DMG удалите старую запись Fotur из «Мониторинг ввода» и «Универсальный доступ», добавьте приложение из `/Applications` заново, включите оба переключателя и полностью перезапустите Fotur. Затем отдельно проверьте: глобальный хоткей вне окна программы, обычную диктовку в TextEdit/Safari и автозамену `ghbdtn` → `привет`. Счётчик исправлений не должен расти, если macOS заблокировала отправку текста.

Для публичной бесшовной установки нужны Developer ID Application, hardened runtime, notarization и stapling. Секреты сертификата в репозиторий добавлять нельзя; они настраиваются только через GitHub Actions Secrets.
