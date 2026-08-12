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

    // These are intentionally protected even when they happen to resemble an ordinary
    // English word after a layout conversion. A false positive in source code is much
    // more expensive than leaving one unusual natural-language word unchanged.
    private static readonly HashSet<string> ProgrammingKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "abstract","as","async","await","base","break","case","catch","checked","class","const","continue","default","delegate","do","else","enum","event","explicit","extern","finally","fixed","for","foreach","goto","if","implicit","in","interface","internal","is","lock","namespace","new","operator","out","override","params","private","protected","public","readonly","ref","return","sealed","sizeof","stackalloc","static","struct","switch","this","throw","try","typeof","unchecked","unsafe","using","virtual","void","volatile","while","yield",
        "boolean","byte","char","double","float","int","long","number","object","short","string","var","decimal","dynamic","null","true","false","undefined","function","let","import","export","from","extends","implements","package","module","require","default","finally","instanceof","keyof","never","unknown","readonly","type","declare","satisfies",
        "select","insert","update","delete","create","alter","drop","table","database","index","join","left","right","inner","outer","where","group","order","having","limit","values","into","primary","foreign","key","begin","commit","rollback"
    };

    private static readonly HashSet<string> CommandExecutables = new(StringComparer.OrdinalIgnoreCase)
    {
        "git","gh","docker","kubectl","helm","terraform","tofu","ansible","npm","pnpm","yarn","bun","npx","node","deno","dotnet","msbuild","nuget","powershell","pwsh","bash","zsh","sh","cmd","make","cmake","gradle","mvn","java","python","python3","pip","pip3","poetry","uv","cargo","rustc","go","code","adb","ssh","scp","curl","wget","rsync","grep","rg","sed","awk","jq","yq","chmod","chown","systemctl","journalctl","ps","kill"
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
        var originalScore = ScoreDetectedTokens(word);
        var candidates = new List<string> { BuildSelectiveCandidate(word) };

        // A whole-phrase conversion remains useful for natural prose, but it is unsafe if
        // the phrase includes a command, identifier or other deliberately-English token.
        if (!HasProtectedToken(word))
        {
            candidates.Add(hasCyrillic ? LayoutConverter.ToEnglish(word) : LayoutConverter.ToRussian(word));
            if (hasCyrillic && hasLatin)
            {
                candidates.Add(LayoutConverter.ToEnglish(word));
                candidates.Add(LayoutConverter.ToRussian(word));
            }
        }

        var candidate = candidates
            .Where(value => !string.Equals(value, word, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Select(value => new { Value = value, Score = ScoreDetectedTokens(value) })
            .OrderByDescending(value => value.Score)
            .FirstOrDefault();
        if (candidate is null)
            return new(false, word, word, DetectLastLanguage(word), 0);

        var confidence = Sigmoid(candidate.Score - originalScore);
        var language = DetectLastLanguage(candidate.Value);
        return confidence >= threshold
            ? new(true, word, candidate.Value, language, confidence)
            : new(false, word, word, DetectLastLanguage(word), confidence);
    }

    private string BuildSelectiveCandidate(string phrase)
    {
        var result = new System.Text.StringBuilder(phrase.Length);
        var start = 0;
        while (start < phrase.Length)
        {
            var whitespace = char.IsWhiteSpace(phrase[start]);
            var end = start + 1;
            while (end < phrase.Length && char.IsWhiteSpace(phrase[end]) == whitespace) end++;
            var part = phrase[start..end];
            result.Append(whitespace ? part : ConvertTokenWhenProven(part));
            start = end;
        }
        return result.ToString();
    }

    private string ConvertTokenWhenProven(string token)
    {
        var core = token.Trim(',', '.', '!', '?', ':', ';', '"', '\'', '(', ')', '[', ']', '{', '}');
        if (core.Length < 2 || IsProtectedToken(token) || IsProtectedToken(core)) return token;
        var hasCyrillic = core.Any(IsCyrillic);
        var hasLatin = core.Any(c => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z');
        if (hasCyrillic == hasLatin) return token;

        var nativeLanguage = hasLatin ? TextLanguage.English : TextLanguage.Russian;
        var convertedLanguage = hasLatin ? TextLanguage.Russian : TextLanguage.English;
        var convertedCore = hasLatin ? LayoutConverter.ToRussian(core) : LayoutConverter.ToEnglish(core);
        // One accidental-looking pattern is not sufficient evidence. This margin protects
        // unfamiliar class names, domain terms and short code identifiers.
        if (Score(convertedCore, convertedLanguage) < Score(core, nativeLanguage) + 1.35) return token;
        return token.Replace(core, convertedCore, StringComparison.Ordinal);
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

    private static TextLanguage DetectLastLanguage(string phrase)
    {
        for (var i = phrase.Length - 1; i >= 0; i--)
        {
            if (IsCyrillic(phrase[i])) return TextLanguage.Russian;
            if (phrase[i] is >= 'A' and <= 'Z' or >= 'a' and <= 'z') return TextLanguage.English;
        }
        return TextLanguage.Unknown;
    }

    private static bool AllTokensAreProtected(string phrase)
    {
        var tokens = phrase.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.Trim(',', '.', '!', '?', ':', ';', '"', '\'', '(', ')', '[', ']', '{', '}'))
            .Where(token => token.Length > 0)
            .ToArray();
        if (IsCommandPhrase(tokens)) return true;
        return tokens.Length > 0 && tokens.All(IsProtectedToken);
    }

    private static bool HasProtectedToken(string phrase) => phrase
        .Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
        .Select(token => token.Trim(',', '.', '!', '?', ':', ';', '"', '\'', '(', ')', '[', ']', '{', '}'))
        .Any(IsProtectedToken);

    private static bool IsProtectedToken(string token)
    {
        if (TechnicalSafeTokens.Contains(token) || ProgrammingKeywords.Contains(token) || CommandExecutables.Contains(token) ||
            token.StartsWith("--", StringComparison.Ordinal)) return true;
        if (token.Contains('.') || token.Contains('/') || token.Contains('\\') || token.Contains('_') || token.Contains('@') ||
            token.Contains('$') || token.Contains('#') || token.Contains('=') || token.Contains(':')) return true;
        if (LooksLikeCodeSyntax(token)) return true;
        if (token.Contains('-') && token.Any(char.IsLetter)) return true;
        // camelCase/PascalCase identifiers are overwhelmingly code or product names.
        return token.Length >= 3 && token.Any(char.IsLower) && token.Any(char.IsUpper);
    }

    private static bool LooksLikeCodeSyntax(string token) =>
        token.Contains("=>", StringComparison.Ordinal) || token.Contains("::", StringComparison.Ordinal) ||
        token.Contains("?.", StringComparison.Ordinal) || token.Contains("??", StringComparison.Ordinal) ||
        token.Contains("==", StringComparison.Ordinal) || token.Contains("!=", StringComparison.Ordinal) ||
        token.Contains("<=", StringComparison.Ordinal) || token.Contains(">=", StringComparison.Ordinal) ||
        token.Contains("&&", StringComparison.Ordinal) || token.Contains("||", StringComparison.Ordinal) ||
        token.Contains("+=", StringComparison.Ordinal) || token.Contains("-=", StringComparison.Ordinal) ||
        token.Contains("*=", StringComparison.Ordinal) || token.Contains("/=", StringComparison.Ordinal) ||
        token.Contains("()", StringComparison.Ordinal) || token.Contains('{') || token.Contains('}') ||
        (token.Length > 2 && token[0] == '<' && token[^1] == '>');

    private static bool IsCommandPhrase(IReadOnlyList<string> tokens) =>
        tokens.Count > 0 && CommandExecutables.Contains(tokens[0]) && tokens.Count > 1;

    private static bool IsCyrillic(char c) => c is >= 'А' and <= 'я' or 'Ё' or 'ё';
    private static double Sigmoid(double value) => 1d / (1d + Math.Exp(-value));
}
