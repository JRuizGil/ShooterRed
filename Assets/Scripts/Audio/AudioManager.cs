using UnityEngine;
using Fusion;

/// <summary>
/// Sistema de audio centralizado
/// Gestiona sonidos de disparos, impactos y eventos del juego
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip[] gunShotClips;
    [SerializeField] private AudioClip[] impactClips;
    [SerializeField] private AudioClip grenadeExplosionClip;
    [SerializeField] private AudioClip airStrikeClip;
    [SerializeField] private AudioClip killStreakClip;
    [SerializeField] private AudioClip gameStartClip;
    [SerializeField] private AudioClip gameEndClip;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource masterAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioSource musicAudioSource;

    [Header("Volume Settings")]
    [SerializeField] private float masterVolume = 1f;
    [SerializeField] private float sfxVolume = 0.8f;
    [SerializeField] private float musicVolume = 0.5f;

    private static AudioManager instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);

        // Crear audio sources si no existen
        if (masterAudioSource == null)
        {
            GameObject masterGo = new GameObject("MasterAudioSource");
            masterGo.transform.SetParent(transform);
            masterAudioSource = masterGo.AddComponent<AudioSource>();
        }

        if (sfxAudioSource == null)
        {
            GameObject sfxGo = new GameObject("SFXAudioSource");
            sfxGo.transform.SetParent(transform);
            sfxAudioSource = sfxGo.AddComponent<AudioSource>();
        }

        if (musicAudioSource == null)
        {
            GameObject musicGo = new GameObject("MusicAudioSource");
            musicGo.transform.SetParent(transform);
            musicAudioSource = musicGo.AddComponent<AudioSource>();
        }

        UpdateVolumes();
    }

    /// <summary>
    /// Reproducir sonido de disparo
    /// </summary>
    public static void PlayGunShot(Vector3 position, string weaponType = "rifle")
    {
        if (instance == null || instance.gunShotClips.Length == 0)
            return;

        AudioClip clip = instance.gunShotClips[Random.Range(0, instance.gunShotClips.Length)];
        instance.PlaySoundAt(clip, position);
    }

    /// <summary>
    /// Reproducir sonido de impacto
    /// </summary>
    public static void PlayImpactSound(Vector3 position, string surfaceType = "metal")
    {
        if (instance == null || instance.impactClips.Length == 0)
            return;

        AudioClip clip = instance.impactClips[Random.Range(0, instance.impactClips.Length)];
        instance.PlaySoundAt(clip, position, 0.5f);
    }

    /// <summary>
    /// Reproducir sonido de explosión de granada
    /// </summary>
    public static void PlayGrenadeExplosion(Vector3 position)
    {
        if (instance == null || instance.grenadeExplosionClip == null)
            return;

        instance.PlaySoundAt(instance.grenadeExplosionClip, position, 1.5f);
    }

    /// <summary>
    /// Reproducir sonido de ataque aéreo
    /// </summary>
    public static void PlayAirStrike(Vector3 position)
    {
        if (instance == null || instance.airStrikeClip == null)
            return;

        instance.PlaySoundAt(instance.airStrikeClip, position, 1.5f);
    }

    /// <summary>
    /// Reproducir sonido de racha de bajas
    /// </summary>
    public static void PlayKillStreakAnnouncement(int streak)
    {
        if (instance == null || instance.killStreakClip == null)
            return;

        instance.sfxAudioSource.PlayOneShot(instance.killStreakClip, instance.sfxVolume);
    }

    /// <summary>
    /// Reproducir sonido de inicio de juego
    /// </summary>
    public static void PlayGameStart()
    {
        if (instance == null || instance.gameStartClip == null)
            return;

        instance.sfxAudioSource.PlayOneShot(instance.gameStartClip, instance.sfxVolume);
    }

    /// <summary>
    /// Reproducir sonido de fin de juego
    /// </summary>
    public static void PlayGameEnd()
    {
        if (instance == null || instance.gameEndClip == null)
            return;

        instance.sfxAudioSource.PlayOneShot(instance.gameEndClip, instance.sfxVolume);
    }

    /// <summary>
    /// Reproducir sonido en una posición específica
    /// </summary>
    private void PlaySoundAt(AudioClip clip, Vector3 position, float volumeMultiplier = 1f)
    {
        if (clip == null)
            return;

        // Crear temporary audio source
        GameObject tempGo = new GameObject("SoundInstance");
        tempGo.transform.position = position;

        AudioSource audioSource = tempGo.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = sfxVolume * volumeMultiplier;
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.Play();

        Destroy(tempGo, clip.length);
    }

    /// <summary>
    /// Actualizar volúmenes
    /// </summary>
    private void UpdateVolumes()
    {
        if (masterAudioSource != null)
            masterAudioSource.volume = masterVolume;

        if (sfxAudioSource != null)
            sfxAudioSource.volume = sfxVolume;

        if (musicAudioSource != null)
            musicAudioSource.volume = musicVolume;
    }

    /// <summary>
    /// Setear volumen maestro
    /// </summary>
    public static void SetMasterVolume(float volume)
    {
        if (instance != null)
        {
            instance.masterVolume = Mathf.Clamp01(volume);
            instance.UpdateVolumes();
        }
    }

    /// <summary>
    /// Setear volumen de SFX
    /// </summary>
    public static void SetSFXVolume(float volume)
    {
        if (instance != null)
        {
            instance.sfxVolume = Mathf.Clamp01(volume);
            instance.UpdateVolumes();
        }
    }

    /// <summary>
    /// Setear volumen de música
    /// </summary>
    public static void SetMusicVolume(float volume)
    {
        if (instance != null)
        {
            instance.musicVolume = Mathf.Clamp01(volume);
            instance.UpdateVolumes();
        }
    }

    public static AudioManager Instance => instance;
}
