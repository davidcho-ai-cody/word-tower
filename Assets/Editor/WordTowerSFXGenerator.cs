using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class WordTowerSFXGenerator
{
    private const int SampleRate = 44100;
    private const string OutputFolder = "Assets/Audio/SFX/Generated";

    private enum WaveType
    {
        Sine,
        Square,
        Triangle,
        Saw
    }

    // =========================================================
    // 메뉴
    // =========================================================

    [MenuItem("WordTower/SFX/Generate All SFX")]
    public static void GenerateAllSFX()
    {
        EnsureOutputFolder();

        GenerateLevelUp();
        GenerateVictory();
        GenerateCoin();
        GenerateAttack();
        GenerateHit();
        GenerateWordSuccess();
        GenerateWordFail();

        AssetDatabase.Refresh();

        Debug.Log(
            "[WordTower] 모든 효과음 생성 완료!\n" +
            OutputFolder
        );
    }

    [MenuItem("WordTower/SFX/Generate Level Up")]
    public static void GenerateLevelUp()
    {
        EnsureOutputFolder();

        float duration = 2.2f;
        float[] samples = CreateEmptyBuffer(duration);

        float[] notes =
        {
            523.25f,   // C5
            659.25f,   // E5
            783.99f,   // G5
            1046.50f,  // C6
            1318.51f   // E6
        };

        AddNoteSequence(
            samples,
            notes,
            0.00f,
            0.13f,
            WaveType.Square,
            0.20f
        );

        AddTone(
            samples,
            0.75f,
            1567.98f,
            0.35f,
            WaveType.Sine,
            0.13f
        );

        AddTone(
            samples,
            1.05f,
            2093.00f,
            0.55f,
            WaveType.Sine,
            0.10f
        );

        ApplyFadeOut(samples, 0.55f);

        SaveWav(
            $"{OutputFolder}/WT_LevelUp.wav",
            samples
        );

        AssetDatabase.Refresh();
    }

    [MenuItem("WordTower/SFX/Generate Victory")]
    public static void GenerateVictory()
    {
        EnsureOutputFolder();

        float duration = 2.8f;
        float[] samples = CreateEmptyBuffer(duration);

        float[] notes =
        {
            523.25f,
            659.25f,
            783.99f,
            1046.50f
        };

        AddNoteSequence(
            samples,
            notes,
            0.00f,
            0.20f,
            WaveType.Square,
            0.17f
        );

        AddTone(
            samples,
            0.90f,
            523.25f,
            1.30f,
            WaveType.Sine,
            0.08f
        );

        AddTone(
            samples,
            0.90f,
            659.25f,
            1.30f,
            WaveType.Sine,
            0.08f
        );

        AddTone(
            samples,
            0.90f,
            783.99f,
            1.30f,
            WaveType.Sine,
            0.08f
        );

        AddTone(
            samples,
            0.90f,
            1046.50f,
            1.30f,
            WaveType.Sine,
            0.05f
        );

        ApplyFadeOut(samples, 0.70f);

        SaveWav(
            $"{OutputFolder}/WT_Victory.wav",
            samples
        );

        AssetDatabase.Refresh();
    }

    [MenuItem("WordTower/SFX/Generate Coin")]
    public static void GenerateCoin()
    {
        EnsureOutputFolder();

        float duration = 0.55f;
        float[] samples = CreateEmptyBuffer(duration);

        AddTone(
            samples,
            0.00f,
            1318.51f,
            0.12f,
            WaveType.Square,
            0.16f
        );

        AddTone(
            samples,
            0.09f,
            1975.53f,
            0.22f,
            WaveType.Sine,
            0.16f
        );

        AddTone(
            samples,
            0.17f,
            2637.02f,
            0.20f,
            WaveType.Sine,
            0.08f
        );

        ApplyFadeOut(samples, 0.18f);

        SaveWav(
            $"{OutputFolder}/WT_Coin.wav",
            samples
        );

        AssetDatabase.Refresh();
    }

    [MenuItem("WordTower/SFX/Generate Attack")]
    public static void GenerateAttack()
    {
        EnsureOutputFolder();

        float duration = 0.42f;
        float[] samples = CreateEmptyBuffer(duration);

        AddFrequencySweep(
            samples,
            0.00f,
            0.28f,
            1100f,
            180f,
            WaveType.Saw,
            0.17f
        );

        AddNoiseBurst(
            samples,
            0.02f,
            0.16f,
            0.08f
        );

        ApplyFadeOut(samples, 0.14f);

        SaveWav(
            $"{OutputFolder}/WT_Attack.wav",
            samples
        );

        AssetDatabase.Refresh();
    }

    [MenuItem("WordTower/SFX/Generate Hit")]
    public static void GenerateHit()
    {
        EnsureOutputFolder();

        float duration = 0.38f;
        float[] samples = CreateEmptyBuffer(duration);

        AddNoiseBurst(
            samples,
            0.00f,
            0.09f,
            0.22f
        );

        AddFrequencySweep(
            samples,
            0.00f,
            0.22f,
            230f,
            75f,
            WaveType.Square,
            0.18f
        );

        AddTone(
            samples,
            0.02f,
            110f,
            0.16f,
            WaveType.Sine,
            0.16f
        );

        ApplyFadeOut(samples, 0.17f);

        SaveWav(
            $"{OutputFolder}/WT_Hit.wav",
            samples
        );

        AssetDatabase.Refresh();
    }

    [MenuItem("WordTower/SFX/Generate Word Success")]
    public static void GenerateWordSuccess()
    {
        EnsureOutputFolder();

        float duration = 0.85f;
        float[] samples = CreateEmptyBuffer(duration);

        float[] notes =
        {
            659.25f,
            783.99f,
            1046.50f
        };

        AddNoteSequence(
            samples,
            notes,
            0.00f,
            0.13f,
            WaveType.Sine,
            0.18f
        );

        AddTone(
            samples,
            0.30f,
            1567.98f,
            0.30f,
            WaveType.Sine,
            0.07f
        );

        ApplyFadeOut(samples, 0.28f);

        SaveWav(
            $"{OutputFolder}/WT_WordSuccess.wav",
            samples
        );

        AssetDatabase.Refresh();
    }

    [MenuItem("WordTower/SFX/Generate Word Fail")]
    public static void GenerateWordFail()
    {
        EnsureOutputFolder();

        float duration = 0.75f;
        float[] samples = CreateEmptyBuffer(duration);

        AddFrequencySweep(
            samples,
            0.00f,
            0.45f,
            390f,
            110f,
            WaveType.Square,
            0.15f
        );

        AddTone(
            samples,
            0.28f,
            90f,
            0.30f,
            WaveType.Sine,
            0.13f
        );

        ApplyFadeOut(samples, 0.28f);

        SaveWav(
            $"{OutputFolder}/WT_WordFail.wav",
            samples
        );

        AssetDatabase.Refresh();
    }

    // =========================================================
    // 음원 생성
    // =========================================================

    private static float[] CreateEmptyBuffer(float duration)
    {
        int totalSamples =
            Mathf.CeilToInt(SampleRate * duration);

        return new float[totalSamples];
    }

    private static void AddNoteSequence(
        float[] samples,
        float[] frequencies,
        float startTime,
        float noteLength,
        WaveType waveType,
        float volume)
    {
        for (int i = 0; i < frequencies.Length; i++)
        {
            float noteStart =
                startTime + (i * noteLength);

            AddTone(
                samples,
                noteStart,
                frequencies[i],
                noteLength * 0.90f,
                waveType,
                volume
            );
        }
    }

    private static void AddTone(
        float[] samples,
        float startTime,
        float frequency,
        float duration,
        WaveType waveType,
        float volume)
    {
        int startSample =
            Mathf.RoundToInt(startTime * SampleRate);

        int length =
            Mathf.RoundToInt(duration * SampleRate);

        for (int i = 0; i < length; i++)
        {
            int targetIndex =
                startSample + i;

            if (targetIndex >= samples.Length)
                break;

            float localTime =
                (float)i / SampleRate;

            float progress =
                (float)i / length;

            float envelope =
                CreateEnvelope(progress);

            float wave =
                GenerateWave(
                    waveType,
                    frequency,
                    localTime
                );

            samples[targetIndex] +=
                wave * volume * envelope;
        }
    }

    private static void AddFrequencySweep(
        float[] samples,
        float startTime,
        float duration,
        float startFrequency,
        float endFrequency,
        WaveType waveType,
        float volume)
    {
        int startSample =
            Mathf.RoundToInt(startTime * SampleRate);

        int length =
            Mathf.RoundToInt(duration * SampleRate);

        float phase = 0f;

        for (int i = 0; i < length; i++)
        {
            int targetIndex =
                startSample + i;

            if (targetIndex >= samples.Length)
                break;

            float progress =
                (float)i / length;

            float frequency =
                Mathf.Lerp(
                    startFrequency,
                    endFrequency,
                    progress
                );

            phase += frequency / SampleRate;

            float wave =
                GenerateWaveFromPhase(
                    waveType,
                    phase
                );

            float envelope =
                Mathf.Pow(
                    1f - progress,
                    1.4f
                );

            samples[targetIndex] +=
                wave * volume * envelope;
        }
    }

    private static void AddNoiseBurst(
        float[] samples,
        float startTime,
        float duration,
        float volume)
    {
        int startSample =
            Mathf.RoundToInt(startTime * SampleRate);

        int length =
            Mathf.RoundToInt(duration * SampleRate);

        for (int i = 0; i < length; i++)
        {
            int targetIndex =
                startSample + i;

            if (targetIndex >= samples.Length)
                break;

            float progress =
                (float)i / length;

            float envelope =
                Mathf.Pow(
                    1f - progress,
                    2.3f
                );

            float noise =
                Random.Range(-1f, 1f);

            samples[targetIndex] +=
                noise * volume * envelope;
        }
    }

    private static float GenerateWave(
        WaveType waveType,
        float frequency,
        float time)
    {
        float phase =
            frequency * time;

        return GenerateWaveFromPhase(
            waveType,
            phase
        );
    }

    private static float GenerateWaveFromPhase(
        WaveType waveType,
        float phase)
    {
        float normalized =
            phase - Mathf.Floor(phase);

        switch (waveType)
        {
            case WaveType.Square:
                return normalized < 0.5f
                    ? 1f
                    : -1f;

            case WaveType.Triangle:
                return
                    1f -
                    4f *
                    Mathf.Abs(
                        normalized - 0.5f
                    );

            case WaveType.Saw:
                return
                    2f * normalized - 1f;

            default:
                return
                    Mathf.Sin(
                        2f *
                        Mathf.PI *
                        normalized
                    );
        }
    }

    private static float CreateEnvelope(float progress)
    {
        const float attack = 0.08f;
        const float release = 0.30f;

        float envelope = 1f;

        if (progress < attack)
        {
            envelope =
                progress / attack;
        }

        if (progress > 1f - release)
        {
            envelope *=
                (1f - progress) /
                release;
        }

        return Mathf.Clamp01(envelope);
    }

    private static void ApplyFadeOut(
        float[] samples,
        float fadeDuration)
    {
        int fadeSamples =
            Mathf.RoundToInt(
                fadeDuration * SampleRate
            );

        int start =
            Mathf.Max(
                0,
                samples.Length - fadeSamples
            );

        int actualFadeLength =
            samples.Length - start;

        for (int i = start; i < samples.Length; i++)
        {
            float progress =
                (float)(i - start) /
                actualFadeLength;

            samples[i] *=
                1f - progress;
        }
    }

    // =========================================================
    // WAV 저장
    // =========================================================

    private static void SaveWav(
        string filePath,
        float[] samples)
    {
        string directory =
            Path.GetDirectoryName(filePath);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using (
            FileStream fileStream =
                new FileStream(
                    filePath,
                    FileMode.Create
                )
        )
        using (
            BinaryWriter writer =
                new BinaryWriter(fileStream)
        )
        {
            int dataSize =
                samples.Length * 2;

            WriteAscii(writer, "RIFF");
            writer.Write(36 + dataSize);

            WriteAscii(writer, "WAVE");
            WriteAscii(writer, "fmt ");

            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);

            WriteAscii(writer, "data");
            writer.Write(dataSize);

            foreach (float sample in samples)
            {
                float clamped =
                    Mathf.Clamp(
                        sample,
                        -1f,
                        1f
                    );

                short pcm =
                    (short)(
                        clamped *
                        short.MaxValue
                    );

                writer.Write(pcm);
            }
        }

        Debug.Log(
            "[WordTower SFX] 생성 완료: " +
            filePath
        );
    }

    private static void WriteAscii(
        BinaryWriter writer,
        string text)
    {
        writer.Write(
            Encoding.ASCII.GetBytes(text)
        );
    }

    // =========================================================
    // 폴더 생성
    // =========================================================

    private static void EnsureOutputFolder()
    {
        if (!Directory.Exists(OutputFolder))
        {
            Directory.CreateDirectory(
                OutputFolder
            );
        }
    }
}