using FoturTypingHelper.Core;

internal enum TargetLanguage { Russian, English }
internal sealed record SmokePhrase(string Keys, string Expected, TargetLanguage Target);

internal static class SmokePhraseCatalog
{
    private static readonly string[] Russian =
    [
        "привет", "как дела", "доброе утро", "спасибо за помощь", "я бы хотел", "Кирилл сегодня работает",
        "давай созвонимся вечером", "проект почти готов", "проверь последнее сообщение", "отправь документ коллеге",
        "мы начинаем новую задачу", "эта программа работает быстро", "исправление должно быть точным",
        "сегодня хорошая погода", "завтра будет важная встреча", "пожалуйста сохрани изменения",
        "нужно обновить приложение", "открой настройки диктовки", "выбери русский язык", "микрофон подключён правильно",
        "текст появился в браузере", "я закончил проверку", "результат выглядит отлично", "это короткая фраза",
        "это уже немного более длинная фраза", "контекст помогает исправлять предыдущие слова",
        "не переключай раскладку вручную", "автокоррекция сделает всё сама", "мы тестируем заглавные буквы",
        "Москва встречает гостей", "Александр написал ответ", "Екатерина проверила отчёт", "Fotur помогает печатать",
        "браузер получил правильный текст", "редактор сохранил новый файл", "сообщение отправлять не нужно",
        "проверка идёт локально", "данные не уходят в облако", "модель распознаёт живую речь",
        "англицизмы иногда требуют контекста", "сленг постоянно меняется", "давай попробуем ещё раз",
        "всё получилось с первого раза", "быстро печатать очень удобно", "медленный компьютер тоже справится",
        "большая модель работает точнее", "маленькая модель запускается быстрее", "выбери подходящий микрофон",
        "индикатор показывает уровень звука", "тишина не должна вставлять текст", "шум нужно аккуратно отфильтровать",
        "длинная запись делится на части", "пунктуация делает текст понятнее", "убери лишние слова паразиты",
        "личный словарь знает редкие имена", "горячую клавишу можно изменить", "конфликт сочетаний уже проверен",
        "окно уходит в системный трей", "обновление загрузится при запуске", "новая версия доступна на сайте",
        "установщик проверяет контрольную сумму", "старые настройки будут сохранены", "интерфейс выглядит профессионально",
        "фирменная рамка показывает запись", "рамка исчезает после обработки", "цвет плавно меняется по краям",
        "пользователь всегда видит состояние", "ошибка должна быть понятной", "повторная попытка остаётся доступной",
        "мы проверяем очень длинную фразу чтобы убедиться что несколько предыдущих слов заменяются вместе без задержки и потери заглавных букв",
        "когда человек печатает с обычной скоростью приложение должно незаметно анализировать контекст и исправлять неверную раскладку",
        "после завершения диктовки активное поле снова получает фокус а распознанный текст появляется в правильном месте",
        "эта финальная русская фраза проверяет стабильность последовательной обработки большого набора тестовых примеров",
        "последняя проверка раскладки завершена успешно",
        "готово"
    ];

    private static readonly string[] English =
    [
        "hello", "how are you", "good morning", "thanks for your help", "I would like", "Kirill is working today",
        "let us call tonight", "the project is almost ready", "check the latest message", "send the document to a colleague",
        "we are starting a new task", "this program works quickly", "the correction must be accurate",
        "the weather is good today", "tomorrow we have an important meeting", "please save the changes",
        "the application needs an update", "open the dictation settings", "select the English language",
        "the microphone is connected correctly", "the text appeared in the browser", "I finished the review",
        "the result looks excellent", "this is a short phrase", "this phrase is slightly longer than the previous one",
        "context helps to fix previous words", "do not switch the layout manually", "autocorrection will handle everything",
        "we are testing capital letters", "London welcomes new visitors", "Alexander wrote a response",
        "Catherine reviewed the report", "Fotur helps people type", "the browser received the correct text",
        "the editor saved a new file", "there is no need to send this message", "the check runs locally",
        "data never leaves this computer", "the model recognizes natural speech", "technical terms sometimes need context",
        "modern slang changes constantly", "let us try one more time", "everything worked on the first attempt",
        "fast typing feels very comfortable", "a slower computer can also manage", "the larger model is more accurate",
        "the smaller model starts faster", "choose the correct microphone", "the meter shows the audio level",
        "silence should not insert any text", "background noise should be filtered carefully",
        "a long recording is split into segments", "punctuation makes every sentence clearer",
        "remove unnecessary filler words", "the personal dictionary knows rare names", "the hotkey can be changed",
        "hotkey conflicts are checked in advance", "the window stays in the system tray",
        "the update downloads during startup", "a new version is available on GitHub",
        "the installer verifies the checksum", "existing settings remain available", "the interface looks professional",
        "the branded border indicates recording", "the border disappears after processing", "the color moves smoothly around the screen",
        "the user can always see the current state", "every error needs a clear explanation",
        "another attempt remains available", "we test a very long English phrase to ensure that many previous words are replaced together without delay or lost capital letters",
        "when a person types at a normal speed the application should analyze context quietly and correct the wrong keyboard layout",
        "after dictation the original field receives focus again and the recognized text appears exactly where the user expects it",
        "this final English phrase verifies stable sequential processing across a large collection of realistic test examples",
        "the final keyboard layout check completed successfully",
        "ready"
    ];

    public static IReadOnlyList<SmokePhrase> All => Russian
        .Select(text => new SmokePhrase(LayoutConverter.ToEnglish(text), text, TargetLanguage.Russian))
        .Concat(English.Select(text => new SmokePhrase(LayoutConverter.ToRussian(text), text, TargetLanguage.English)))
        .ToArray();
}
