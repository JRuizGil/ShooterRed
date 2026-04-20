using UnityEngine;

public class UiManager : MonoBehaviour
{
    public Canvas[] CanvasRenderers;
    void Awake()
    {
        CanvasRenderers = GetComponentsInChildren<Canvas>();
    }
    void Start()
    {
        
    }
    void OnEnable()
    {
        foreach (Canvas canvas in CanvasRenderers)
        {
            canvas.enabled = false;
        }
        CanvasRenderers[0].enabled = true;

    }
}

