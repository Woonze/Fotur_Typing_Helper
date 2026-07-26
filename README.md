# Fotur Typing Helper

Open-source помощник ввода для Windows 10/11, macOS 12+ и экспериментальной Linux x64-сборки. Версия 1.3.0 исправляет проверку обновлений, добавляет Linux artifact, восстанавливает повреждённые UTF-8 строки в коде/UI и усиливает защиту технического английского текста вроде `docker compose`, `git push` и `npm install`.

## Скачать 1.3.0

После публикации релиза файлы будут доступны по прямым ссылкам:

| Платформа | Файл |
|---|---|
| Windows x64 — установщик | [FoturTypingHelper-Setup-1.3.0-win-x64.exe](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/FoturTypingHelper-Setup-1.3.0-win-x64.exe) |
| Windows x64 — portable | [FoturTypingHelper-1.3.0-win-x64-portable.zip](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/FoturTypingHelper-1.3.0-win-x64-portable.zip) |
| macOS Apple Silicon — M1 и новее | [FoturTypingHelper-1.3.0-macos-arm64.dmg](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/FoturTypingHelper-1.3.0-macos-arm64.dmg) |
| macOS Intel | [FoturTypingHelper-1.3.0-macos-x64.dmg](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/FoturTypingHelper-1.3.0-macos-x64.dmg) |
| Linux x64 — experimental | [FoturTypingHelper-1.3.0-linux-x64.tar.gz](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/FoturTypingHelper-1.3.0-linux-x64.tar.gz) |

Все версии находятся в [GitHub Releases](https://github.com/Woonze/Fotur_Typing_Helper/releases). Пользователям Mac перед запуском стоит открыть [пошаговую инструкцию по установке и разрешениям](docs/MACOS_INSTALL.md).

## Возможности

- Автокоррекция EN↔RU с контекстом фразы, сохранением регистра и защитой известных корректных слов.
- Локальная диктовка Whisper: русский, английский, auto language detection и отдельный режим перевода речи на английский.
- Модели Tiny, Base, Small и Medium; по умолчанию используется Small.
- VAD, noise gate, prompt личного словаря, голосовая пунктуация и фильтр слов-паразитов.
- Фирменная подсветка краёв экрана во время записи.
- Переназначаемые горячие клавиши, проверка конфликтов и отмена последнего исправления.
- Автозапуск, трей и автообновление из GitHub Releases с проверкой SHA-256.
- Windows installer/portable, macOS DMG/ZIP для Apple Silicon и Intel, Linux x64 tar.gz.

Распознавание выполняется локально. Интернет требуется для первой загрузки модели и проверки обновлений; текст и аудио не отправляются в облако.

## Linux

Linux-сборка 1.3.0 экспериментальная. UI и локальная диктовка упакованы. Запись использует `arecord` (`alsa-utils`), вставка текста — `xdotool` на X11. На Wayland синтетическая вставка и глобальные хоткеи ограничены системой. Глобальная автокоррекция на Linux пока отключена и вынесена в backlog.

## macOS

На macOS нужны три разрешения: «Микрофон», «Мониторинг ввода» и «Универсальный доступ». Fotur проверяет их при запуске, показывает статус в настройках и ждёт включения без закрытия приложения.

Сборки 1.3.0 имеют ad-hoc подпись. Без Apple Developer ID приложение пока нельзя нотарифицировать, поэтому Gatekeeper может потребовать подтверждение через «Системные настройки → Конфиденциальность и безопасность → Всё равно открыть».

## Документация

- [Каталог функций](docs/FUNCTIONS.md)
- [Backlog и матрица готовности](docs/BACKLOG.md)
- [Установка на macOS](docs/MACOS_INSTALL.md)
- [Архитектура](docs/ARCHITECTURE.md)
- [Сборка и тестирование](docs/BUILDING.md)
- [Политика безопасности](SECURITY.md)

Создатель: Кирилл, GitHub [Woonze](https://github.com/Woonze). Лицензия: [MIT](LICENSE).
