using TMPro;
using UnityEngine;

public class TextChoiceSelector : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI choice;

    private void Awake()
    {
        GetComponent<UnityEngine.UI.Button>().onClick.AddListener(OnSelect);
    }
    public void OnSelect()
    {
        //AiOrchestrator.Instance.OnUserTalkChoose(choice.text);
    }
}
