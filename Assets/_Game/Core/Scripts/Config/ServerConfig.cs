using UnityEngine;

namespace Core.Config
{
    [CreateAssetMenu(fileName = "ServerConfig", menuName = "Primora/Server Config")]
    public class ServerConfig : ScriptableObject
    {
        [Header("API Settings")]
        [Tooltip("Base URL of the Backend API (e.g. http://localhost:8000)")]
        public string ApiBaseUrl = "http://localhost:8000";

        [Tooltip("Toggle on to use mock data instead of real API calls if disconnected")]
        public bool UseMockServer = false;
        
        public string GetFullUrl(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
                return ApiBaseUrl;
                
            if (endpoint.StartsWith("/"))
                return ApiBaseUrl + endpoint;
                
            return ApiBaseUrl + "/" + endpoint;
        }
    }
}
