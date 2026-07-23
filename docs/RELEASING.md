# Публикация Fotur Typing Helper 1.1.0

## Перед релизом

1. Убедиться, что версия `1.1.0` одинакова в `Directory.Build.props`, installer, manifest, UI и документах.
2. Дождаться зелёных Windows и обеих macOS matrix-задач для коммита из `main`.
3. Скачать артефакты Actions: Windows Setup/portable, два macOS DMG/ZIP и checksum-файлы.
4. Вручную проверить Windows installer/portable и новый DMG хотя бы на одном физическом Mac.
5. Для Intel и Apple Silicon проверить запуск из «Программ», три разрешения, автокоррекцию, удержание хоткея, диктовку, tray и проверку обновлений.
6. Перечитать `RELEASE_NOTES.md`. Не объявлять Developer ID, notarization или физическую проверку, если их не было.

## Через сайт GitHub

1. Открыть репозиторий → **Releases** → **Draft a new release**.
2. Нажать **Choose a tag**, ввести `v1.1.0`, выбрать **Create new tag on publish** и ветку `main`.
3. Title: `Fotur Typing Helper 1.1.0`.
4. Вставить содержимое `RELEASE_NOTES.md`.
5. Приложить:
   - `FoturTypingHelper-Setup-1.1.0-win-x64.exe`
   - `FoturTypingHelper-1.1.0-win-x64-portable.zip`
   - `FoturTypingHelper-1.1.0-macos-arm64.dmg` и `.zip`
   - `FoturTypingHelper-1.1.0-macos-x64.dmg` и `.zip`
   - `SHA256SUMS.txt`, `SHA256SUMS-macos-arm64.txt`, `SHA256SUMS-macos-x64.txt`
6. Пока физический Mac не проверен, поставить **Set as a pre-release**. После проверки отредактировать релиз, снять флажок и поставить **Set as the latest release**.

В описании каждого релиза обязательно оставлять отдельные прямые ссылки: Windows installer, Windows portable, macOS M-чип, macOS Intel и инструкция для Mac. Шаблон уже находится в `RELEASE_NOTES.md`.

## Через GitHub CLI

```powershell
gh release create v1.1.0 `
  artifacts/installer/FoturTypingHelper-Setup-1.1.0-win-x64.exe `
  artifacts/FoturTypingHelper-1.1.0-win-x64-portable.zip `
  artifacts/FoturTypingHelper-1.1.0-macos-arm64.dmg `
  artifacts/FoturTypingHelper-1.1.0-macos-arm64.zip `
  artifacts/FoturTypingHelper-1.1.0-macos-x64.dmg `
  artifacts/FoturTypingHelper-1.1.0-macos-x64.zip `
  artifacts/SHA256SUMS.txt `
  artifacts/SHA256SUMS-macos-arm64.txt `
  artifacts/SHA256SUMS-macos-x64.txt `
  --repo Woonze/Fotur_Typing_Helper `
  --title "Fotur Typing Helper 1.1.0" `
  --notes-file RELEASE_NOTES.md `
  --prerelease
```

После физической проверки Mac:

```powershell
gh release edit v1.1.0 `
  --repo Woonze/Fotur_Typing_Helper `
  --prerelease=false `
  --latest
```

Автообновление читает только опубликованный стабильный endpoint `/releases/latest`. Draft и prerelease пользователям не предлагаются. Поэтому новый пакет следует сначала проверить как prerelease, а затем сделать стабильным и Latest.
