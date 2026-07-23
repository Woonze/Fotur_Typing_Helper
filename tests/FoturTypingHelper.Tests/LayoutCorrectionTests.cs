using FoturTypingHelper.Core;

namespace FoturTypingHelper.Tests;

public sealed class LayoutCorrectionTests
{
    [Theory]
    [InlineData("ghbdtn", "привет")]
    [InlineData("руддщ", "hello")]
    [InlineData("Rfr ltkf?", "Как дела?")]
    [InlineData("Z ,s [jntk", "Я бы хотел")]
    [InlineData("Rbhbkk", "Кирилл")]
    public void LayoutConverter_MapsPhysicalKeys(string source, string expected)
    {
        var result = source.Any(c => c is >= 'А' and <= 'я')
            ? LayoutConverter.ToEnglish(source)
            : LayoutConverter.ToRussian(source);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Scorer_CorrectsObviousWrongLayout()
    {
        var decision = new LanguageScorer().Evaluate("ghbdtn");
        Assert.True(decision.ShouldCorrect);
        Assert.Equal("привет", decision.Replacement);
        Assert.Equal(TextLanguage.Russian, decision.Language);
    }

    [Fact]
    public void Scorer_KeepsValidEnglishWord()
    {
        var decision = new LanguageScorer().Evaluate("hello");
        Assert.False(decision.ShouldCorrect);
    }

    [Theory]
    [InlineData("рщц фку нщг", "how are you")]
    [InlineData("еру ьщвуд кусщптшяуы тфегкфд ызууср", "the model recognizes natural speech")]
    public void Scorer_UsesEveryWordInEnglishPhrase(string source, string expected)
    {
        var decision = new LanguageScorer().Evaluate(source, 0.60);
        Assert.True(decision.ShouldCorrect);
        Assert.Equal(expected, decision.Replacement);
        Assert.Equal(TextLanguage.English, decision.Language);
    }

    [Theory]
    [InlineData("Fotur")]
    [InlineData("interface")]
    [InlineData("ready")]
    public void Scorer_KeepsKnownEnglishAndBrandWords(string source)
    {
        Assert.False(new LanguageScorer().Evaluate(source).ShouldCorrect);
    }

    [Fact]
    public void Scorer_RepairsMixedPhraseAfterDelayedLayoutSwitch()
    {
        var decision = new LanguageScorer().Evaluate("Fotur рудзы зущзду ензу", 0.60);
        Assert.True(decision.ShouldCorrect);
        Assert.Equal("Fotur helps people type", decision.Replacement);
        Assert.Equal(TextLanguage.English, decision.Language);
    }

    [Fact]
    public void Scorer_RepairsMixedEnglishPhraseAfterCommonArticle()
    {
        var decision = new LanguageScorer().Evaluate("the аштфд лунищфкв", 0.60);
        Assert.True(decision.ShouldCorrect);
        Assert.Equal("the final keyboard", decision.Replacement);
    }

    [Fact]
    public void VoiceCommands_FormatsPunctuation()
    {
        var result = VoiceCommandProcessor.Process("привет запятая как дела вопросительный знак", true);
        Assert.Equal("Привет, как дела?", result);
    }

    [Theory]
    [InlineData("vtyz", "меня")]
    [InlineData("pjden", "зовут")]
    [InlineData("vtyz pjden djdty", "меня зовут вовен")]
    public void Scorer_CorrectsUnknownRussianWordsAndPhrases(string source, string expected)
    {
        var decision = new LanguageScorer().Evaluate(source, 0.64);
        Assert.True(decision.ShouldCorrect);
        Assert.Equal(expected, decision.Replacement);
    }

    [Fact]
    public void Hotkeys_AreNormalizedAndConflictsRejected()
    {
        Assert.True(HotkeyGesture.TryParse("shift + ctrl + d", out var gesture, out _));
        Assert.Equal("Ctrl+Shift+D", gesture.ToString());
        Assert.True(HotkeyGesture.TryParse("command + space", out var macGesture, out _));
        Assert.Equal("Cmd+Space", macGesture.ToString());
        Assert.Equal("Сочетания диктовки и отмены совпадают", HotkeyGesture.ValidatePair("Ctrl+Shift+D", "Ctrl+Shift+D"));
        Assert.Equal("Это сочетание зарезервировано Windows", HotkeyGesture.ValidatePair("Alt+F4", "Ctrl+Alt+Backspace"));
    }
}
