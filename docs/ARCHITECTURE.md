# Архитектура 1.0.0

```text
FoturTypingHelper.App       Avalonia UI, tray, screen-edge overlay, runtime, updater
FoturTypingHelper.Core      настройки, EN↔RU, scoring, hotkeys, audio/postprocessing, platform contracts
FoturTypingHelper.Windows   Win32 hook/SendInput/UIA, NAudio, Whisper CPU, registry autostart
FoturTypingHelper.Mac       CGEvent/TIS, OpenAL, Whisper CoreML/CPU, LaunchAgent
FoturTypingHelper.Tests     unit tests переносимой логики
BrowserSmoke               настоящий Chrome + SendInput + 150 фраз
DictationSmoke             ручной полный цикл с микрофоном и локальной моделью
```

`Core` не зависит от API ОС. `IKeyboardService`, `IAudioRecorder`, `IDictationService`, `ITextInjectionService`, `IActiveWindowService` и `IAutostartService` выбираются composition root по текущей платформе. Это сохраняет один интерфейс и позволяет развивать Windows/macOS независимо.

## Автокоррекция

Hook формирует слово и контекст до 24 слов. `LanguageScorer` токенизирует фрагмент, сравнивает частоты слов/сочетаний и пользовательский словарь для исходного и физически конвертированного вариантов. Для смешанного фрагмента отдельно строятся RU- и EN-кандидаты, поэтому задержка системного переключения после первого слова не портит продолжение. Инъекция помечается marker, чтобы hook не анализировал собственную замену.

Windows использует `WH_KEYBOARD_LL`, `ToUnicodeEx`, `SendInput` и `WM_INPUTLANGCHANGEREQUEST`. macOS использует CGEvent tap/post и TIS input source. Обе реализации поддерживают настраиваемую диктовку и отмену. Результат синтетической вставки возвращается в runtime: статистика обновляется только после подтверждённой отправки, а macOS отдельно диагностирует разрешения ListenEvent/Input Monitoring и PostEvent/Accessibility.

## Диктовка

`AppRuntime` запоминает активную цель, запускает platform recorder и показывает четыре overlay-окна на каждый монитор. После остановки WAV проходит VAD/noise gate, затем Whisper получает язык/auto detection, режим translate и prompt личного словаря. Финальный текст проходит команды и постобработку, возвращается в активную цель и временный WAV удаляется.

Windows включает CPU runtime. macOS предпочитает CoreML и откатывается на CPU. Модель хранится в локальном каталоге пользователя и загружается только при первом использовании.

## Обновление

На старте фоновая задача читает `/releases/latest`, сравнивает SemVer и выбирает `win-x64.exe`, `macos-arm64.zip` либо `macos-x64.zip`. Пакет принимается только при совпадении SHA-256. Windows передаёт установку Inno Setup; macOS helper заменяет установленный `.app` после выхода текущего процесса.

Updater не считает prerelease последней стабильной версией. SHA-файл и пакет размещены в одном GitHub Release, поэтому для защиты от компрометации аккаунта в будущем нужен отдельно подписанный манифест.
