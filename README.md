# Fotur Typing Helper

Open-source помощник ввода для Windows 10/11, macOS 12+ и экспериментальной Linux x64-сборки. Версия 1.3.0 автоматически исправляет русско-английскую раскладку целыми фразами, запускает локальную диктовку по глобальной горячей клавише, лучше защищает технический английский текст и исправляет проверку обновлений через GitHub Releases. В этом релизе также добавлена Linux-сборка и приведены в порядок релизные пакеты: без чужих runtime, без пользовательских PDB и с лицензиями внутри дистрибутивов.

## Скачать 1.3.0

После публикации релиза файлы будут доступны по прямым ссылкам:

| Платформа | Файл |
|---|---|
| Windows x64 — установщик | [FoturTypingHelper-Setup-1.3.0-win-x64.exe](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/FoturTypingHelper-Setup-1.3.0-win-x64.exe) |
| Windows x64 — portable | [FoturTypingHelper-1.3.0-win-x64-portable.zip](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/FoturTypingHelper-1.3.0-win-x64-portable.zip) |
| macOS Apple Silicon — M1 и новее | [FoturTypingHelper-1.3.0-macos-arm64.dmg](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/FoturTypingHelper-1.3.0-macos-arm64.dmg) |
| macOS Intel | [FoturTypingHelper-1.3.0-macos-x64.dmg](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/FoturTypingHelper-1.3.0-macos-x64.dmg) |
| Linux x64 — experimental | [FoturTypingHelper-1.3.0-linux-x64.tar.gz](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.0/FoturTypingHelper-1.3.0-linux-x64.tar.gz) |

Все опубликованные версии находятся в [GitHub Releases](https://github.com/Woonze/Fotur_Typing_Helper/releases). Пользователям Mac перед запуском стоит открыть [пошаговую инструкцию по установке и разрешениям](docs/MACOS_INSTALL.md).

## Возможности

- Исправление EN↔RU с сохранением регистра, контекста до 24 слов и смешанных фрагментов вроде `Fotur помогает`.
- Локальная диктовка Whisper: русский, английский, автоматическое определение и отдельный перевод речи на английский.
- Модели Tiny, Base, Small и Medium; по умолчанию используется Small.
- Выбор микрофона, живой индикатор, VAD, шумовой порог, словарный prompt, голосовая пунктуация и фильтр слов-паразитов.
- Фирменная бирюзово-розовая рамка по краям всех экранов во время записи.
- На macOS рамка диктовки прозрачна для кликов, а права Input Monitoring/Accessibility проверяются при каждом запуске без закрытия приложения.
- Переназначаемые горячие клавиши, проверка конфликтов, отмена исправления и исключения приложений.
- Автозапуск, работа в трее и автоматическое обновление из GitHub Releases с проверкой SHA-256.
- Windows x64 installer/portable, отдельные macOS DMG/ZIP для Apple Silicon и Intel, экспериментальный Linux x64 tar.gz.
- Чистые релизные пакеты: только runtime целевой платформы, лицензия и notice зависимостей внутри архива, PDB вынесены в отдельные symbols-артефакты.

Распознавание выполняется локально. Интернет требуется для первой загрузки модели и проверки обновлений; текст и аудио не отправляются в облако. Временный WAV удаляется после распознавания.

## Linux

Linux-сборка 1.3.0 экспериментальная. UI, локальная Whisper-диктовка и установка модели упакованы в `FoturTypingHelper-1.3.0-linux-x64.tar.gz`. Запись использует `arecord` из `alsa-utils`, вставка текста — `xdotool` на X11. На Wayland синтетическая вставка и глобальные хоткеи ограничены системой, поэтому глобальная автокоррекция и глобальные хоткеи на Linux пока отключены и оставлены в backlog. Linux-артефакт нужен уже сейчас, чтобы заранее проверить упаковку, зависимости и будущий кроссплатформенный UI.

## macOS

На macOS нужны три независимых разрешения: «Микрофон», «Мониторинг ввода» и «Универсальный доступ». Первое позволяет записывать речь, второе — видеть глобальные горячие клавиши и набранный текст, третье — удалять и вставлять исправленный текст. Fotur проверяет эти права при каждом запуске, показывает их в настройках и продолжает ждать включения без закрытия приложения.

Сборки 1.3.0 имеют ad-hoc подпись. Без Apple Developer ID приложение пока нельзя нотарифицировать, поэтому Gatekeeper может потребовать подтверждение через «Системные настройки → Конфиденциальность и безопасность → Всё равно открыть». Подробности и решение типовых проблем есть в [инструкции для macOS](docs/MACOS_INSTALL.md).

## Документация

- [Каталог функций](docs/FUNCTIONS.md)
- [Backlog и матрица готовности](docs/BACKLOG.md)
- [Установка на macOS](docs/MACOS_INSTALL.md)
- [Архитектура](docs/ARCHITECTURE.md)
- [Сборка и тестирование](docs/BUILDING.md)
- [Политика безопасности](SECURITY.md)

Создатель: Кирилл, GitHub [Woonze](https://github.com/Woonze). Лицензия: [MIT](LICENSE).
