# Fotur Typing Helper 1.1.0

Обновление macOS-интеграции, установки и автообновления. Windows-версия остаётся основной проверенной сборкой; macOS-пакеты приложены для тестирования на Apple Silicon и Intel.

## Скачать

- **Windows x64 — установщик:** [скачать EXE](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.1.0/FoturTypingHelper-Setup-1.1.0-win-x64.exe)
- **Windows x64 — portable:** [скачать ZIP](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.1.0/FoturTypingHelper-1.1.0-win-x64-portable.zip)
- **macOS Apple Silicon — M1/M2/M3/M4/M5:** [скачать DMG](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.1.0/FoturTypingHelper-1.1.0-macos-arm64.dmg)
- **macOS Intel:** [скачать DMG](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.1.0/FoturTypingHelper-1.1.0-macos-x64.dmg)
- **Инструкция для Mac:** [установка и выдача разрешений](https://github.com/Woonze/Fotur_Typing_Helper/blob/v1.1.0/docs/MACOS_INSTALL.md)
- **Контрольные суммы:** [Windows](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.1.0/SHA256SUMS.txt), [macOS Apple Silicon](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.1.0/SHA256SUMS-macos-arm64.txt), [macOS Intel](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.1.0/SHA256SUMS-macos-x64.txt)

## Что изменилось

- Исправлено удержание буквенной горячей клавиши на macOS: повторные `KeyDown` теперь подавляются и больше не печатают букву в активное поле.
- Подготовлен профессиональный DMG: фирменный фон, окно установки и ярлык «Программы» для привычного drag-and-drop.
- Добавлена единая фирменная иконка приложения для Windows, macOS, окна и системного трея.
- Название macOS-приложения закреплено как `Fotur Typing Helper`, bundle id — `tech.fotur.typinghelper`.
- Обновлён проверяющий GitHub Releases модуль: отдельный пакет для Windows, Apple Silicon и Intel, сверка SHA-256 и понятный статус «Установлена актуальная версия».
- Добавлена подробная инструкция для macOS с первым запуском, тремя разрешениями и восстановлением работы после обновления.

## Важно для macOS

Перетащите приложение из DMG в «Программы» и запускайте именно установленную копию. Для автокоррекции и глобальных хоткеев нужны «Мониторинг ввода» и «Универсальный доступ», для диктовки — «Микрофон». После выдачи прав полностью завершите Fotur и запустите снова.

Текущие macOS-пакеты подписаны ad-hoc и пока не нотарифицированы Apple: для первого запуска может потребоваться «Всё равно открыть». Физическую проверку нового DMG на Intel и Apple Silicon продолжаем отдельно; если на конкретном Mac глобальный хоткей или автозамена не сработают, это будет исправляться в следующем обновлении.
