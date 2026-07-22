# Публикация GitHub Release

## Подготовка

1. Обновить одинаковую версию в `Directory.Build.props` и `installer/FoturTypingHelper.iss`.
2. Запустить `./scripts/build-release.ps1` из PowerShell.
3. Проверить тесты, portable-приложение и установщик на чистой Windows 10/11.
4. Убедиться, что self-contained папка содержит `runtimes/win-x64/whisper.dll`; скрипт сборки проверяет это автоматически.
5. Запустить BrowserSmoke и DictationSmoke из `BUILDING.md`.
6. Подготовить заметки: что добавлено, исправлено, известные ограничения и способ отката.

## Публикация на сайте GitHub

1. Открыть страницу репозитория → **Releases** → **Draft a new release**.
2. Создать тег вида `v0.2.0` от проверенного коммита основной ветки.
3. Заголовок сделать понятным человеку, например `Fotur Typing Helper 0.2.0`.
4. Для ранней публичной версии включить **Set as a pre-release**.
5. Приложить Setup `.exe`, ZIP portable-сборки и файл SHA-256.
6. Перечитать заметки и нажать **Publish release**.

## GitHub CLI

После установки `gh` и команды `gh auth login` выпуск можно создать так:

```powershell
gh release create v0.2.0 `
  artifacts/installer/FoturTypingHelper-Setup-0.2.0-win-x64.exe `
  artifacts/FoturTypingHelper-0.2.0-win-x64-portable.zip `
  artifacts/SHA256SUMS.txt `
  --repo Woonze/Fotur_Typing_Helper `
  --title "Fotur Typing Helper 0.2.0" `
  --notes-file RELEASE_NOTES.md `
  --prerelease
```

Неподписанный установщик может вызвать Microsoft Defender SmartScreen. Убирать предупреждение обходными способами нельзя: для публичного стабильного выпуска нужен доверенный code-signing сертификат.
