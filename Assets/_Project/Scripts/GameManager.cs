using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerLauncher playerLauncher;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text heightText;
    [SerializeField] private TMP_Text jumpsText;

    [Header("Timer Settings")]
    [SerializeField] private float timeLimit = 15f;
    [SerializeField] private float maxHeight = 80f;
    [SerializeField] private float maxScore = 300f;

    private float timeLeft;
    private float startPlayerY;

    private bool isTimerStarted = false;
    private bool isGameOver = false;

    public bool IsTimerStarted => isTimerStarted;
    public bool IsGameOver => isGameOver;

    private void Awake()
    {
        timeLeft = timeLimit;

        if (player != null)
        {
            startPlayerY = player.position.y;
        }

        if (playerLauncher != null)
        {
            playerLauncher.SetInputEnabled(true);
            playerLauncher.OnFirstLaunch += StartTimer;
            playerLauncher.OnJumpsRemainingChanged += UpdateJumpsText;
            UpdateJumpsText(playerLauncher.JumpsRemaining);
        }

        UpdateTimerText();
        UpdateHeight();
    }

    private void Update()
    {
        if (isGameOver)
        {
            return;
        }

        if (isTimerStarted)
        {
            UpdateTimer();
        }

        UpdateHeight();
    }

    private void StartTimer()
    {
        if (isTimerStarted)
        {
            return;
        }

        isTimerStarted = true;
    }

    private void UpdateTimer()
    {
        timeLeft -= Time.deltaTime;

        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            EndGame();
        }

        UpdateTimerText();
    }

    private void UpdateTimerText()
    {
        if (timerText != null)
        {
            timerText.text = $"Осталось {timeLeft:F1} секунд";
        }
    }

    private float GetScore(float height)
    {
        return (height / maxHeight) * maxScore;
    }

    private void UpdateHeight()
    {
        if (player == null || heightText == null)
        {
            return;
        }

        float height = Mathf.Max(0f, player.position.y - startPlayerY);
        float score = GetScore(height);
        heightText.text = $"Результат: {score:F1}";
    }

    private void UpdateJumpsText(int jumpsRemaining)
    {
        if (jumpsText != null && playerLauncher != null)
        {
            jumpsText.text = $"Прыжки: {jumpsRemaining}/{playerLauncher.MaxJumps}";
        }
    }

    private void EndGame()
    {
        isGameOver = true;

        if (playerLauncher != null)
        {
            playerLauncher.FreezePlayer();
        }
    }

    private void OnDestroy()
    {
        if (playerLauncher != null)
        {
            playerLauncher.OnFirstLaunch -= StartTimer;
            playerLauncher.OnJumpsRemainingChanged -= UpdateJumpsText;
        }
    }
}
