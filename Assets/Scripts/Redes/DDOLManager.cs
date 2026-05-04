using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DDOLManager : MonoBehaviour
{
    public static DDOLManager Instance { get; private set; } // Agregado para acceso global
    public NetworkRunner runner; // Asegúrate de que el nombre coincida
    public string sceneToLoad = "MainMenu";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}