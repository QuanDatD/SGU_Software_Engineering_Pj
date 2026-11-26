using Xunit;
using Zoo.BUS.Services;
using Zoo.DAL.Entities;
using Zoo.DAL.Enums;

namespace Zoo.Tests;

public class PublicationRulesTests
{
    [Fact]
    public void Ready_AcceptsAudioThatMatchesTheCurrentScript()
    {
        var translation = CreateReadyTranslation();

        Assert.True(PublicationRules.Ready(translation));
    }

    [Fact]
    public void Ready_RejectsAudioFromAnOlderScript()
    {
        var translation = CreateReadyTranslation();
        translation.ScriptText = "The script was updated after audio generation.";

        Assert.False(PublicationRules.Ready(translation));
    }

    [Fact]
    public void Ready_RejectsAudioWithoutAPath()
    {
        var translation = CreateReadyTranslation();
        translation.AudioPath = null;

        Assert.False(PublicationRules.Ready(translation));
    }

    [Fact]
    public void Invalidate_ClearsAudioMetadataAndKeepsTheTranslation()
    {
        var translation = CreateReadyTranslation();
        var originalScript = translation.ScriptText;
        translation.IsEditedByAdmin = true;

        PublicationRules.Invalidate(translation);

        Assert.Equal(AudioStatus.Missing, translation.AudioStatus);
        Assert.Null(translation.AudioPath);
        Assert.Null(translation.AudioContentHash);
        Assert.Equal(originalScript, translation.ScriptText);
        Assert.True(translation.IsEditedByAdmin);
        Assert.False(PublicationRules.Ready(translation));
    }

    private static NarrationTranslation CreateReadyTranslation()
    {
        const string script = "Welcome to the elephant exhibit.";
        return new NarrationTranslation
        {
            Title = "Elephant exhibit",
            ScriptText = script,
            AudioStatus = AudioStatus.Ready,
            AudioPath = "elephant-en.mp3",
            AudioContentHash = PublicationRules.Hash(script)
        };
    }
}
