using UnityEngine;
using TMPro;

public class BackgroundGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer backgroundPrefab;
    [SerializeField] private GameManager gameManager;

    [Header("Generation Settings")]
    [SerializeField] private int piecesCount = 20;
    [SerializeField] private float startY = 0f;
    [SerializeField] private float xPosition = 0f;
    [SerializeField] private float zPosition = 0f;

    [Header("Sorting Settings")]
    [SerializeField] private int backgroundSortingOrder = -1000;

    [Header("Parallax Settings")]
    [SerializeField] private bool enableVerticalParallax = true;
    [SerializeField] private Transform parallaxCamera;
    [SerializeField] private float verticalParallaxFollow = 0.5f;

    [Header("Seam Fix")]
    [SerializeField] private float overlap = 0.01f;

    [Header("Score Markers")]
    [SerializeField] private bool generateScoreMarkers = true;
    [SerializeField] private int markerScoreStep = 10;
    [SerializeField] private int maxMarkerScore = 300;
    [SerializeField] private float fallbackMaxHeight = 450f;
    [SerializeField] private float fallbackMaxScore = 300f;
    [SerializeField] private float fallbackStartY = 0f;
    [SerializeField] private float markerLineStartX = 2.5f;
    [SerializeField] private float markerLineEndX = 3f;
    [SerializeField] private float markerTextX = 3.15f;
    [SerializeField] private float markerZ = 0f;
    [SerializeField] private float markerLineWidth = 0.04f;
    [SerializeField] private float markerFontSize = 1f;
    [SerializeField] private TMP_FontAsset markerFontAsset;
    [SerializeField] private Vector2 markerTextBoxSize = new Vector2(2f, 1f);
    [SerializeField] private Color markerColor = Color.white;
    [SerializeField] private int markerSortingOrder = 20;

    private const string BackgroundPiecesParentName = "BackgroundPieces";
    private const string ScoreMarkersParentName = "ScoreMarkers";

    private Material markerLineMaterial;
    private Transform backgroundPiecesParent;
    private Vector3 backgroundBasePosition;
    private float cameraStartY;
    private bool parallaxInitialized;

    public Transform BackgroundPiecesParent => backgroundPiecesParent;

    private void Start()
    {
        EnsureBackgroundPiecesParent();
        MoveExistingBackgroundPiecesToParent();
        GenerateBackground();
        ApplyBackgroundSorting();
        GenerateScoreMarkers();
        InitializeParallax();
    }

    private void LateUpdate()
    {
        UpdateParallax();
    }

    private void GenerateBackground()
    {
        if (backgroundPrefab == null)
        {
            Debug.LogWarning("Background prefab is not assigned.");
            return;
        }

        float pieceHeight = backgroundPrefab.bounds.size.y;

        for (int i = 0; i < piecesCount; i++)
        {
            float yPosition = startY + i * (pieceHeight - overlap);

            SpriteRenderer piece = Instantiate(
                backgroundPrefab,
                new Vector3(xPosition, yPosition, zPosition),
                Quaternion.identity,
                backgroundPiecesParent
            );

            piece.name = $"BackgroundPiece_{i}";
            piece.sortingOrder = backgroundSortingOrder;
        }
    }

    private void ApplyBackgroundSorting()
    {
        if (backgroundPiecesParent == null)
        {
            return;
        }

        SpriteRenderer[] renderers = backgroundPiecesParent.GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].sortingOrder = backgroundSortingOrder;
            }
        }
    }

    private void EnsureBackgroundPiecesParent()
    {
        backgroundPiecesParent = transform.Find(BackgroundPiecesParentName);

        if (backgroundPiecesParent != null)
        {
            return;
        }

        GameObject parentObject = new GameObject(BackgroundPiecesParentName);
        parentObject.transform.SetParent(transform, false);
        backgroundPiecesParent = parentObject.transform;
    }

    private void MoveExistingBackgroundPiecesToParent()
    {
        if (backgroundPiecesParent == null)
        {
            return;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            if (child == backgroundPiecesParent || child.name == ScoreMarkersParentName)
            {
                continue;
            }

            if (child.GetComponent<GroundStretchToBackground>() != null)
            {
                continue;
            }

            if (child.GetComponentInChildren<SpriteRenderer>(true) == null)
            {
                continue;
            }

            child.SetParent(backgroundPiecesParent, true);
        }
    }

    private void InitializeParallax()
    {
        if (!enableVerticalParallax || backgroundPiecesParent == null)
        {
            return;
        }

        if (parallaxCamera == null && Camera.main != null)
        {
            parallaxCamera = Camera.main.transform;
        }

        if (parallaxCamera == null)
        {
            return;
        }

        cameraStartY = parallaxCamera.position.y;
        backgroundBasePosition = backgroundPiecesParent.position;
        parallaxInitialized = true;
    }

    private void UpdateParallax()
    {
        if (!enableVerticalParallax)
        {
            return;
        }

        if (!parallaxInitialized)
        {
            InitializeParallax();
        }

        if (!parallaxInitialized)
        {
            return;
        }

        float cameraDeltaY = parallaxCamera.position.y - cameraStartY;
        Vector3 targetPosition = backgroundBasePosition;
        targetPosition.y += cameraDeltaY * verticalParallaxFollow;
        backgroundPiecesParent.position = targetPosition;
    }

    private void GenerateScoreMarkers()
    {
        if (!generateScoreMarkers)
        {
            return;
        }

        if (markerScoreStep <= 0 || maxMarkerScore <= 0)
        {
            Debug.LogWarning("Score marker settings are invalid.");
            return;
        }

        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        float gameplayMaxScore = gameManager != null ? gameManager.MaxScore : fallbackMaxScore;
        float gameplayMaxHeight = gameManager != null ? gameManager.MaxHeight : fallbackMaxHeight;
        float startWorldY = gameManager != null ? gameManager.StartPlayerY : fallbackStartY;

        if (gameplayMaxScore <= 0f)
        {
            Debug.LogWarning("Score markers cannot be generated with max score <= 0.");
            return;
        }

        Transform markersParent = GetOrCreateScoreMarkersParent();
        ClearChildren(markersParent);

        int lastScore = Mathf.Min(maxMarkerScore, Mathf.FloorToInt(gameplayMaxScore));

        for (int score = markerScoreStep; score <= lastScore; score += markerScoreStep)
        {
            float markerY = gameManager != null
                ? gameManager.GetWorldYForScore(score)
                : startWorldY + score / gameplayMaxScore * gameplayMaxHeight;

            CreateMarkerLine(markersParent, score, markerY);
            CreateMarkerText(markersParent, score, markerY);
        }
    }

    private Transform GetOrCreateScoreMarkersParent()
    {
        Transform markersParent = transform.Find(ScoreMarkersParentName);

        if (markersParent != null)
        {
            return markersParent;
        }

        GameObject markersObject = new GameObject(ScoreMarkersParentName);
        markersObject.transform.SetParent(transform, false);

        return markersObject.transform;
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    private void CreateMarkerLine(Transform parent, int score, float markerY)
    {
        GameObject lineObject = new GameObject($"ScoreMarkerLine_{score}");
        lineObject.transform.SetParent(parent, false);
        lineObject.transform.position = new Vector3(0f, markerY, markerZ);

        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, new Vector3(markerLineStartX, 0f, 0f));
        lineRenderer.SetPosition(1, new Vector3(markerLineEndX, 0f, 0f));
        lineRenderer.startWidth = markerLineWidth;
        lineRenderer.endWidth = markerLineWidth;
        lineRenderer.startColor = markerColor;
        lineRenderer.endColor = markerColor;
        lineRenderer.sortingOrder = markerSortingOrder;

        Material material = GetMarkerLineMaterial();

        if (material != null)
        {
            lineRenderer.material = material;
        }
    }

    private void CreateMarkerText(Transform parent, int score, float markerY)
    {
        GameObject textObject = new GameObject($"ScoreMarkerText_{score}");
        textObject.transform.SetParent(parent, false);
        textObject.transform.position = new Vector3(markerTextX, markerY, markerZ);

        TextMeshPro markerText = textObject.AddComponent<TextMeshPro>();
        markerText.text = score.ToString();
        markerText.fontSize = markerFontSize;
        markerText.alignment = TextAlignmentOptions.MidlineLeft;
        markerText.color = markerColor;
        markerText.rectTransform.sizeDelta = markerTextBoxSize;

        if (markerFontAsset != null)
        {
            markerText.font = markerFontAsset;
        }

        Renderer textRenderer = markerText.GetComponent<Renderer>();

        if (textRenderer != null)
        {
            textRenderer.sortingOrder = markerSortingOrder;
        }
    }

    private Material GetMarkerLineMaterial()
    {
        if (markerLineMaterial != null)
        {
            return markerLineMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            return null;
        }

        markerLineMaterial = new Material(shader);
        markerLineMaterial.color = markerColor;

        return markerLineMaterial;
    }
}
