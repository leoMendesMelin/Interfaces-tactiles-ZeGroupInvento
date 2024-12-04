using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class RoomNetworkService : MonoBehaviour
{
    private const string SERVER_URL = "http://localhost:9090";

    public IEnumerator FetchRoom(System.Action<Room> onSuccess)
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{SERVER_URL}/room"))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                jsonResponse = jsonResponse.TrimStart('[').TrimEnd(']');
                Room room = JsonUtility.FromJson<Room>(jsonResponse);
                onSuccess?.Invoke(room);
            }
        }
    }

    public IEnumerator AddRoomElement(string roomId, RoomElement element, System.Action<Room> onSuccess)
    {
        string jsonData = JsonUtility.ToJson(element);
        using (UnityWebRequest request = new UnityWebRequest($"{SERVER_URL}/room/{roomId}/elements", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success && !string.IsNullOrEmpty(request.downloadHandler.text))
            {
                Debug.Log($"Received response: {request.downloadHandler.text}");
                Room updatedRoom = JsonUtility.FromJson<Room>(request.downloadHandler.text);
                if (updatedRoom != null)
                {
                    onSuccess?.Invoke(updatedRoom);
                }
                else
                {
                    Debug.LogError("Failed to parse room data from response");
                }
            }
            else
            {
                Debug.LogError($"Error: {request.error}");
                if (!string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    Debug.LogError($"Response: {request.downloadHandler.text}");
                }
            }
        }
    }

    public IEnumerator UpdateRoomElement(string roomId, RoomElement element, System.Action<Room> onSuccess)
    {
        string jsonData = JsonUtility.ToJson(element);
        using (UnityWebRequest request = UnityWebRequest.Put($"{SERVER_URL}/room/{roomId}/elements/{element.id}", jsonData))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Room updatedRoom = JsonUtility.FromJson<Room>(request.downloadHandler.text);
                onSuccess?.Invoke(updatedRoom);
            }
        }
    }
}