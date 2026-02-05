using UnityEngine;
using UnityEditor; // Chỉ chạy trong Editor

public class BoneFixer : MonoBehaviour
{
    // Hướng dẫn:
    // 1. Gắn script này vào Player
    // 2. Kéo Mesh cánh tay vào ô "Mesh Can Suaa"
    // 3. Kéo Mesh thân người (đang chạy ngon) vào ô "Mesh Chuan"
    // 4. Chuột phải vào tên Script chọn "Fix Bones Now"

    public SkinnedMeshRenderer meshCanSua; // Mesh cánh tay (đang bị sai xương)
    public SkinnedMeshRenderer meshChuan;  // Mesh body (đang đúng xương)

    [ContextMenu("Fix Bones Now")] // Tạo nút bấm ở menu chuột phải
    void FixBones()
    {
        if (meshCanSua == null || meshChuan == null)
        {
            Debug.LogError("Chưa kéo đủ Mesh vào script!");
            return;
        }

        // 1. Copy xương Root
        meshCanSua.rootBone = meshChuan.rootBone;

        // 2. Tìm và gán lại từng đốt xương
        Transform[] bonesMoi = new Transform[meshCanSua.bones.Length];

        // Lấy danh sách xương chuẩn từ body
        Transform[] bonesChuan = meshChuan.bones;

        for (int i = 0; i < meshCanSua.bones.Length; i++)
        {
            string tenXuongCanTim = meshCanSua.bones[i].name;
            bool timThay = false;

            // Tìm trong list xương chuẩn xem có cái nào trùng tên không
            foreach (Transform bone in bonesChuan)
            {
                if (bone.name == tenXuongCanTim)
                {
                    bonesMoi[i] = bone; // Gán xương chuẩn vào
                    timThay = true;
                    break;
                }
            }

            if (!timThay) Debug.LogWarning("Không tìm thấy xương: " + tenXuongCanTim);
        }

        meshCanSua.bones = bonesMoi;
        Debug.Log("Đã nối xương xong! Hãy thử xóa root cũ.");
    }
}