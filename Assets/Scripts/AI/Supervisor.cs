using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Supervisor : MonoBehaviour
{
    [SerializeField] private string API_URL = "http://91.241.50.84:8002/supervision/";
    [SerializeField] TextMeshProUGUI resultText;

    [SerializeField] Button sendButton;
    [SerializeField] TextMeshProUGUI buttonText;


    [SerializeField]
    TextMeshProUGUI historyText;

    [System.Serializable]
    public class ApiRequest
    {
        public int session_id;
        public string transcript;
    }

    public void SendPostRequest()
    {
        buttonText.text = "Cevap Bekleniyor...";
        sendButton.interactable = false;
        StartCoroutine(PostData(historyText.text));
    }

    IEnumerator PostData(string text)
    {
        // Göndermek istediðiniz veriyi oluþturun
        ApiRequest requestData = new ApiRequest
        {
            session_id = 1011,
            transcript = text
        };

        // JSON'a çevirin
        string jsonString = JsonUtility.ToJson(requestData);

        // UnityWebRequest oluþturun
        UnityWebRequest request = new UnityWebRequest(API_URL, "POST");

        // JSON verisini byte array'e çevirin
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);

        // Upload ve Download handler'larý ayarlayýn
        request.uploadHandler = new UploadHandlerRaw(jsonBytes);
        request.downloadHandler = new DownloadHandlerBuffer();

        // Content-Type header'ýný ayarlayýn
        request.SetRequestHeader("Content-Type", "application/json");

        // Ýsteði gönderin
        yield return request.SendWebRequest();

        // Sonucu kontrol edin
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Success: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Error: " + request.error);
        }

        resultText.text = request.downloadHandler.text;
        buttonText.text = "Süpervizyon Ýste";
        sendButton.interactable = true;
        request.Dispose();
    }
}
