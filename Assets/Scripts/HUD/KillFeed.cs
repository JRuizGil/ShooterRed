using Fusion;
using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Sistema de Kill Feed - muestra eventos de eliminaciones en tiempo real
/// Se sincroniza a través de la red
/// </summary>
public class KillFeed : NetworkBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private Canvas killFeedCanvas;
    [SerializeField] private GameObject killFeedEntryPrefab;
    [SerializeField] private Transform killFeedContainer;
    [SerializeField] private int maxEntriesOnScreen = 5;
    [SerializeField] private float entryDisplayDuration = 5f;

    // Cola de eventos de kill para mostrar
    private Queue<KillFeedEntry> feedEntries = new Queue<KillFeedEntry>();
    private List<KillFeedEntryUI> activeEntries = new List<KillFeedEntryUI>();

    private static KillFeed instance;

    public struct KillFeedEntry
    {
        public string killerName;
        public string victimName;
        public string causeOfDeath;
        public float spawnTime;
    }

    public override void Spawned()
    {
        instance = this;

        if (killFeedCanvas == null)
            killFeedCanvas = FindFirstObjectByType<Canvas>();

        if (killFeedContainer == null && killFeedCanvas != null)
        {
            GameObject containerGo = new GameObject("KillFeedContainer");
            containerGo.transform.SetParent(killFeedCanvas.transform, false);
            killFeedContainer = containerGo.transform;

            RectTransform rectTransform = containerGo.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.one;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(-300, -200);
            rectTransform.offsetMax = Vector2.zero;
        }

        Debug.Log("[KillFeed] Spawned");
    }

    public override void FixedUpdateNetwork()
    {
        // Actualizar duración de entradas
        float currentTime = Time.time;

        for (int i = activeEntries.Count - 1; i >= 0; i--)
        {
            KillFeedEntryUI entry = activeEntries[i];
            float elapsedTime = currentTime - entry.spawnTime;

            if (elapsedTime > entryDisplayDuration)
            {
                Destroy(entry.gameObject);
                activeEntries.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Agregar una entrada al kill feed (desde GameState o PlayerHealth)
    /// </summary>
    public void AddKillFeedEntry(string killerName, string victimName, string cause)
    {
        if (!HasStateAuthority)
            return;

        RPC_AddKillEntry(killerName, victimName, cause);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AddKillEntry(string killerName, string victimName, string cause)
    {
        // Crear entrada visual
        if (killFeedContainer != null && killFeedEntryPrefab != null)
        {
            GameObject entryGo = Instantiate(killFeedEntryPrefab, killFeedContainer);
            KillFeedEntryUI entryUI = entryGo.GetComponent<KillFeedEntryUI>();
            
            if (entryUI == null)
                entryUI = entryGo.AddComponent<KillFeedEntryUI>();

            entryUI.Initialize(killerName, victimName, cause, Time.time);
            activeEntries.Add(entryUI);

            // Limitar cantidad de entradas visibles
            while (activeEntries.Count > maxEntriesOnScreen)
            {
                if (activeEntries[0] != null)
                    Destroy(activeEntries[0].gameObject);
                activeEntries.RemoveAt(0);
            }
        }

        Debug.Log($"[KillFeed] {killerName} killed {victimName} with {cause}");
    }

    public static KillFeed Instance => instance;
}

/// <summary>
/// Componente UI para una entrada individual del kill feed
/// </summary>
public class KillFeedEntryUI : MonoBehaviour
{
    private TextMeshProUGUI textComponent;
    public float spawnTime { get; private set; }

    public void Initialize(string killerName, string victimName, string cause, float time)
    {
        spawnTime = time;

        // Crear o encontrar TextMeshProUGUI
        textComponent = GetComponent<TextMeshProUGUI>();
        if (textComponent == null)
        {
            textComponent = gameObject.AddComponent<TextMeshProUGUI>();
        }

        // Formato del mensaje
        string causeIcon = GetCauseIcon(cause);
        textComponent.text = $"<color=red>{killerName}</color> {causeIcon} <color=blue>{victimName}</color>";
        textComponent.fontSize = 36;
        textComponent.alignment = TextAlignmentOptions.TopRight;

        // Animar entrada
        StartCoroutine(AnimateEntry());
    }

    private string GetCauseIcon(string cause)
    {
        return cause switch
        {
            "Rifle" => "🔫",
            "Pistol" => "🔫",
            "Grenade" => "💣",
            "Air Strike" => "💥",
            "Turret" => "🎯",
            _ => "⚔️"
        };
    }

    private System.Collections.IEnumerator AnimateEntry()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Fade in
        canvasGroup.alpha = 0;
        float duration = 0.3f;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = 1;
    }
}
