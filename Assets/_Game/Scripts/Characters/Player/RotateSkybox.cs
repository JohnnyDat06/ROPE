using UnityEngine;

public class RotateSkybox : MonoBehaviour
{
    [Tooltip("Tốc độ xoay của bầu trời")]
    public float speed = 1.0f;

    void Update()
    {
        RenderSettings.skybox.SetFloat("_Rotation", Time.time * speed);
    }
}