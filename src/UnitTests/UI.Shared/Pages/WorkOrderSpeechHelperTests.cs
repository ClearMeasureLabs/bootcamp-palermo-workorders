using System.Reflection;
using System.Runtime.CompilerServices;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;
using Toolbelt.Blazor.SpeechSynthesis;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderSpeechHelperTests
{
    [Test]
    public void CreateUtterance_ShouldSetTextAndLang()
    {
        var utterance = WorkOrderSpeechHelper.CreateUtterance(
            "Hola mundo",
            "es-ES",
            []);

        utterance.Text.ShouldBe("Hola mundo");
        utterance.Lang.ShouldBe("es-ES");
        utterance.Voice.ShouldBeNull();
    }

    [Test]
    public void CreateUtterance_ShouldSelectMatchingVoice_ByLanguagePrefix()
    {
        var english = CreateVoice("en-US");
        var spanish = CreateVoice("es-MX");

        var utterance = WorkOrderSpeechHelper.CreateUtterance(
            "Buenos dias",
            "es-ES",
            [english, spanish]);

        utterance.Voice.ShouldBe(spanish);
    }

    [Test]
    public void CreateUtterance_ShouldLeaveVoiceUnset_WhenNoMatch()
    {
        var english = CreateVoice("en-US");

        var utterance = WorkOrderSpeechHelper.CreateUtterance(
            "Bonjour",
            "fr-FR",
            [english]);

        utterance.Voice.ShouldBeNull();
    }

    private static SpeechSynthesisVoice CreateVoice(string lang)
    {
        var voice = (SpeechSynthesisVoice)RuntimeHelpers.GetUninitializedObject(typeof(SpeechSynthesisVoice));
        var langField = typeof(SpeechSynthesisVoice).GetField("<Lang>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);
        langField.ShouldNotBeNull();
        langField.SetValue(voice, lang);
        return voice;
    }
}
