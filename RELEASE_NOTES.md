# Fotur Typing Helper 1.3.0

## Скачать

- **Windows x64 — установщик:** [скачать EXE](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/FoturTypingHelper-Setup-1.3.0-win-x64.exe)
- **Windows x64 — portable:** [скачать ZIP](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/FoturTypingHelper-1.3.0-win-x64-portable.zip)
- **macOS Apple Silicon — M1/M2/M3/M4/M5:** [скачать DMG](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/FoturTypingHelper-1.3.0-macos-arm64.dmg)
- **macOS Intel:** [скачать DMG](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/FoturTypingHelper-1.3.0-macos-x64.dmg)
- **Linux x64 — experimental:** [скачать TAR.GZ](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/FoturTypingHelper-1.3.0-linux-x64.tar.gz)
- **Инструкция для Mac:** [установка и выдача разрешений](https://github.com/Woonze/Fotur_Typing_Helper/blob/v1.3.0/docs/MACOS_INSTALL.md)

## Главное

- Исправлена проверка обновлений: больше нет общего “Не удалось проверить обновления” без причины при отсутствии release, сетевом сбое или неподходящем asset.
- Добавлена экспериментальная Linux x64-сборка: UI, локальная Whisper-диктовка, запись через `arecord`, вставка через `xdotool` на X11.
- Релизные пакеты очищаются от runtime-файлов других платформ и от `.pdb`; symbols публикуются отдельными artifact.
- Windows portable теперь распаковывается в корневую папку `FoturTypingHelper-1.3.0-win-x64-portable/`.
- LICENSE и THIRD_PARTY_NOTICES.md включены в Windows installer/portable, macOS bundle/DMG и Linux archive.
- Версия Inno Setup больше не хранится отдельно: build script передаёт её из `Directory.Build.props` через `/DAppVersion=...`.
- Восстановлены повреждённые UTF-8 строки в коде, runtime-статусах, updater, hotkey validation, macOS plist и тестах.
- Улучшена логика автокоррекции для технического английского: защищены `docker compose up -d`, `git push origin main`, `npm install` и похожие команды.

## Ограничения Linux

Linux 1.3.0 — experimental. Глобальная автокоррекция и глобальные хоткеи пока отключены. На Wayland синтетическая вставка ограничена системой; полноценная поддержка вынесена в backlog.
