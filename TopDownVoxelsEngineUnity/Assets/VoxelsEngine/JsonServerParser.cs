using UnityEngine;

public static class JsonHelper
{
    [System.Serializable]
    private class ServerDetails
    {
        public string host;
        public int port;
    }

    public static (string host, int port) ParseServerDetails(string jsonData)
    {
        var details = JsonUtility.FromJson<ServerDetails>(jsonData);
        return (details.host, details.port);
    }
}