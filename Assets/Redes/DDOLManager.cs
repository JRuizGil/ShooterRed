using Fusion;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DDOLManager : MonoBehaviour
{
    public NetworkRunner runner;
    public string sceneToLoad = "MainMenu";
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.LoadScene(sceneToLoad);
    }
}
