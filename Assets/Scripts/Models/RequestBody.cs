[System.Serializable]
public class RequestBody
{
    public Content[] contents;  // "messages" değil "contents"
}

[System.Serializable]
public class Content
{
    public string role;
    public Part[] parts;  // "content" değil "parts" array'i
}

[System.Serializable]
public class Part
{
    public string text;  // İçerik "text" property'sinde
}

[System.Serializable]
public class GeminiResponse
{
    public Candidate[] candidates;
}

[System.Serializable]
public class Candidate
{
    public Content content;
}
