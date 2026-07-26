# Fotur Typing Helper 1.3.0

Обновление инфраструктуры релиза и стабильности: исправлена проверка обновлений, добавлена экспериментальная Linux x64-сборка, а релизные пакеты очищены от чужих platform/runtime-файлов. Документация и шаблоны Issue сохранены подробными, но обновлены под текущую версию.

## Скачать

- **Windows x64 — установщик:** [скачать EXE](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/FoturTypingHelper-Setup-1.3.0-win-x64.exe)
- **Windows x64 — portable:** [скачать ZIP](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/FoturTypingHelper-1.3.0-win-x64-portable.zip)
- **macOS Apple Silicon — M1/M2/M3/M4/M5:** [скачать DMG](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/FoturTypingHelper-1.3.0-macos-arm64.dmg)
- **macOS Intel:** [скачать DMG](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/FoturTypingHelper-1.3.0-macos-x64.dmg)
- **Linux x64 — experimental:** [скачать TAR.GZ](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/FoturTypingHelper-1.3.0-linux-x64.tar.gz)
- **Инструкция для Mac:** [установка и выдача разрешений](https://github.com/Woonze/Fotur_Typing_Helper/blob/v1.3.0/docs/MACOS_INSTALL.md)
- **Контрольные суммы:** [Windows](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/SHA256SUMS.txt), [macOS Apple Silicon](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/SHA256SUMS-macos-arm64.txt), [macOS Intel](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/SHA256SUMS-macos-x64.txt), [Linux](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/SHA256SUMS-linux-x64.txt)

## Что изменилось

- Исправлена проверка обновлений через GitHub Releases. Fotur теперь выбирает asset под текущую платформу, различает Windows/macOS ARM/macOS Intel/Linux, проверяет SHA-256 и показывает понятный статус, если latest release или нужный файл недоступен.
- Добавлена экспериментальная Linux x64-сборка. В пакет входят Avalonia UI, локальная Whisper-диктовка, загрузка моделей и базовая вставка текста через `xdotool` на X11. Глобальная автокоррекция и глобальные хоткеи на Linux пока отключены из-за ограничений Wayland/X11 и вынесены в backlog.
- Релизные пакеты очищены от файлов других платформ. Windows portable больше не содержит Linux/macOS platform DLL и чужие native runtimes; Linux tar.gz не содержит Windows/macOS platform DLL; macOS build-процесс оставляет только нужный `macos-arm64` или `macos-x64` runtime плюс CoreML.
- Пользовательские ZIP/TAR/DMG больше не включают `.pdb`. Отладочные символы публикуются отдельными symbols-артефактами, чтобы основной пакет был меньше и аккуратнее.
- Windows portable распаковывается в общую папку `FoturTypingHelper-1.3.0-win-x64-portable/`, а не разбрасывает файлы в текущую директорию.
- Во все дистрибутивы добавлены `LICENSE` и `THIRD_PARTY_NOTICES.md`. Inno Setup показывает MIT-лицензию и устанавливает оба файла рядом с приложением.
- Версия установщика больше не дублируется вручную: build script читает `Directory.Build.props` и передаёт `/DAppVersion=...` в Inno Setup.
- Восстановлены повреждённые UTF-8 строки в UI, документах и Issue templates.
- Дополнительно усилена защита технического английского текста: `docker compose`, `git push`, `npm install`, `json config` и похожие команды не должны ложно переводиться в русскую раскладку.

## Важно для macOS

Перетащите приложение из DMG в «Программы» и запускайте именно установленную копию. Для автокоррекции и глобальных хоткеев нужны «Мониторинг ввода» и «Универсальный доступ», для диктовки — «Микрофон». Fotur 1.3.0 остаётся открытым и пере-проверяет права в фоне; если macOS попросит перезапуск после изменения Privacy-настроек, выполните это требование системы.

Текущие macOS-пакеты подписаны ad-hoc и пока не нотарифицированы Apple: для первого запуска может потребоваться «Всё равно открыть». Физическую проверку нового DMG на Intel и Apple Silicon продолжаем отдельно; если на конкретном Mac глобальный хоткей или автозамена не сработают, это будет исправляться в следующем обновлении.

## Важно для Linux

Linux x64 — experimental. Для записи нужен `alsa-utils`, для вставки текста на X11 нужен `xdotool`. На Wayland многие окружения блокируют synthetic input, поэтому Linux-версия сейчас нужна прежде всего для проверки UI, локальной диктовки, упаковки и будущей платформенной базы.
