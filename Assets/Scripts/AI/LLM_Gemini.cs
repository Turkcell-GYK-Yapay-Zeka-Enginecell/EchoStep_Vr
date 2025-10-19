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
        StartCoroutine(TalkToLLM("Merhaba, nasýlsýn?"));
    }
    private string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
    private string API_KEY = "AIzaSyBXz4wqPPnfKsFBZJ5zSxtogJdQnzofDww";


    public void Start()
    {
        string prompt;
        DateTime currentDate = DateTime.Now;


        prompt = $"Bugünün tarihi: {currentDate.ToString("dd/MM/yyyy")}\n";
        prompt += "Sana etrafýmda tanýmlamýþ olduðum cisimleri ve konumlarýný söyleyeceðim senden istediðim þey beni bu konuda yönlendirmen yani mesela sol aþaðýda köpek var diyeceðim sen de bana diyeceksin ki 'Sol aþaðýnda köpek var saðdan yürü...'.\n" +
            "Ama sakýn þunu gözlemle veya emin deðilim ama þöyle yapabilirsin gibi þeyler söyleme çünkü bunu sanki körebe oyununda birini yönlendiriyormuþsun gibi düþünerek o kiþiye yardýmcý olmak için yapýyormuþsun gibi konuþ o kiþi etrafýnda neler olduðunu bilmiyor bunu da unutma yani etrafýný betimleyerek te konuþ" +
            "Tabi ki oyun oynuyormuþsun gibi çocuksu þeyleri söyleme ayrýca hopla zýpla takla at gibi saçma direktifler de verme " +
            "basitçe yönlendir yani sola dön, saðdan ilerle falan gibi þeyler söyle ve ayrýca her þeyi türkçe konuþ";


        if (onlyShortAnswers)
            prompt += "Cevaplarýn kýsa, öz ve net olsun.\n";

        if (closedContext)
        {
            prompt += "Eðer bilmediðin bir þey sorarsam bunu bilmediðini söyle.\n";
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

        Debug.Log("Güncellenen messageHistory: " + JsonUtility.ToJson(new RequestBody { contents = messageHistory.ToArray() }));

        // history textini güncelle
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
        Debug.Log("Gönderilen JSON: " + jsonRequestBody); // JSON'u kontrol et


        // UnityWebRequest objesini oluþtur
        using (UnityWebRequest request = new UnityWebRequest(API_URL, "POST"))
        {
            // JSON verisini byte dizisine dönüþtür
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequestBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // HTTP baþlýklarýný (headers) ekle
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-goog-api-key", API_KEY);

            // Ýsteði gönder ve yanýtý bekle
            yield return request.SendWebRequest();

            // Hata kontrolü yap
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("API isteði baþarýsýz: " + request.error);
            }
            else
            {
                Debug.Log("API'den gelen yanýt: " + request.downloadHandler.text);

                try
                {
                    GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(request.downloadHandler.text);
                    string responseText = response.candidates[0].content.parts[0].text;

                    AppendConversation(responseText, "model");
                    AiOrchestrator.Instance.OnLLMResponse(responseText);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("JSON parse hatasý: " + e.Message);
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
