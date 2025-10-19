using System;
using TMPro;
using UnityEngine;

internal class AiOrchestrator : MonoBehaviourSingleton<AiOrchestrator>
{
    [SerializeField] LLM_Gemini largeLanguageModel;
    [SerializeField] TextToSpeech_Piper textToSpeech;
    //[SerializeField] SpeechToText_API speechToText;


    bool isSTTTalking = false;
    bool isWaitingForLLMToResponse = false;

    public void OnPlayerHitButton(string text)
    {
        Debug.Log("Sağ grip tuşuna basıldı!");
        if (isSTTTalking) return;
        if (isWaitingForLLMToResponse) return;
        AskLLM(text);
    }

    internal void AskLLM(string text)
    {
        isWaitingForLLMToResponse = true;
        largeLanguageModel.SendApiPost(text);
    }

    internal void OnLLMResponse(string text)
    {
        isWaitingForLLMToResponse = false;
        isSTTTalking = true;
        textToSpeech.GenerateSpeech(text);
    }

    internal void OnSTTTalkFinished()
    {
        isSTTTalking = false;
    }

    [ContextMenu("Test ")]
    public void Test()
    {
        //OnUserTalkChoose("Merhabalar, nasılsın efenim?");
    }
}