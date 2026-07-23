# Публикация Fotur Typing Helper 1.0.0

## Подготовка

1. Убедиться, что версия одинакова в `Directory.Build.props` и `installer/FoturTypingHelper.iss`.
2. Дождаться зелёных `Windows build` и обеих matrix-задач `macOS build` для нужного коммита.
3. Скачать артефакты Actions и собрать в одной папке Windows Setup/portable, два macOS ZIP, два DMG и три SHA-файла.
4. Вручную проверить Windows installer/portable и хотя бы один физический Mac. Для Intel и Apple Silicon минимум проверить запуск, три разрешения, автокоррекцию, диктовку, tray и обновление.
5. Перечитать `RELEASE_NOTES.md`. Не объявлять notarization или подпись, пока сертификаты реально не подключены.

## Через сайт GitHub

1. Открыть репозиторий → **Releases** → **Draft a new release**.
2. Нажать **Choose a tag**, ввести `v1.0.0`, выбрать **Create new tag on publish** и ветку `main`.
3. Title: `Fotur Typing Helper 1.0.0`.
4. Вставить содержимое `RELEASE_NOTES.md`.
5. Приложить:
   - `FoturTypingHelper-Setup-1.0.0-win-x64.exe`
   - `FoturTypingHelper-1.0.0-win-x64-portable.zip`
   - `FoturTypingHelper-1.0.0-macos-arm64.dmg` и `.zip`
   - `FoturTypingHelper-1.0.0-macos-x64.dmg` и `.zip`
   - `SHA256SUMS.txt`, `SHA256SUMS-macos-arm64.txt`, `SHA256SUMS-macos-x64.txt`
6. Для финальной 1.0.0 не ставить **pre-release**. Нажать **Publish release**.

Имена ZIP и checksum-файлов важны: встроенный updater ищет их по этим суффиксам.

## Через GitHub CLI

```powershell
gh release create v1.0.0 `
  artifacts/installer/FoturTypingHelper-Setup-1.0.0-win-x64.exe `
  artifacts/FoturTypingHelper-1.0.0-win-x64-portable.zip `
  artifacts/FoturTypingHelper-1.0.0-macos-arm64.dmg `
  artifacts/FoturTypingHelper-1.0.0-macos-arm64.zip `
  artifacts/FoturTypingHelper-1.0.0-macos-x64.dmg `
  artifacts/FoturTypingHelper-1.0.0-macos-x64.zip `
  artifacts/SHA256SUMS.txt `
  artifacts/SHA256SUMS-macos-arm64.txt `
  artifacts/SHA256SUMS-macos-x64.txt `
  --repo Woonze/Fotur_Typing_Helper `
  --title "Fotur Typing Helper 1.0.0" `
  --notes-file RELEASE_NOTES.md
```

Автообновление проверяет только опубликованный стабильный `/releases/latest`; draft и prerelease пользователям не предлагаются.
