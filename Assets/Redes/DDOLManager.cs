using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DDOLManager : MonoBehaviour
{
    public string sceneToLoad = "MainMenu";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.LoadScene(sceneToLoad);
    }
}
