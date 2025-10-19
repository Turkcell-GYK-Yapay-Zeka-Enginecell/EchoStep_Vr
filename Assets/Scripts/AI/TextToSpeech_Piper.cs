using System.Collections.Generic;
using Unity.InferenceEngine;
using UnityEngine;

public class TextToSpeech_Piper1 : MonoBehaviour
{
    [SerializeField] private ModelAsset modelAsset;
    [SerializeField] private AudioSource audioSource;

    private Model runtimeModel;
    private Worker worker;

    [Header("Test")]
    public string testText = "Merhaba Unity";

    private static readonly Dictionary<char, int> PhonemeIdMap = new Dictionary<char, int>
    {
        {'a', 14}, {'e', 18}, {'i', 21}, {'o', 27}, {'u', 33},
        {'b', 15}, {'c', 16}, {'d', 17}, {'f', 19}, {'h', 20},
        {'j', 22}, {'k', 23}, {'l', 24}, {'m', 25}, {'n', 26},
        {'p', 28}, {'r', 30}, {'s', 31}, {'t', 32}, {'v', 34},
        {'y', 37}, {'z', 38}, {'ç', 40}, {' ', 3}, {'.', 10}
    };

    void Start()
    {
        try
        {
            runtimeModel = ModelLoader.Load(modelAsset);
            worker = new Worker(runtimeModel, BackendType.GPUCompute);

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            Debug.Log("✅ InferenceEngine ile Piper TTS yüklendi!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Model yükleme hatası: {e.Message}");
        }
    }

    [ContextMenu("Generate Turkish Speech")]
    public void TestGenerateSpeech()
    {
        GenerateSpeech(testText);
    }

    public void GenerateSpeech(string text)
    {
        try
        {
            var phonemeIds = TextToPhonemeIds(text);
            Debug.Log($"🔤 Phoneme IDs: {string.Join(", ", phonemeIds)}");

            var actualShape = new TensorShape(1, phonemeIds.Length);

            using var inputTensor = new Tensor<int>(actualShape, phonemeIds);

            // Schedule işlemi
            worker.Schedule(inputTensor);

            // Unity 6.1 InferenceEngine'de sadece PeekOutput() var
            var outputTensor = worker.PeekOutput() as Tensor<float>;

            if (outputTensor != null)
            {
                Debug.Log($"🎵 Output tensor alındı: {outputTensor.count} samples");

                var audioData = new float[outputTensor.count];

                // Tensor verilerine doğrudan erişim
                for (int i = 0; i < outputTensor.count; i++)
                {
                    audioData[i] = outputTensor[i];
                }

                var audioClip = CreateAudioClip(audioData);

                if (audioClip != null)
                {
                    audioSource.clip = audioClip;
                    audioSource.Play();
                    Invoke(nameof(OnAudioEnd), audioSource.clip.length);
                    Debug.Log($"🎉 Türkçe ses başarıyla üretildi!");
                }
            }
            else
            {
                Debug.LogError("❌ Output tensor alınamadı!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TTS hatası: {e.Message}");
        }
    }

    public void OnAudioEnd()
    {
        Debug.Log("🔈 Ses oynatma tamamlandı.");

        AiOrchestrator.Instance.OnSTTTalkFinished();
    }

    private int[] TextToPhonemeIds(string text)
    {
        List<int> phonemeIds = new List<int>();

        phonemeIds.Add(1); // Start token "^"

        foreach (char c in text.ToLower())
        {
            if (PhonemeIdMap.ContainsKey(c))
            {
                phonemeIds.Add(PhonemeIdMap[c]);
            }
            else if (c == 'ı') phonemeIds.Add(79); // Turkish ı
            else if (c == 'ğ') phonemeIds.Add(68); // Turkish ğ
            else if (c == 'ş') phonemeIds.Add(96); // Turkish ş
            else if (c == 'ö') phonemeIds.Add(45); // Turkish ö
            else if (c == 'ü') phonemeIds.Add(105); // Turkish ü
            else phonemeIds.Add(3); // Space
        }

        phonemeIds.Add(2); // End token "$"

        return phonemeIds.ToArray();
    }

    private AudioClip CreateAudioClip(float[] audioData)
    {
        if (audioData == null || audioData.Length == 0) return null;

        for (int i = 0; i < audioData.Length; i++)
        {
            audioData[i] = Mathf.Clamp(audioData[i], -1f, 1f);
        }

        var clip = AudioClip.Create("PiperTTS", audioData.Length, 1, 22050, false);
        clip.SetData(audioData, 0);
        return clip;
    }

    void OnDestroy()
    {
        worker?.Dispose();
    }
}
