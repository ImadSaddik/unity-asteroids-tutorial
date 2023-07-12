using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int score { get; private set; }
    private int maxScore;
    public int lives { get; private set; }

    [SerializeField] private PlayerAgent player;
    [SerializeField] private ParticleSystem explosionEffect;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text maxScoreText;
    [SerializeField] private Text livesText;
    [SerializeField] private Text rewardText;

    private void Awake()
    {
        if (Instance != null) {
            DestroyImmediate(gameObject);
        } else {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        NewGame();
    }

    private void Update()
    {
        if (lives <= 0) {
            NewGame();
        }

        if (score > maxScore)
        {
            maxScore = score;
        }

        rewardText.text = "Reward: " + player.GetCumulativeReward().ToString("0.00");
        maxScoreText.text = "Max score: " + maxScore.ToString();
    }

    private void NewGame()
    {
        Asteroid[] asteroids = FindObjectsOfType<Asteroid>();
        player.gameObject.SetActive(true);

        for (int i = 0; i < asteroids.Length; i++) {
            Destroy(asteroids[i].gameObject);
        }

        SetScore(0);
        SetLives(3);
        Respawn();
    }

    private void SetScore(int score)
    {
        this.score = score;
        scoreText.text = "Score: " + score.ToString();
    }

    private void SetLives(int lives)
    {
        this.lives = lives;
        livesText.text = lives.ToString();
    }

    private void Respawn()
    {
        player.gameObject.transform.position = Vector3.zero;
        player.transform.position = Vector3.zero;
        Invoke(nameof(TurnOnCollisions), player.respawnInvulnerability);
    }

    private void TurnOnCollisions()
    {
        player.gameObject.layer = LayerMask.NameToLayer("Player");
    }

    public void OnAsteroidDestroyed(Asteroid asteroid)
    {
        explosionEffect.transform.position = asteroid.transform.position;
        explosionEffect.Play();

        if (asteroid.size < 0.7f) {
            SetScore(score + 100); // small asteroid
        } else if (asteroid.size < 1.4f) {
            SetScore(score + 50); // medium asteroid
        } else {
            SetScore(score + 25); // large asteroid
        }
        player.AddReward(0.0002f);
    }

    public void OnPlayerDeath()
    {
        explosionEffect.transform.position = player.transform.position;
        explosionEffect.Play();

        player.gameObject.transform.position = Vector3.one * 10000f;
        SetLives(lives - 1);

        if (lives <= 0) {
            player.AddReward(-1f);
            player.gameObject.SetActive(false);
            player.EndEpisode();
        } else {
            player.AddReward(-0.3f);
            TurnOffCollisions();
            Invoke(nameof(Respawn), player.respawnDelay);
        }
    }

    private void TurnOffCollisions()
    {
        player.gameObject.layer = LayerMask.NameToLayer("Ignore Collisions");
    }
}
