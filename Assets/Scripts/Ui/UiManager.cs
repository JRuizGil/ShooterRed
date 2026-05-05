using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestor central de UI
/// Controla qué paneles están visibles en cada momento
/// </summary>
public class UiManager : MonoBehaviour
{
    [SerializeField] private List<Canvas> canvasList = new List<Canvas>();
    [SerializeField] private Canvas mainMenuCanvas;
    [SerializeField] private Canvas gameplayCanvas;
    [SerializeField] private Canvas pauseMenuCanvas;

    private int currentCanvasIndex = 0;

    private void Awake()
    {
        // Desactivar todos los canvas
        foreach (Canvas canvas in canvasList)
        {
            canvas.enabled = false;
        }
    }

    private void Start()
    {
        // Mostrar menú principal
        if (mainMenuCanvas != null)
        {
            ShowCanvas(mainMenuCanvas);
        }
        else if (canvasList.Count > 0)
        {
            ShowCanvas(canvasList[0]);
        }

        Debug.Log("[UiManager] Initialized");
    }

    /// <summary>
    /// Mostrar un canvas específico
    /// </summary>
    public void ShowCanvas(Canvas canvas)
    {
        foreach (Canvas c in canvasList)
        {
            c.enabled = (c == canvas);
        }
        Debug.Log($"[UiManager] Showing canvas: {canvas.name}");
    }

    /// <summary>
    /// Mostrar canvas por índice
    /// </summary>
    public void ShowCanvasByIndex(int index)
    {
        if (index >= 0 && index < canvasList.Count)
        {
            ShowCanvas(canvasList[index]);
            currentCanvasIndex = index;
        }
    }

    /// <summary>
    /// Mostrar el canvas de gameplay
    /// </summary>
    public void ShowGameplayUI()
    {
        if (gameplayCanvas != null)
            ShowCanvas(gameplayCanvas);
    }

    /// <summary>
    /// Mostrar pausa
    /// </summary>
    public void ShowPauseMenu()
    {
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.enabled = true;
    }

    /// <summary>
    /// Ocultar pausa
    /// </summary>
    public void HidePauseMenu()
    {
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.enabled = false;
    }

    /// <summary>
    /// Singleton
    /// </summary>
    public static UiManager Instance { get; private set; }

    private void OnEnable()
    {
        if (Instance == null)
            Instance = this;
    }
}


