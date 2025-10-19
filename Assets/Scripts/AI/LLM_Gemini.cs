using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class LLM_Gemini : MonoBehaviourSingleton<LLM_Gemini>
{
    [Header("NPC")]

    //[SerializeField] TextMeshProUGUI history;

    [SerializeField] private bool onlyShortAnswers = false;
    [SerializeField] private bool closedContext = false;
    List<Content> messageHistory = new List<Content>();

    [ContextMenu("Test Gemini")]
    public void TestGemini()
    {
        StartCoroutine(TalkToLLM("Merhaba, nas�ls�n?"));
    }
    private string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
    private string API_KEY = "AIzaSyBXz4wqPPnfKsFBZJ5zSxtogJdQnzofDww";


    public void Start()
    {
        string prompt;
        DateTime currentDate = DateTime.Now;


        prompt = $"Bug�n�n tarihi: {currentDate.ToString("dd/MM/yyyy")}\n";
        prompt += "Sana etraf�mda tan�mlam�� oldu�um cisimleri ve konumlar�n� s�yleyece�im senden istedi�im �ey beni bu konuda y�nlendirmen yani mesela sol a�a��da k�pek var diyece�im sen de bana diyeceksin ki 'Sol a�a��nda k�pek var sa�dan y�r�...'.\n" +
            "Ama sak�n �unu g�zlemle veya emin de�ilim ama ��yle yapabilirsin gibi �eyler s�yleme ��nk� bunu sanki k�rebe oyununda birini y�nlendiriyormu�sun gibi d���nerek o ki�iye yard�mc� olmak i�in yap�yormu�sun gibi konu� o ki�i etraf�nda neler oldu�unu bilmiyor bunu da unutma yani etraf�n� betimleyerek te konu�" +
            "Tabi ki oyun oynuyormu�sun gibi �ocuksu �eyleri s�yleme ayr�ca hopla z�pla takla at gibi sa�ma direktifler de verme " +
            "basit�e y�nlendir yani sola d�n, sa�dan ilerle falan gibi �eyler s�yle ve ayr�ca her �eyi t�rk�e konu�";


        if (onlyShortAnswers)
            prompt += "Cevaplar�n k�sa, �z ve net olsun.\n";

        if (closedContext)
        {
            prompt += "E�er bilmedi�in bir �ey sorarsam bunu bilmedi�ini s�yle.\n";
        }

        prompt += "\n===";

        Debug.Log("Prompt: " + prompt);

        messageHistory = new List<Content>();
        AppendConversation(prompt, "user");

    }


    private void AppendConversation(string msg, string myRole)
    {
        Content newContent = new Content()
        {
            role = myRole,
            parts = new Part[] { new Part { text = msg } }
        };
        messageHistory.Add(newContent);

        Debug.Log("G�ncellenen messageHistory: " + JsonUtility.ToJson(new RequestBody { contents = messageHistory.ToArray() }));

        // history textini g�ncelle
        //history.text = "";

        //foreach (var content in messageHistory)
        //{
        //    foreach (var part in content.parts)
        //    {
        //        history.text += $"{content.role}: {part.text}\n";
        //    }
        //}
    }


    public IEnumerator TalkToLLM(string msg)
    {
        RequestBody requestBody = new RequestBody();
        AppendConversation(msg, "user");
        requestBody.contents = messageHistory.ToArray(); // contents property'si

        string jsonRequestBody = JsonUtility.ToJson(requestBody);
        Debug.Log("G�nderilen JSON: " + jsonRequestBody); // JSON'u kontrol et


        // UnityWebRequest objesini olu�tur
        using (UnityWebRequest request = new UnityWebRequest(API_URL, "POST"))
        {
            // JSON verisini byte dizisine d�n��t�r
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequestBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // HTTP ba�l�klar�n� (headers) ekle
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-goog-api-key", API_KEY);

            // �ste�i g�nder ve yan�t� bekle
            yield return request.SendWebRequest();

            // Hata kontrol� yap
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("API iste�i ba�ar�s�z: " + request.error);
            }
            else
            {
                Debug.Log("API'den gelen yan�t: " + request.downloadHandler.text);

                try
                {
                    GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(request.downloadHandler.text);
                    string responseText = response.candidates[0].content.parts[0].text;

                    AppendConversation(responseText, "model");
                    AiOrchestrator.Instance.OnLLMResponse(responseText);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("JSON parse hatas�: " + e.Message);
                    Debug.LogError("Raw response: " + request.downloadHandler.text);
                }
            }

        }


    }


    internal void SendApiPost(string userInput)
    {
        StartCoroutine(TalkToLLM(userInput));
    }
}
