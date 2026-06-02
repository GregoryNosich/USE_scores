using UnityEngine;

public class BackgroundGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer backgroundPrefab;

    [Header("Generation Settings")]
    [SerializeField] private int piecesCount = 20;
    [SerializeField] private float startY = 0f;
    [SerializeField] private float xPosition = 0f;
    [SerializeField] private float zPosition = 5f;

    [Header("Seam Fix")]
    [SerializeField] private float overlap = 0.01f;

    private void Start()
    {
        GenerateBackground();
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
                transform
            );

            piece.name = $"BackgroundPiece_{i}";
        }
    }
}