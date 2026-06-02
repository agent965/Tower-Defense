using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Tower Sounds")]
    [SerializeField] private AudioClip towerShoot;
    [SerializeField] private AudioClip towerPlace;
    [SerializeField] private AudioClip towerSell;
    [SerializeField] private AudioClip towerUpgrade;

    [Header("Enemy Sounds")]
    [SerializeField] private AudioClip enemyHit;
    [SerializeField] private AudioClip enemyDeath;
    [SerializeField] private AudioClip enemyReachedEnd;

    [Header("Wave Sounds")]
    [SerializeField] private AudioClip waveStart;
    [SerializeField] private AudioClip waveComplete;
    [SerializeField] private AudioClip bossSpawn;

    [Header("UI Sounds")]
    [SerializeField] private AudioClip buttonClick;
    [SerializeField] private AudioClip purchaseFailed;

    [Header("Game Sounds")]
    [SerializeField] private AudioClip victory;
    [SerializeField] private AudioClip defeat;

    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume    = 1f;

    private AudioSource source;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
    }

    private void Play(AudioClip clip)
    {
        if (clip == null) return;
        source.PlayOneShot(clip, masterVolume * sfxVolume);
    }

    // ── Tower ──────────────────────────────────────────────────────────────
    public void PlayTowerShoot()   => Play(towerShoot);
    public void PlayTowerPlace()   => Play(towerPlace);
    public void PlayTowerSell()    => Play(towerSell);
    public void PlayTowerUpgrade() => Play(towerUpgrade);

    // ── Enemy ──────────────────────────────────────────────────────────────
    public void PlayEnemyHit()        => Play(enemyHit);
    public void PlayEnemyDeath()      => Play(enemyDeath);
    public void PlayEnemyReachedEnd() => Play(enemyReachedEnd);

    // ── Wave ───────────────────────────────────────────────────────────────
    public void PlayWaveStart()    => Play(waveStart);
    public void PlayWaveComplete() => Play(waveComplete);
    public void PlayBossSpawn()    => Play(bossSpawn);

    // ── UI ─────────────────────────────────────────────────────────────────
    public void PlayButtonClick()    => Play(buttonClick);
    public void PlayPurchaseFailed() => Play(purchaseFailed);

    // ── Game ───────────────────────────────────────────────────────────────
    public void PlayVictory() => Play(victory);
    public void PlayDefeat()  => Play(defeat);
}
