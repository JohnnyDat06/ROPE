using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TutorialStep
{
    [TextArea(3, 10)] public string description; // Nội dung chữ
    public Sprite image; // Ảnh minh họa
}

[CreateAssetMenu(fileName = "NewTutorial", menuName = "Tutorial System/Tutorial Data")]
public class TutorialData : ScriptableObject
{
    public string tutorialID; // ID duy nhất (ví dụ: "Tut_Movement", "Tut_Attack") để lưu đã xem hay chưa
    public List<TutorialStep> steps; // Danh sách các trang hướng dẫn
}