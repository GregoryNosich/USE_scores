using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerLauncher playerLauncher;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text heightText;
    [SerializeField] private TMP_Text jumpsText;
    [SerializeField] private GameObject tutorialObject;

    [Header("End Screen")]
    [SerializeField] private GameObject endScreenRoot;
    [SerializeField] private TMP_Text endScreenText;
    [SerializeField] private Button endScreenButton;

    [Header("Timer Settings")]
    [SerializeField] private float timeLimit = 15f;
    [SerializeField] private float lowTimeWarningThreshold = 3f;
    [SerializeField] private float maxHeight = 80f;
    [SerializeField] private float maxScore = 300f;

    private float timeLeft;
    private float startPlayerY;
    private Color timerTextBaseColor;
    private Color jumpsTextBaseColor;
    private Coroutine jumpsTextFlashRoutine;

    private bool isTimerStarted = false;
    private bool isGameOver = false;

    public bool IsTimerStarted => isTimerStarted;
    public bool IsGameOver => isGameOver;
    public float MaxHeight => maxHeight;
    public float MaxScore => maxScore;
    public float StartPlayerY => startPlayerY;

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
            playerLauncher.OnFirstAttachAfterLaunch += HideTutorial;
            playerLauncher.OnJumpAttemptWithoutJumps += FlashJumpsTextRed;
            playerLauncher.OnJumpsRemainingChanged += UpdateJumpsText;
            UpdateJumpsText(playerLauncher.JumpsRemaining);
        }

        if (tutorialObject == null)
        {
            tutorialObject = GameObject.Find("Tutorial");
        }

        if (timerText != null)
        {
            timerTextBaseColor = timerText.color;
        }

        if (jumpsText != null)
        {
            jumpsTextBaseColor = jumpsText.color;
        }

        UpdateTimerText();
        UpdateHeight();
        SetRunTextVisible(false);
        EnsureEndScreen();
        SetEndScreenVisible(false);
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
        SetRunTextVisible(true);
        UpdateTimerText();
        UpdateHeight();

        if (playerLauncher != null)
        {
            UpdateJumpsText(playerLauncher.JumpsRemaining);
        }
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
            timerText.color = timeLeft <= lowTimeWarningThreshold ? Color.red : timerTextBaseColor;
        }
    }

    private float GetScore(float height)
    {
        return (height / maxHeight) * maxScore;
    }

    private float GetCurrentScore()
    {
        if (player == null)
        {
            return 0f;
        }

        float height = Mathf.Max(0f, player.position.y - startPlayerY);
        return GetScore(height);
    }

    public float GetWorldYForScore(float score)
    {
        if (maxScore <= 0f)
        {
            return startPlayerY;
        }

        return startPlayerY + score / maxScore * maxHeight;
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

    private void SetRunTextVisible(bool visible)
    {
        if (timerText != null)
        {
            timerText.gameObject.SetActive(visible);
        }

        if (heightText != null)
        {
            heightText.gameObject.SetActive(visible);
        }

        if (jumpsText != null)
        {
            jumpsText.gameObject.SetActive(visible);
        }
    }

    private void HideTutorial()
    {
        if (tutorialObject != null)
        {
            tutorialObject.SetActive(false);
        }
    }

    private void FlashJumpsTextRed()
    {
        if (jumpsText == null)
        {
            return;
        }

        if (jumpsTextFlashRoutine != null)
        {
            StopCoroutine(jumpsTextFlashRoutine);
        }

        jumpsTextFlashRoutine = StartCoroutine(FlashJumpsTextRedRoutine());
    }

    private IEnumerator FlashJumpsTextRedRoutine()
    {
        jumpsText.color = Color.red;

        yield return new WaitForSeconds(0.5f);

        if (jumpsText != null)
        {
            jumpsText.color = jumpsTextBaseColor;
        }

        jumpsTextFlashRoutine = null;
    }

    private void EndGame()
    {
        isGameOver = true;

        if (playerLauncher != null)
        {
            playerLauncher.FreezePlayer();
        }

        ShowEndScreen();
    }

    private void EnsureEndScreen()
    {
        if (endScreenRoot != null && endScreenText != null && endScreenButton != null)
        {
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning("End screen cannot be created because Canvas was not found.");
            return;
        }

        endScreenRoot = new GameObject("EndScreen", typeof(RectTransform));
        endScreenRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = endScreenRoot.GetComponent<RectTransform>();
        StretchToParent(rootRect);

        GameObject panelObject = new GameObject("EndScreenDimPanel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(endScreenRoot.transform, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        StretchToParent(panelRect);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject textObject = new GameObject("EndScreenText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(endScreenRoot.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        StretchToParent(textRect);
        textRect.offsetMin = new Vector2(24f, 24f);
        textRect.offsetMax = new Vector2(-24f, -24f);

        endScreenText = textObject.GetComponent<TextMeshProUGUI>();
        endScreenText.alignment = TextAlignmentOptions.Center;
        endScreenText.fontSize = 52f;
        endScreenText.color = Color.white;

        if (heightText != null)
        {
            endScreenText.font = heightText.font;
            endScreenText.fontSharedMaterial = heightText.fontSharedMaterial;
        }

        GameObject buttonObject = new GameObject("EndScreenFullscreenButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(endScreenRoot.transform, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        StretchToParent(buttonRect);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = Color.clear;

        endScreenButton = buttonObject.GetComponent<Button>();
        endScreenButton.targetGraphic = buttonImage;
        endScreenButton.onClick.AddListener(RestartScene);
    }

    private void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void ShowEndScreen()
    {
        EnsureEndScreen();

        if (endScreenText != null)
        {
            endScreenText.text = $"Игра окончена!\nРезультат: {GetCurrentScore():F1}";
        }

        SetEndScreenVisible(true);
    }

    private void SetEndScreenVisible(bool visible)
    {
        if (endScreenRoot != null)
        {
            endScreenRoot.SetActive(visible);
        }
    }

    private void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDestroy()
    {
        if (playerLauncher != null)
        {
            playerLauncher.OnFirstLaunch -= StartTimer;
            playerLauncher.OnFirstAttachAfterLaunch -= HideTutorial;
            playerLauncher.OnJumpAttemptWithoutJumps -= FlashJumpsTextRed;
            playerLauncher.OnJumpsRemainingChanged -= UpdateJumpsText;
        }
    }
}
