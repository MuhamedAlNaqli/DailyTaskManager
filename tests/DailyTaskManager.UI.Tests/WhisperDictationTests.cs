using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NAudio.Wave;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Infrastructure.Services;

namespace DailyTaskManager.UI.Tests;

/// <summary>
/// Dictation: cutting the microphone stream into phrases, and (when the speech model is installed) the whole
/// chain from audio to text
/// </summary>
[TestClass]
public class WhisperDictationTests
{
    private static byte[] Tone(double seconds, double amplitude)
    {
        var samples = (int)(16000 * seconds);
        var bytes = new byte[samples * 2];
        for (var i = 0; i < samples; i++)
        {
            var value = (short)(Math.Sin(i * 2 * Math.PI * 220 / 16000) * amplitude * short.MaxValue);
            BitConverter.GetBytes(value).CopyTo(bytes, i * 2);
        }

        return bytes;
    }

    private static void Feed(WhisperSpeechService.PhraseCutter cutter, byte[] audio)
    {
        // The microphone delivers 50 ms chunks
        const int chunk = 1600;
        for (var offset = 0; offset < audio.Length; offset += chunk)
        {
            var count = Math.Min(chunk, audio.Length - offset);
            cutter.Feed(audio.AsSpan(offset, count).ToArray(), count);
        }
    }

    [TestMethod]
    public void PhraseCutter_ShouldEmitOnePhraseAfterAPause()
    {
        var phrases = new List<float[]>();
        var cutter = new WhisperSpeechService.PhraseCutter(phrases.Add);

        Feed(cutter, Tone(1.0, 0.001));   // room noise
        Feed(cutter, Tone(1.5, 0.3));     // speech
        Assert.AreEqual(0, phrases.Count, "Nothing is emitted while the speaker is still talking");

        Feed(cutter, Tone(1.2, 0.001));   // pause
        Assert.AreEqual(1, phrases.Count, "A pause ends the phrase");
        Assert.IsTrue(phrases[0].Length >= 16000 * 3 / 2, "The whole phrase is kept");
        Assert.IsTrue(phrases[0].Length < 16000 * 3, "The long pause is not");
    }

    [TestMethod]
    public void PhraseCutter_ShouldIgnoreShortBlipsAndFlushTheLastWords()
    {
        var phrases = new List<float[]>();
        var cutter = new WhisperSpeechService.PhraseCutter(phrases.Add);

        Feed(cutter, Tone(0.1, 0.3));     // a click
        Feed(cutter, Tone(1.5, 0.001));
        Assert.AreEqual(0, phrases.Count, "A click is not speech");

        Feed(cutter, Tone(1.0, 0.3));     // speech, then the user presses Stop
        cutter.Flush();
        Assert.AreEqual(1, phrases.Count, "Stopping hands over the last words");
    }

    [TestMethod]
    public async Task Transcribe_ShouldUnderstandARecordedSentence_WhenTheModelIsInstalled()
    {
        if (!File.Exists(WhisperSpeechService.ModelPath))
        {
            Assert.Inconclusive("The speech model is downloaded on first use of dictation; it is not on this machine.");
        }

        var settings = new Mock<ISettingsService>();
        using var service = new WhisperSpeechService(NullLogger<WhisperSpeechService>.Instance, settings.Object);

        using var reader = new WaveFileReader(Path.Combine(AppContext.BaseDirectory, "Assets", "dictation-sample.wav"));
        var provider = reader.ToSampleProvider();
        var samples = new List<float>();
        var buffer = new float[4096];
        int read;
        while ((read = provider.Read(buffer, 0, buffer.Length)) > 0) samples.AddRange(buffer.Take(read));

        var text = await service.TranscribeAsync(samples.ToArray(), "en");
        var watch = System.Diagnostics.Stopwatch.StartNew();
        await service.TranscribeAsync(samples.ToArray(), "en");
        Console.WriteLine($"Warm transcription of {samples.Count / 16000.0:0.0} s of audio took {watch.ElapsedMilliseconds} ms");
        Console.WriteLine("Transcribed: " + text);

        StringAssert.Contains(text.ToLowerInvariant(), "tariff");
        StringAssert.Contains(text.ToLowerInvariant(), "testing");
    }
    [TestMethod]
    public async Task Dictation_ShouldCutRealSpeechIntoPhrasesAndTranscribeThem_WhenTheModelIsInstalled()
    {
        if (!File.Exists(WhisperSpeechService.ModelPath)) Assert.Inconclusive("The speech model is not on this machine.");

        // The recording goes through the same phrase cutter the microphone uses, with a pause appended like a user who stops talking
        var phrases = new List<float[]>();
        var cutter = new WhisperSpeechService.PhraseCutter(phrases.Add);
        using (var reader = new WaveFileReader(Path.Combine(AppContext.BaseDirectory, "Assets", "dictation-sample.wav")))
        {
            var bytes = new byte[1600];
            int read;
            while ((read = reader.Read(bytes, 0, bytes.Length)) > 0) cutter.Feed(bytes, read);
        }

        Feed(cutter, Tone(1.2, 0.0005));
        Assert.IsTrue(phrases.Count >= 1, "The speech is cut into at least one phrase");

        var settings = new Mock<ISettingsService>();
        using var service = new WhisperSpeechService(NullLogger<WhisperSpeechService>.Instance, settings.Object);
        var text = string.Join(" ", await Task.WhenAll(phrases.Select(p => service.TranscribeAsync(p, "en"))));
        Console.WriteLine($"{phrases.Count} phrase(s): {text}");

        StringAssert.Contains(text.ToLowerInvariant(), "tariff");
        StringAssert.Contains(text.ToLowerInvariant(), "testing");
    }
}
