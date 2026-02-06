using UnityEngine;

public class AudioListenerFix : MonoBehaviour
{
    void Start()
    {
        // Tìm tất cả Audio Listener trong scene
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);

        // Nếu có nhiều hơn 1
        if (listeners.Length > 1)
        {
            Debug.LogWarning($"Tìm thấy {listeners.Length} AudioListeners. Đang tắt bớt...");

            // Giữ lại cái đầu tiên, tắt hết mấy cái sau
            for (int i = 1; i < listeners.Length; i++)
            {
                Destroy(listeners[i]); // Hoặc listeners[i].enabled = false;
            }
        }
    }
}