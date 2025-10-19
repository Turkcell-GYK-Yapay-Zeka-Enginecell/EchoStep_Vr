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
        // G�ndermek istedi�iniz veriyi olu�turun
        ApiRequest requestData = new ApiRequest
        {
            session_id = 1011,
            transcript = text
        };

        // JSON'a �evirin
        string jsonString = JsonUtility.ToJson(requestData);

        // UnityWebRequest olu�turun
        UnityWebRequest request = new UnityWebRequest(API_URL, "POST");

        // JSON verisini byte array'e �evirin
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);

        // Upload ve Download handler'lar� ayarlay�n
        request.uploadHandler = new UploadHandlerRaw(jsonBytes);
        request.downloadHandler = new DownloadHandlerBuffer();

        // Content-Type header'�n� ayarlay�n
        request.SetRequestHeader("Content-Type", "application/json");

        // �ste�i g�nderin
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
        buttonText.text = "S�pervizyon �ste";
        sendButton.interactable = true;
        request.Dispose();
    }
}
