# Backlog и матрица готовности

Статусы: `[x]` реализовано и автоматически проверено, `[~]` реализовано, но нужна ручная проверка на целевых устройствах, `[ ]` запланировано.

## Версия 1.3.0

- [x] Версия продукта, installer, manifest, README, FUNCTIONS, BACKLOG, BUILDING, ARCHITECTURE и RELEASE_NOTES синхронизированы на 1.3.0.
- [x] Исправлены повреждённые UTF-8 строки в updater, tray, runtime-статусах, hotkey validation, scorer tests и macOS plist.
- [x] Updater больше не падает на `404 releases/latest` и сетевой ошибке, а показывает понятный статус.
- [x] Updater выбирает Linux x64 asset `FoturTypingHelper-1.3.0-linux-x64.tar.gz` и checksum `SHA256SUMS-linux-x64.txt`.
- [x] Добавлен Linux x64 проект, publish script и GitHub Actions workflow.
- [x] Windows/macOS packages очищаются от чужих native runtimes.
- [x] Русский словарь автокоррекции восстановлен после mojibake.
- [x] Защищены дополнительные технические команды: `docker compose up -d`, `git push origin main`, `npm install`.
- [~] Linux UI и локальная диктовка доступны как experimental tar.gz; требуется ручной тест на реальном Linux/X11.

## Ограничения Linux

- [ ] Глобальная автокоррекция Linux через evdev/libinput с безопасным permission flow.
- [ ] Глобальные хоткеи Linux для X11 и Wayland.
- [ ] Wayland-вставка через desktop portal/IME-safe механизм.
- [ ] `.deb`, `.rpm` или AppImage вместо tar.gz.
- [ ] Autostart через `.desktop` файл после выбора стабильной схемы установки.

## Диктовка и модели

- [x] Tiny/Base/Small/Medium, размеры/RAM и выбор в UI.
- [x] Prompt личного словаря без сохранения диктовки.
- [x] Обычная транскрипция и локальный перевод на английский разделены.
- [ ] Стабильный потоковый предварительный текст во время записи.
- [ ] Перевод на целевые языки, кроме английского.

## Матрица приложений

| Приложение | Автокоррекция | Диктовка | Статус |
|---|---:|---:|---|
| Chrome input | [x] | [~] | 150-фразовый Windows smoke |
| Notepad | [ ] | [ ] | ручной release-check |
| Edge/password/contenteditable | [ ] | [ ] | расширить автоматизацию |
| Telegram Desktop | [ ] | [ ] | emoji/editing |
| Discord | [ ] | [ ] | Electron/contenteditable |
| Microsoft Word | [ ] | [ ] | форматированный текст и Undo |
| VS Code | [ ] | [ ] | Monaco editor |
| Windows Terminal/PowerShell | [ ] | [ ] | безопасный режим для команд |
| ChatGPT/Codex desktop | [ ] | [ ] | Electron и нативные поля |
| macOS TextEdit/Safari/Chrome | [~] | [~] | требует физический Mac |
| Linux X11 apps | [ ] | [~] | диктовка через xdotool, автокоррекция в backlog |
