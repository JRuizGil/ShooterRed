using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestor central de UI.
/// Solo un canvas visible a la vez. Navega entre MainMenu, LobbyMenu,
/// SettingsMenu, ExitMenu y ControlsMenu.
/// </summary>
public class UiManager : MonoBehaviour
{
    public static UiManager Instance { get; private set; }

    [Header("Menus principales")]
    [SerializeField] private Canvas mainMenuCanvas;
    [SerializeField] private Canvas lobbyMenuCanvas;
    [SerializeField] private Canvas settingsMenuCanvas;
    [SerializeField] private Canvas exitMenuCanvas;
    [SerializeField] private Canvas controlsMenuCanvas;

    // Lista interna — se rellena automáticamente en Awake
    private List<Canvas> allCanvases = new List<Canvas>();

    // Historial de navegación para poder volver atrás
    private Stack<Canvas> navigationHistory = new Stack<Canvas>();
    private Canvas currentCanvas;

    // =========================================================

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Registrar todos los canvas
        RegisterCanvas(mainMenuCanvas);
        RegisterCanvas(lobbyMenuCanvas);
        RegisterCanvas(settingsMenuCanvas);
        RegisterCanvas(exitMenuCanvas);
        RegisterCanvas(controlsMenuCanvas);

        // Ocultar todos al arrancar
        HideAll();
    }

    private void Start()
    {
        // El juego arranca en el MainMenu
        ShowMainMenu();
    }

    // =========================================================
    // REGISTRO
    // =========================================================

    private void RegisterCanvas(Canvas c)
    {
        if (c != null && !allCanvases.Contains(c))
            allCanvases.Add(c);
    }

    // =========================================================
    // NÚCLEO — solo un canvas activo a la vez
    // =========================================================

    /// <summary>
    /// Muestra el canvas indicado y oculta todos los demás.
    /// Guarda el canvas anterior en el historial para poder volver.
    /// </summary>
    public void Show(Canvas target)
    {
        if (target == null)
        {
            Debug.LogWarning("[UiManager] Show() llamado con canvas null");
            return;
        }

        // Guardar en historial si hay un canvas activo
        if (currentCanvas != null && currentCanvas != target)
            navigationHistory.Push(currentCanvas);

        foreach (Canvas c in allCanvases)
            c.enabled = (c == target);

        currentCanvas = target;
        Debug.Log($"[UiManager] Mostrando: {target.name}");
    }

    /// <summary>
    /// Vuelve al canvas anterior (tipo "botón atrás").
    /// Si no hay historial, va al MainMenu.
    /// </summary>
    public void GoBack()
    {
        if (navigationHistory.Count > 0)
        {
            Canvas previous = navigationHistory.Pop();
            // No guardar en historial al volver
            foreach (Canvas c in allCanvases)
                c.enabled = (c == previous);
            currentCanvas = previous;
            Debug.Log($"[UiManager] Volviendo a: {previous.name}");
        }
        else
        {
            ShowMainMenu();
        }
    }

    /// <summary>
    /// Oculta todos los canvas.
    /// </summary>
    public void HideAll()
    {
        foreach (Canvas c in allCanvases)
            c.enabled = false;
        currentCanvas = null;
    }

    // =========================================================
    // NAVEGACIÓN — llamar desde botones de Unity
    // =========================================================

    public void ShowMainMenu()
    {
        navigationHistory.Clear(); // MainMenu es la raíz, limpiar historial
        Show(mainMenuCanvas);
    }

    public void ShowLobbyMenu()
    {
        Show(lobbyMenuCanvas);
    }

    public void ShowSettingsMenu()
    {
        Show(settingsMenuCanvas);
    }

    public void ShowExitMenu()
    {
        Show(exitMenuCanvas);
    }

    public void ShowControlsMenu()
    {
        Show(controlsMenuCanvas);
    }

    // =========================================================
    // ACCIONES ESPECIALES
    // =========================================================

    /// <summary>
    /// Confirmar salida del juego (llamar desde botón "Sí" en ExitMenu).
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[UiManager] Saliendo del juego...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Getter del canvas actual (por si otros sistemas lo necesitan).
    /// </summary>
    public Canvas GetCurrentCanvas() => currentCanvas;
}