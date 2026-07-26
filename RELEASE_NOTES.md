# Fotur Typing Helper 1.2.0

Точечное обновление стабильности: macOS-диктовка больше не перекрывает клики, разрешения проверяются при каждом запуске, а отмена автозамены и удалённый текст обрабатываются корректнее.

## Скачать

- **Windows x64 — установщик:** [скачать EXE](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.2.0/FoturTypingHelper-Setup-1.2.0-win-x64.exe)
- **Windows x64 — portable:** [скачать ZIP](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.2.0/FoturTypingHelper-1.2.0-win-x64-portable.zip)
- **macOS Apple Silicon — M1/M2/M3/M4/M5:** [скачать DMG](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.2.0/FoturTypingHelper-1.2.0-macos-arm64.dmg)
- **macOS Intel:** [скачать DMG](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.2.0/FoturTypingHelper-1.2.0-macos-x64.dmg)
- **Инструкция для Mac:** [установка и выдача разрешений](https://github.com/Woonze/Fotur_Typing_Helper/blob/v1.2.0/docs/MACOS_INSTALL.md)
- **Контрольные суммы:** [Windows](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.2.0/SHA256SUMS.txt), [macOS Apple Silicon](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.2.0/SHA256SUMS-macos-arm64.txt), [macOS Intel](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.2.0/SHA256SUMS-macos-x64.txt)

## Что изменилось

- macOS-обводка во время диктовки стала прозрачной для кликов: свечение остаётся видимым, но не блокирует окна, кнопки и поля под ним.
- При запуске macOS версия проверяет `Input Monitoring` и `Accessibility`, показывает понятный статус и продолжает ждать выдачи прав без закрытия приложения.
- В настройках macOS появился отдельный блок разрешений с кнопками открытия нужных разделов: «Мониторинг ввода», «Универсальный доступ» и «Микрофон».
- Исправлена отмена через `Ctrl+Alt+Backspace`: Fotur временно отпускает зажатые модификаторы перед synthetic backspace, поэтому исправленный текст удаляется, а не остаётся рядом с восстановленным.
- Исправлен stale-контекст после удаления: если пользователь удалил слово Backspace и набрал заново, старый удалённый фрагмент больше не участвует в следующей автозамене.
- Улучшена защита технических английских фраз и терминов. `docker compose`, `git pull request`, `json config` и похожие рабочие команды не должны превращаться в русскую раскладку.
- Версия, installer, app manifest, README, FUNCTIONS, BACKLOG, BUILDING и macOS guide синхронизированы на 1.2.0.

## Важно для macOS

Перетащите приложение из DMG в «Программы» и запускайте именно установленную копию. Для автокоррекции и глобальных хоткеев нужны «Мониторинг ввода» и «Универсальный доступ», для диктовки — «Микрофон». Fotur 1.2.0 остаётся открытым и пере-проверяет права в фоне; если macOS сама попросит перезапуск после изменения Privacy-настроек, выполните это требование системы.

Текущие macOS-пакеты подписаны ad-hoc и пока не нотарифицированы Apple: для первого запуска может потребоваться «Всё равно открыть». Физическую проверку нового DMG на Intel и Apple Silicon продолжаем отдельно; если на конкретном Mac глобальный хоткей или автозамена не сработают, это будет исправляться в следующем обновлении.
