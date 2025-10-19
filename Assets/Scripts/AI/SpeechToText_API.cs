using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.IO;
using System;

public class SpeechToText_API : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI textDisplay;
    [SerializeField] AudioSource audioSource;
    [SerializeField] private string API_KEY = "";
    [SerializeField] private string API_URL = "http://91.241.50.84:8000/transcribe-only/";
    MemoryStream stream;

    [ContextMenu("Test Speech to Text")]
    public void TestSpeechToText()
    {
        StartSpeaking();
    }

    public void StartSpeaking()
    {
        stream = new MemoryStream();
        Debug.Log("Microphone is starting to record...");
        audioSource.clip = Microphone.Start(null, false, 10, 44100);
        StartCoroutine(RecordAudio(audioSource.clip));
    }

    IEnumerator RecordAudio(AudioClip audioClip)
    {
        while (Microphone.IsRecording(null))
        {
            yield return null;
        }
        ConvertClipToWav(audioClip);
        StartCoroutine(STT());
    }
    private IEnumerator STT()
    {
        SpeechToTextData speechToTextData = new SpeechToTextData();
        UnityWebRequest request = null;

        // Unity 6 için HTTP ayarý
        Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.None);

        // Try-catch bloðunu yield return'dan ayýr
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        byte[] audioBytes = stream.ToArray();
        formData.Add(new MultipartFormFileSection("file", audioBytes, "recording.wav", "audio/wav"));

        request = UnityWebRequest.Post(API_URL, formData);
        request.SetRequestHeader("Accept", "application/json");

        print("Sending audio data to the API...");
        //AiOrchestrator.Instance.OnSTTWaiting();

        // Yield return'ü try-catch dýþýnda yap
        yield return request.SendWebRequest();

        // Sonucu kontrol et - try-catch burada kullanýlabilir
        try
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log("API'den gelen yanýt: " + responseText);

                speechToTextData = JsonUtility.FromJson<SpeechToTextData>(responseText);
                textDisplay.text = speechToTextData.transcription;
                //AiOrchestrator.Instance.OnUserTalkFinished(speechToTextData.transcription);
            }
            else
            {
                Debug.LogError("API hatasý: " + request.error);
                Debug.LogError("Response code: " + request.responseCode);
                Debug.LogError("Response text: " + request.downloadHandler.text);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("JSON Parse hatasý: " + e.Message);
        }
        finally
        {
            if (request != null)
                request.Dispose();
        }
    }


    [Serializable]
    public class SpeechToTextData
    {
        public string file_path;
        public string transcription;
    }

    // WAV conversion kodu ayný kalacak...
    public Stream ConvertClipToWav(AudioClip clip)
    {
        var data = new float[clip.samples * clip.channels];
        clip.GetData(data, 0);
        if (stream != null) stream.Dispose();
        stream = new MemoryStream();

        var bitsPerSample = (ushort)16;
        var chunkID = "RIFF";
        var format = "WAVE";
        var subChunk1ID = "fmt ";
        var subChunk1Size = (uint)16;
        var audioFormat = (ushort)1;
        var numChannels = (ushort)clip.channels;
        var sampleRate = (uint)clip.frequency;
        var byteRate = (uint)(sampleRate * clip.channels * bitsPerSample / 8);
        var blockAlign = (ushort)(numChannels * bitsPerSample / 8);
        var subChunk2ID = "data";
        var subChunk2Size = (uint)(data.Length * clip.channels * bitsPerSample / 8);
        var chunkSize = (uint)(36 + subChunk2Size);

        WriteString(stream, chunkID);
        WriteUInt(stream, chunkSize);
        WriteString(stream, format);
        WriteString(stream, subChunk1ID);
        WriteUInt(stream, subChunk1Size);
        WriteShort(stream, audioFormat);
        WriteShort(stream, numChannels);
        WriteUInt(stream, sampleRate);
        WriteUInt(stream, byteRate);
        WriteShort(stream, blockAlign);
        WriteShort(stream, bitsPerSample);
        WriteString(stream, subChunk2ID);
        WriteUInt(stream, subChunk2Size);

        foreach (var sample in data)
        {
            var deNormalizedSample = (short)0;
            if (sample > 0)
            {
                var temp = sample * short.MaxValue;
                if (temp > short.MaxValue)
                    temp = short.MaxValue;
                deNormalizedSample = (short)temp;
            }
            if (sample < 0)
            {
                var temp = sample * (-short.MinValue);
                if (temp < short.MinValue)
                    temp = short.MinValue;
                deNormalizedSample = (short)temp;
            }
            WriteShort(stream, (ushort)deNormalizedSample);
        }
        return stream;
    }

    private void WriteUInt(Stream stream, uint data)
    {
        stream.WriteByte((byte)(data & 0xFF));
        stream.WriteByte((byte)((data >> 8) & 0xFF));
        stream.WriteByte((byte)((data >> 16) & 0xFF));
        stream.WriteByte((byte)((data >> 24) & 0xFF));
    }

    private void WriteShort(Stream stream, ushort data)
    {
        stream.WriteByte((byte)(data & 0xFF));
        stream.WriteByte((byte)((data >> 8) & 0xFF));
    }

    private void WriteString(Stream stream, string value)
    {
        foreach (var character in value)
            stream.WriteByte((byte)character);
    }
}
