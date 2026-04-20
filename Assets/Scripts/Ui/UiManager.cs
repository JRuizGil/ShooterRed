using System.Collections.Generic;
using UnityEngine;

public class UiManager : MonoBehaviour
{
    
    public List<Canvas> CanvasList = new List<Canvas>();
    void Awake()
    {
        foreach (Canvas canvas in CanvasList)
        {
            canvas.enabled = false;
        }
    }
    void Start()
    {
        CanvasList[0].enabled = true;
    }
    void OnEnable()
    {
                
    }
}

