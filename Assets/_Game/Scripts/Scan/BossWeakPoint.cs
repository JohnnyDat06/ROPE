using UnityEngine;

[RequireComponent(typeof(Outline))]
public class BossWeakPoint : MonoBehaviour
{
    private Outline outline;

    [Tooltip("Thời gian hiển thị Outline trước khi tự tắt")]
    public float highlightDuration = 3f;

    void Start()
    {
        outline = GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = false; 

            
            outline.OutlineColor = Color.red;
            outline.OutlineWidth = 3.6f;
        }
    }

    
    public void TriggerHighlight()
    {
        if (outline != null && !outline.enabled)
        {
            outline.enabled = true;

           
            CancelInvoke(nameof(TurnOffHighlight));
            Invoke(nameof(TurnOffHighlight), highlightDuration);
        }
    }

    private void TurnOffHighlight()
    {
        if (outline != null)
        {
            outline.enabled = false;
        }
    }
}