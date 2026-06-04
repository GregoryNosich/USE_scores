using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Gameplay")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerLauncher playerLauncher;
    [SerializeField] private float timeLimit = 15f;
    [SerializeField] private float lowTimeWarningThreshold = 3f;
    [SerializeField] private float maxHeight = 600f;
    [SerializeField] private float maxScore = 300f;

    [Header("Run HUD")]
    [SerializeField] private TMP_Text minuteTensText;
    [SerializeField] private TMP_Text minuteOnesText;
    [SerializeField] private TMP_Text dotsText;
    [SerializeField] private TMP_Text secondTensText;
    [SerializeField] private TMP_Text secondOnesText;
    [SerializeField] private TMP_Text heightText;
    [SerializeField] private RectTransform timePanel;
    [SerializeField] private RectTransform scorePanel;
    [SerializeField] private float hudPanelAppearDuration = 0.45f;
    [SerializeField] private float hudPanelStartOffsetY = 120f;
    [SerializeField] private float hudPanelBounceDistance = 14f;

    [Header("Screens")]
    [SerializeField] private GameObject startScreenRoot;
    [SerializeField] private Button startScreenButton;
    [SerializeField] private GameObject endScreenRoot;
    [SerializeField] private TMP_Text endScreenResultText;
    [SerializeField] private GameObject endScreenWarnText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button exitButton;

    [Header("Tutorial Animation")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private Sprite[] dragTutorialFrames;
    [SerializeField] private Sprite[] tapTutorialFrames;
    [SerializeField] private float tutorialFrameRate = 12f;
    [SerializeField] private float tutorialImageHeight = 220f;
    [SerializeField] private Vector2 tutorialAnchoredPosition = new Vector2(-360f, 0f);
    [SerializeField] private int dragPullStartFrame = 4;
    [SerializeField] private float dragPullDownDistance = 120f;

    [Header("Audio")]
    [SerializeField] private AudioClip gameOverWarningClip;
    [Min(0f)]
    [SerializeField] private float gameOverWarningVolume = 0.8f;

    [Header("Music")]
    [SerializeField] private AudioClip musicClip;
    [Min(0f)]
    [SerializeField] private float musicVolume = 0.15f;

    private readonly TMP_Text[] timerParts = new TMP_Text[5];
    private readonly Color[] timerPartBaseColors = new Color[5];

    private float timeLeft;
    private float startPlayerY;

    private Image tutorialAnimationImage;
    private Sprite[] activeTutorialFrames;
    private float tutorialFrameTimer;
    private int tutorialFrameIndex;
    private Vector2 tutorialBaseAnchoredPosition;
    private bool isDragTutorialActive;

    private Vector2 timePanelBasePosition;
    private Vector2 scorePanelBasePosition;
    private float hudPanelAppearTimer;
    private bool isHudPanelAnimationActive;

    private bool isStartScreenVisible;
    private bool isTimerStarted;
    private bool isGameOver;
    private bool hasPlayedGameOverWarningSound;
    private AudioSource sfxAudioSource;

    public float MaxHeight => maxHeight;
    public float MaxScore => maxScore;
    public float StartPlayerY => startPlayerY;

    private void Awake()
    {
        timeLeft = timeLimit;
        startPlayerY = player != null ? player.position.y : 0f;

        SubscribePlayerEvents();
        CacheTimerParts();
        CacheHudPanelPositions();
        ConfigureButtons();

        UpdateTimerText();
        UpdateHeight();
        SetRunHudVisible(false);
        SetEndScreenVisible(false);
        EnsureMusic();
        CreateSfxAudioSource();

        if (startScreenRoot != null)
        {
            ShowStartScreen();
        }
        else
        {
            StartGame();
        }
    }

    private void Update()
    {
        if (isGameOver)
        {
            return;
        }

        if (isStartScreenVisible)
        {
            UpdateHeight();
            return;
        }

        if (isTimerStarted)
        {
            UpdateTimer();
        }

        UpdateHeight();
        UpdateRunHudPanelAnimation();
        UpdateTutorialAnimation();
    }

    private void SubscribePlayerEvents()
    {
        if (playerLauncher == null)
        {
            return;
        }

        playerLauncher.OnFirstLaunch += StartTimer;
        playerLauncher.OnFirstAttachAfterLaunch += HideTutorial;
        playerLauncher.OnObstacleZoneAttachAttempt += EndGameFromRedZone;
    }

    private void CacheTimerParts()
    {
        timerParts[0] = minuteTensText;
        timerParts[1] = minuteOnesText;
        timerParts[2] = dotsText;
        timerParts[3] = secondTensText;
        timerParts[4] = secondOnesText;

        for (int i = 0; i < timerParts.Length; i++)
        {
            timerPartBaseColors[i] = timerParts[i] != null ? timerParts[i].color : Color.white;
        }
    }

    private void CacheHudPanelPositions()
    {
        if (timePanel != null)
        {
            timePanelBasePosition = timePanel.anchoredPosition;
        }

        if (scorePanel != null)
        {
            scorePanelBasePosition = scorePanel.anchoredPosition;
        }
    }

    private void ConfigureButtons()
    {
        if (startScreenButton != null)
        {
            startScreenButton.onClick.RemoveListener(StartGame);
            startScreenButton.onClick.AddListener(StartGame);
        }

        if (restartButton != null)
        {
            restartButton.onClick = new Button.ButtonClickedEvent();
            restartButton.onClick.AddListener(RestartGame);
        }

        if (exitButton != null)
        {
            exitButton.onClick = new Button.ButtonClickedEvent();
        }
    }

    private void ShowStartScreen()
    {
        isStartScreenVisible = true;
        startScreenRoot.SetActive(true);
        HideTutorial();

        if (playerLauncher != null)
        {
            playerLauncher.SetInputEnabled(false);
        }
    }

    private void StartGame()
    {
        if (isGameOver)
        {
            return;
        }

        isStartScreenVisible = false;

        if (startScreenRoot != null)
        {
            startScreenRoot.SetActive(false);
        }

        if (playerLauncher != null)
        {
            playerLauncher.SetInputEnabled(true);
        }

        ShowDragTutorial();
    }

    private void StartTimer()
    {
        if (isTimerStarted)
        {
            return;
        }

        isTimerStarted = true;
        SetRunHudVisible(true);
        UpdateTimerText();
        UpdateHeight();
        ShowTapTutorial();
    }

    private void UpdateTimer()
    {
        timeLeft -= Time.deltaTime;

        if (timeLeft <= lowTimeWarningThreshold)
        {
            PlayGameOverWarningSound();
        }

        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            EndGame();
        }

        UpdateTimerText();
    }

    private void UpdateTimerText()
    {
        int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        bool isLowTime = timeLeft <= lowTimeWarningThreshold;

        SetTimerPart(0, (minutes / 10).ToString(), isLowTime);
        SetTimerPart(1, (minutes % 10).ToString(), isLowTime);
        SetTimerPart(2, ":", isLowTime);
        SetTimerPart(3, (seconds / 10).ToString(), isLowTime);
        SetTimerPart(4, (seconds % 10).ToString(), isLowTime);
    }

    private void SetTimerPart(int index, string value, bool isLowTime)
    {
        TMP_Text text = timerParts[index];

        if (text == null)
        {
            return;
        }

        text.text = value;
        text.color = isLowTime ? Color.red : timerPartBaseColors[index];
    }

    private void UpdateHeight()
    {
        if (player == null || heightText == null)
        {
            return;
        }

        heightText.text = Mathf.RoundToInt(GetCurrentScore()).ToString();
    }

    private float GetScore(float height)
    {
        if (maxHeight <= 0f)
        {
            return 0f;
        }

        return height / maxHeight * maxScore;
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

    private void SetRunHudVisible(bool visible)
    {
        SetTimerPartsVisible(visible);

        if (heightText != null)
        {
            heightText.gameObject.SetActive(visible);
        }

        SetRunHudPanelsVisible(visible, visible);
    }

    private void SetTimerPartsVisible(bool visible)
    {
        for (int i = 0; i < timerParts.Length; i++)
        {
            if (timerParts[i] != null)
            {
                timerParts[i].gameObject.SetActive(visible);
            }
        }
    }

    private void SetRunHudPanelsVisible(bool visible, bool animate)
    {
        isHudPanelAnimationActive = visible && animate;
        hudPanelAppearTimer = 0f;

        SetHudPanelVisible(timePanel, timePanelBasePosition, visible, animate);
        SetHudPanelVisible(scorePanel, scorePanelBasePosition, visible, animate);
    }

    private void SetHudPanelVisible(RectTransform panel, Vector2 basePosition, bool visible, bool animate)
    {
        if (panel == null)
        {
            return;
        }

        panel.gameObject.SetActive(visible);
        panel.anchoredPosition = visible && animate
            ? basePosition + Vector2.up * hudPanelStartOffsetY
            : basePosition;
    }

    private void UpdateRunHudPanelAnimation()
    {
        if (!isHudPanelAnimationActive)
        {
            return;
        }

        float duration = Mathf.Max(0.01f, hudPanelAppearDuration);
        hudPanelAppearTimer += Time.deltaTime;

        float progress = Mathf.Clamp01(hudPanelAppearTimer / duration);
        float offsetY = GetHudPanelAppearOffsetY(progress);

        ApplyHudPanelOffset(timePanel, timePanelBasePosition, offsetY);
        ApplyHudPanelOffset(scorePanel, scorePanelBasePosition, offsetY);

        if (progress >= 1f)
        {
            isHudPanelAnimationActive = false;
            ApplyHudPanelOffset(timePanel, timePanelBasePosition, 0f);
            ApplyHudPanelOffset(scorePanel, scorePanelBasePosition, 0f);
        }
    }

    private float GetHudPanelAppearOffsetY(float progress)
    {
        float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
        float offsetY = hudPanelStartOffsetY * (1f - easedProgress);

        if (progress > 0.55f)
        {
            float bounceProgress = (progress - 0.55f) / 0.45f;
            offsetY -= Mathf.Sin(bounceProgress * Mathf.PI) * hudPanelBounceDistance;
        }

        return offsetY;
    }

    private void ApplyHudPanelOffset(RectTransform panel, Vector2 basePosition, float offsetY)
    {
        if (panel != null)
        {
            panel.anchoredPosition = basePosition + Vector2.up * offsetY;
        }
    }

    private void ShowDragTutorial()
    {
        ShowTutorialAnimation(dragTutorialFrames, true);
    }

    private void ShowTapTutorial()
    {
        ShowTutorialAnimation(tapTutorialFrames, false);
    }

    private void ShowTutorialAnimation(Sprite[] frames, bool isDragAnimation)
    {
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        EnsureTutorialAnimationImage();

        if (tutorialAnimationImage == null)
        {
            return;
        }

        activeTutorialFrames = frames;
        isDragTutorialActive = isDragAnimation;
        tutorialFrameTimer = 0f;
        tutorialFrameIndex = 0;
        tutorialAnimationImage.sprite = activeTutorialFrames[tutorialFrameIndex];
        tutorialAnimationImage.gameObject.SetActive(true);
        FitTutorialAnimationImage();
        ApplyTutorialFrameOffset();
    }

    private void EnsureTutorialAnimationImage()
    {
        if (tutorialAnimationImage != null || uiCanvas == null)
        {
            return;
        }

        GameObject tutorialAnimationObject = new GameObject(
            "TutorialAnimation",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        tutorialAnimationObject.transform.SetParent(uiCanvas.transform, false);

        RectTransform rectTransform = tutorialAnimationObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 0.5f);
        rectTransform.anchorMax = new Vector2(1f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = tutorialAnchoredPosition;
        tutorialBaseAnchoredPosition = tutorialAnchoredPosition;

        tutorialAnimationImage = tutorialAnimationObject.GetComponent<Image>();
        tutorialAnimationImage.raycastTarget = false;
        tutorialAnimationImage.preserveAspect = true;
        tutorialAnimationImage.color = Color.white;
        tutorialAnimationObject.SetActive(false);
    }

    private void UpdateTutorialAnimation()
    {
        if (tutorialAnimationImage == null ||
            activeTutorialFrames == null ||
            activeTutorialFrames.Length <= 1 ||
            !tutorialAnimationImage.gameObject.activeSelf)
        {
            return;
        }

        float frameDuration = 1f / Mathf.Max(1f, tutorialFrameRate);
        tutorialFrameTimer += Time.deltaTime;

        while (tutorialFrameTimer >= frameDuration)
        {
            tutorialFrameTimer -= frameDuration;
            tutorialFrameIndex = (tutorialFrameIndex + 1) % activeTutorialFrames.Length;
            tutorialAnimationImage.sprite = activeTutorialFrames[tutorialFrameIndex];
            ApplyTutorialFrameOffset();
        }
    }

    private void ApplyTutorialFrameOffset()
    {
        if (tutorialAnimationImage == null)
        {
            return;
        }

        RectTransform rectTransform = tutorialAnimationImage.rectTransform;

        if (!isDragTutorialActive || activeTutorialFrames == null)
        {
            rectTransform.anchoredPosition = tutorialBaseAnchoredPosition;
            return;
        }

        int pullStartIndex = Mathf.Clamp(dragPullStartFrame, 1, activeTutorialFrames.Length) - 1;

        if (tutorialFrameIndex <= pullStartIndex)
        {
            rectTransform.anchoredPosition = tutorialBaseAnchoredPosition;
            return;
        }

        int pullFramesCount = Mathf.Max(1, activeTutorialFrames.Length - pullStartIndex - 1);
        float pullProgress = (tutorialFrameIndex - pullStartIndex) / (float)pullFramesCount;
        rectTransform.anchoredPosition = tutorialBaseAnchoredPosition +
            Vector2.down * dragPullDownDistance * pullProgress;
    }

    private void FitTutorialAnimationImage()
    {
        if (tutorialAnimationImage == null || tutorialAnimationImage.sprite == null)
        {
            return;
        }

        Rect spriteRect = tutorialAnimationImage.sprite.rect;

        if (spriteRect.height <= 0f)
        {
            return;
        }

        float width = tutorialImageHeight * spriteRect.width / spriteRect.height;
        tutorialAnimationImage.rectTransform.sizeDelta = new Vector2(width, tutorialImageHeight);
    }

    private void HideTutorial()
    {
        activeTutorialFrames = null;
        isDragTutorialActive = false;

        if (tutorialAnimationImage != null)
        {
            tutorialAnimationImage.gameObject.SetActive(false);
        }
    }

    private void EndGame()
    {
        EndGame(false);
    }

    private void EndGameFromRedZone()
    {
        EndGame(true);
    }

    private void EndGame(bool showRedZoneWarning)
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;

        if (playerLauncher != null)
        {
            playerLauncher.FreezePlayer();
        }

        HideTutorial();
        ShowEndScreen(showRedZoneWarning);
    }

    private void ShowEndScreen(bool showRedZoneWarning)
    {
        if (endScreenResultText != null)
        {
            endScreenResultText.text =
                $"{Mathf.RoundToInt(GetCurrentScore())} \u0438\u0437 {Mathf.RoundToInt(maxScore)}";
        }

        SetEndScreenWarningVisible(showRedZoneWarning);
        SetEndScreenVisible(true);
    }

    private void SetEndScreenVisible(bool visible)
    {
        if (endScreenRoot != null)
        {
            endScreenRoot.SetActive(visible);
        }

        if (!visible)
        {
            SetEndScreenWarningVisible(false);
        }
    }

    private void SetEndScreenWarningVisible(bool visible)
    {
        if (endScreenWarnText != null)
        {
            endScreenWarnText.SetActive(visible);
        }
    }

    private void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void CreateSfxAudioSource()
    {
        sfxAudioSource = gameObject.AddComponent<AudioSource>();

        sfxAudioSource.playOnAwake = false;
        sfxAudioSource.loop = false;
    }

    private void EnsureMusic()
    {
        if (musicClip != null)
        {
            MusicPlayer.Play(musicClip, musicVolume);
        }
    }

    private void PlayGameOverWarningSound()
    {
        if (hasPlayedGameOverWarningSound)
        {
            return;
        }

        hasPlayedGameOverWarningSound = true;
        PlayOneShotScaled(sfxAudioSource, gameOverWarningClip, gameOverWarningVolume);
    }

    private void PlayOneShotScaled(AudioSource source, AudioClip clip, float volume)
    {
        if (source == null || clip == null || volume <= 0f)
        {
            return;
        }

        float remainingVolume = volume;

        while (remainingVolume > 0f)
        {
            float layerVolume = Mathf.Min(remainingVolume, 1f);
            source.PlayOneShot(clip, layerVolume);
            remainingVolume -= layerVolume;
        }
    }

    private void OnDestroy()
    {
        if (startScreenButton != null)
        {
            startScreenButton.onClick.RemoveListener(StartGame);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
        }

        if (playerLauncher != null)
        {
            playerLauncher.OnFirstLaunch -= StartTimer;
            playerLauncher.OnFirstAttachAfterLaunch -= HideTutorial;
            playerLauncher.OnObstacleZoneAttachAttempt -= EndGameFromRedZone;
        }
    }
}
