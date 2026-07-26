namespace FoturTypingHelper.Core;

public enum TextLanguage { Unknown, Russian, English }

public sealed record CorrectionDecision(
    bool ShouldCorrect, string Original, string Replacement, TextLanguage Language, double Confidence);

public sealed class LanguageScorer
{
    private static readonly HashSet<string> CommonRussian = new(StringComparer.OrdinalIgnoreCase)
    {
        "и","в","не","на","я","что","это","как","по","но","мы","вы","он","она","они","для","из","у","к","с",
        "привет","спасибо","пожалуйста","да","нет","хорошо","можно","нужно","будет","есть","работа","текст","сегодня",
        "когда","если","уже","только","очень","всё","все","тоже","ещё","еще","программа","проект","сделать",
        "готово","интерфейс","браузер","редактор","сообщение","проверка","данные","модель","диктовка","микрофон",
        "автокоррекция","исправление","результат","обновление","установщик","словарь","ошибка","тишина","пунктуация",
        "доброе","давай","отправь","выбери","открой","александр","екатерина","кирилл","москва","релиз","версия",
        "скачать","сборка","ссылка","документ","функция","настройки","разрешения","компьютер","экран","окно",
        "нужно","надо","можешь","можем","сделай","проверь","исправь","добавь","удали","замени"
    };

    private static readonly HashSet<string> CommonEnglish = new(StringComparer.OrdinalIgnoreCase)
    {
        "a","i","the","and","or","not","to","of","in","is","it","that","this","for","you","we","he","she","they",
        "hello","thanks","thank","please","yes","no","good","can","need","will","work","text","today","when","if",
        "already","only","very","also","program","project","make","with","from","have","has","are","was",
        "fotur","helps","people","type","how","would","like","kirill","let","us","call","send","document","colleague","works","quickly",
        "result","looks","excellent","phrase","slightly","longer","do","switch","layout","london","welcomes","visitors",
        "there","check","runs","locally","model","recognizes","natural","speech","fast","typing","feels","comfortable",
        "slower","computer","manage","larger","smaller","starts","faster","silence","should","insert","punctuation",
        "sentence","clearer","remove","unnecessary","filler","words","hotkey","conflicts","available","github","interface",
        "professional","color","moves","smoothly","every","error","needs","clear","explanation","person","types","final","keyboard","ready",
        "docker","compose","kubernetes","cluster","container","containers","image","images","volume","volumes","service","services",
        "redis","postgres","mysql","nginx","node","react","dotnet","avalonia","whisper","chrome","macos","windows","linux","codex",
        "chatgpt","openai","api","json","yaml","yml","http","https","localhost","config","env","shell","terminal","powershell",
        "git","commit","branch","merge","pull","push","fetch","clone","checkout","rebase","request","release","build","test","tests","runner","workflow","workflows",
        "up","down","restart","logs","exec","install","update","upgrade","remove","run","start","stop"
    };

    private static readonly HashSet<string> TechnicalSafeTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "docker","compose","docker-compose","kubernetes","kubectl","k8s","redis","postgres","postgresql","mysql","nginx",
        "node","npm","pnpm","yarn","bun","npx","react","vue","svelte","dotnet","avalonia","whisper","openai","chatgpt","codex",
        "api","sdk","json","yaml","yml","xml","html","css","js","ts","tsx","jsx","http","https","localhost",
        "env","config","git","github","commit","branch","merge","pull","push","fetch","clone","checkout","rebase","request","pr","ci","cd","dmg","zip","exe","tar","gz",
        "up","down","restart","logs","exec","install","update","upgrade","remove","run","start","stop"
    };

    private static readonly string[] RussianPatterns =
        ["ст", "но", "то", "на", "ен", "ов", "ни", "ра", "ко", "пр", "ть", "ый", "ая", "ие", "что", "это",
         "ме", "ня", "те", "еб", "бя", "зо", "ву", "ут", "по", "ро", "го", "де", "ла", "ли", "ва", "ре",
         "ка", "та", "же", "сь", "не", "за", "чт", "бы", "мо", "до", "ес", "ет", "им", "ми", "ил", "ло"];
    private static readonly string[] EnglishPatterns =
        ["th", "he", "in", "er", "an", "re", "on", "at", "en", "nd", "ing", "ion", "ed", "ly", "that", "the",
         "ou", "it", "is", "or", "ti", "as", "te", "et", "ng", "of", "ha", "to", "hi", "me", "my", "yo"];
    private readonly HashSet<string> _custom;

    public LanguageScorer(IEnumerable<string>? customDictionary = null) =>
        _custom = new HashSet<string>(customDictionary ?? [], StringComparer.OrdinalIgnoreCase);

    public CorrectionDecision Evaluate(string word, double threshold = 0.72)
    {
        if (string.IsNullOrWhiteSpace(word) || word.Length < 2 || word.Any(char.IsDigit))
            return new(false, word, word, TextLanguage.Unknown, 0);
        if (AllTokensAreProtected(word))
            return new(false, word, word, TextLanguage.English, 0);

        var hasCyrillic = word.Any(IsCyrillic);
        var hasLatin = word.Any(c => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z');
        if (!hasCyrillic && !hasLatin) return new(false, word, word, TextLanguage.Unknown, 0);
        if (hasCyrillic && hasLatin)
        {
            var englishCandidate = LayoutConverter.ToEnglish(word);
            var russianCandidate = LayoutConverter.ToRussian(word);
            var mixedOriginalScore = ScoreDetectedTokens(word);
            var englishScore = Score(englishCandidate, TextLanguage.English);
            var russianScore = Score(russianCandidate, TextLanguage.Russian);
            var bestScore = Math.Max(englishScore, russianScore);
            var mixedConfidence = Sigmoid(bestScore - mixedOriginalScore);
            if (mixedConfidence < threshold) return new(false, word, word, TextLanguage.Unknown, mixedConfidence);
            return englishScore >= russianScore
                ? new(true, word, englishCandidate, TextLanguage.English, mixedConfidence)
                : new(true, word, russianCandidate, TextLanguage.Russian, mixedConfidence);
        }

        var candidate = hasLatin ? LayoutConverter.ToRussian(word) : LayoutConverter.ToEnglish(word);
        var originalLanguage = hasLatin ? TextLanguage.English : TextLanguage.Russian;
        var candidateLanguage = hasLatin ? TextLanguage.Russian : TextLanguage.English;
        var originalScore = Score(word, originalLanguage);
        var candidateScore = Score(candidate, candidateLanguage);
        var confidence = Sigmoid(candidateScore - originalScore);

        return confidence >= threshold
            ? new(true, word, candidate, candidateLanguage, confidence)
            : new(false, word, word, originalLanguage, confidence);
    }

    private double Score(string word, TextLanguage language)
    {
        var normalized = word.ToLowerInvariant();
        var common = language == TextLanguage.Russian ? CommonRussian : CommonEnglish;
        var patterns = language == TextLanguage.Russian ? RussianPatterns : EnglishPatterns;
        var tokens = normalized.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.Trim(',', '.', '!', '?', ':', ';', '"', '\'', '(', ')', '[', ']', '{', '}'))
            .Where(token => token.Length > 0)
            .ToArray();
        var score = tokens.Sum(token => common.Contains(token) ? 4.8 : 0);
        score += tokens.Sum(token => _custom.Contains(token) ? 5.5 : 0);
        score += tokens.Sum(token => patterns.Count(token.Contains) * 0.7);

        if (language == TextLanguage.Russian)
        {
            if (normalized.Any(c => "ъыь".Contains(c))) score += 0.15;
            if (normalized.Contains("ьы") || normalized.Contains("ъь")) score -= 2;
        }
        else
        {
            var vowels = normalized.Count(c => "aeiouy".Contains(c));
            if (vowels == 0 && normalized.Length > 3) score -= 1.4;
            if (normalized.Any(c => !char.IsLetter(c) && c is not '\'' and not '-')) score -= 1;
        }

        return score;
    }

    private double ScoreDetectedTokens(string phrase) => phrase
        .Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
        .Sum(token => token.Any(IsCyrillic)
            ? Score(token, TextLanguage.Russian)
            : Score(token, TextLanguage.English));

    private static bool AllTokensAreProtected(string phrase)
    {
        var tokens = phrase.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.Trim(',', '.', '!', '?', ':', ';', '"', '\'', '(', ')', '[', ']', '{', '}'))
            .Where(token => token.Length > 0)
            .ToArray();
        return tokens.Length > 0 && tokens.All(token =>
            TechnicalSafeTokens.Contains(token) ||
            token.Contains('.') ||
            token.Contains('/') ||
            token.Contains('\\') ||
            token.Contains('_') ||
            token.Contains('-') && token.Any(char.IsLetter));
    }

    private static bool IsCyrillic(char c) => c is >= 'А' and <= 'я' or 'Ё' or 'ё';
    private static double Sigmoid(double value) => 1d / (1d + Math.Exp(-value));
}
