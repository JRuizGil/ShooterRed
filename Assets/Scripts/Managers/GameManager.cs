using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    private string playerName = "";
    private string sessionCode = "";
    private bool isHost = false;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        Debug.Log("[GameManager] Initialized");
    }
    
    public void SetPlayerName(string name)
    {
        playerName = name;
        Debug.Log($"[GameManager] Player name set: {playerName}");
    }
    
    public string GetPlayerName()
    {
        return playerName;
    }
    
    public void SetSessionCode(string code)
    {
        sessionCode = code;
        Debug.Log($"[GameManager] Session code set: {sessionCode}");
    }
    
    public string GetSessionCode()
    {
        return sessionCode;
    }
    
    public void SetIsHost(bool host)
    {
        isHost = host;
        Debug.Log($"[GameManager] Is host: {isHost}");
    }
    
    public bool GetIsHost()
    {
        return isHost;
    }
    
    public void ResetLobbyData()
    {
        playerName = "";
        sessionCode = "";
        isHost = false;
        Debug.Log("[GameManager] Lobby data reset");
    }
}
