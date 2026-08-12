# Fotur Typing Helper 1.3.1

Патч качества для автокоррекции и локальной диктовки. Fotur осторожнее работает рядом с кодом и командами, одинаково обрабатывает ручное редактирование на Windows/macOS/Linux и надёжнее восстанавливается после сорванной загрузки модели Whisper.

## Скачать

- **Windows x64 — установщик:** [скачать EXE](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.1/FoturTypingHelper-Setup-1.3.1-win-x64.exe)
- **Windows x64 — portable:** [скачать ZIP](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.1/FoturTypingHelper-1.3.1-win-x64-portable.zip)
- **macOS Apple Silicon — M1/M2/M3/M4/M5:** [скачать DMG](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.1/FoturTypingHelper-1.3.1-macos-arm64.dmg)
- **macOS Intel:** [скачать DMG](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.1/FoturTypingHelper-1.3.1-macos-x64.dmg)
- **Linux x64 — X11:** [скачать TAR.GZ](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.1/FoturTypingHelper-1.3.1-linux-x64.tar.gz)
- **Инструкция для Mac:** [установка и выдача разрешений](https://github.com/Woonze/Fotur_Typing_Helper/blob/v1.3.1/docs/MACOS_INSTALL.md)
- **Контрольные суммы:** [Windows](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.1/SHA256SUMS.txt), [macOS Apple Silicon](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.1/SHA256SUMS-macos-arm64.txt), [macOS Intel](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.1/SHA256SUMS-macos-x64.txt), [Linux](https://github.com/Woonze/Fotur_Typing_Helper/releases/download/v1.3.1/SHA256SUMS-linux-x64.txt)

## Что изменилось

- Автокоррекция защищает команды, флаги, пути, URL, переменные окружения, ключевые слова C#/JS/SQL, CLI-инструменты, операторы и идентификаторы (`camelCase`/`PascalCase`). Код вроде `value => value.Trim()`, `kubectl get pods -n default` и `DATABASE_URL=...` не должен менять раскладку.
- Алгоритм стал асимметрично-консервативным: при сомнении он оставляет редкое слово нетронутым, а не рискует испортить корректный текст.
- Windows, macOS и Linux сбрасывают сохранённый контекст после Backspace/Delete/Escape и навигации курсором. Linux не обрабатывает собственную замену `xdotool` как новый пользовательский ввод.
- macOS обрабатывает горячую клавишу так же строго, как Windows и Linux: лишний модификатор не запускает диктовку.
- Загрузка Whisper-моделей стала атомарной: временный файл не считается готовой моделью, удаляется после ошибки, а повреждённая модель автоматически скачивается заново.
- Добавлены регрессионные тесты: 90 unit-тестов покрывают обычную речь, неправильную раскладку, смешанные фразы, код, команды и опасные синтаксические конструкции.

## Важно для macOS

Перетащите приложение из DMG в «Программы» и запускайте именно установленную копию. Для автокоррекции и глобальных хоткеев нужны «Мониторинг ввода» и «Универсальный доступ», для диктовки — «Микрофон». Fotur 1.3.1 остаётся открытым и пере-проверяет права в фоне; если macOS попросит перезапуск после изменения Privacy-настроек, выполните это требование системы.

Текущие macOS-пакеты подписаны ad-hoc и пока не нотарифицированы Apple: для первого запуска может потребоваться «Всё равно открыть». Физическую проверку нового DMG на Intel и Apple Silicon продолжаем отдельно; если на конкретном Mac глобальный хоткей или автозамена не сработают, это будет исправляться в следующем обновлении.

## Важно для Linux

Linux x64 — X11. Для записи нужен `alsa-utils`, для вставки текста на X11 нужен `xdotool`. На Wayland многие окружения блокируют synthetic input, поэтому Linux-версия сейчас нужна прежде всего для проверки UI, локальной диктовки, упаковки и будущей платформенной базы.
