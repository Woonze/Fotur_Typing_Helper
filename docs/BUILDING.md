# Сборка и выпуск

Требования: Windows 10 22H2/11 x64, .NET 8 SDK и Inno Setup 6 для Setup-сборки.

```powershell
dotnet restore
dotnet test
./scripts/build-release.ps1
```

## Расширенные smoke-тесты Windows

После portable-публикации можно проверить реальный глобальный ввод в отдельном локальном окне Chrome:

```powershell
dotnet run --project tests/FoturTypingHelper.BrowserSmoke -c Release -- artifacts/publish/FoturTypingHelper.App.exe
```

Тест вводит пять EN→RU фраз с задержкой 42 мс между клавишами и проверяет значение браузерного поля, включая замену предыдущих слов, заглавные буквы и OEM-клавиши.

Полный локальный цикл диктовки с микрофоном и установленной моделью проверяется так:

```powershell
dotnet run --project tests/FoturTypingHelper.DictationSmoke -c Release
```

Результаты: `artifacts/publish/FoturTypingHelper.App.exe` и
`artifacts/installer/FoturTypingHelper-Setup-0.3.0-win-x64.exe`.

Установщик поддерживает русский и английский интерфейс, выбор каталога,
ярлыки и опцию автозапуска. До приобретения сертификата Windows SmartScreen может
показывать «Неизвестный издатель» — это не устраняется настройкой установщика.
