using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(SpriteRenderer))]
public class GroundStretchToBackground : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private LevelGenerator levelGenerator;
    [SerializeField] private BackgroundGenerator backgroundGenerator;
    [SerializeField] private Transform movingAnchor;
    [SerializeField] private float minHeight = 0.1f;

    private float spriteLocalMaxY;
    private float spawnPointLocalY;
    private float fixedSpawnWorldY;
    private float topAnchorOffsetY;
    private bool initialized;

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnValidate()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void LateUpdate()
    {
        if (!initialized)
        {
            Initialize();
        }

        if (!initialized)
        {
            return;
        }

        StretchBetweenBottomAndAnchor();
    }

    public void Reinitialize()
    {
        initialized = false;
    }

    private void Initialize()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        if (levelGenerator == null)
        {
            levelGenerator = FindObjectOfType<LevelGenerator>();
        }

        if (backgroundGenerator == null)
        {
            backgroundGenerator = GetComponentInParent<BackgroundGenerator>();
        }

        if (backgroundGenerator == null)
        {
            backgroundGenerator = FindObjectOfType<BackgroundGenerator>();
        }

        if (movingAnchor == null)
        {
            movingAnchor = backgroundGenerator != null && backgroundGenerator.BackgroundPiecesParent != null
                ? backgroundGenerator.BackgroundPiecesParent
                : transform.parent;
        }

        if (movingAnchor == null)
        {
            return;
        }

        Bounds spriteBounds = spriteRenderer.sprite.bounds;
        spriteLocalMaxY = spriteBounds.max.y;
        fixedSpawnWorldY = levelGenerator != null
            ? levelGenerator.StartWorldY
            : transform.position.y;

        spawnPointLocalY = transform.InverseTransformPoint(
            new Vector3(transform.position.x, fixedSpawnWorldY, transform.position.z)
        ).y;

        if (Mathf.Approximately(spriteLocalMaxY, spawnPointLocalY))
        {
            return;
        }

        float initialTopWorldY = GetWorldYForLocalSpriteY(spriteLocalMaxY);
        topAnchorOffsetY = initialTopWorldY - movingAnchor.position.y;
        initialized = true;
    }

    private void StretchBetweenBottomAndAnchor()
    {
        float targetTopWorldY = movingAnchor.position.y + topAnchorOffsetY;
        float targetTopDistance = Mathf.Max(minHeight, targetTopWorldY - fixedSpawnWorldY);
        float localTopDistance = spriteLocalMaxY - spawnPointLocalY;
        float parentScaleY = transform.parent != null ? transform.parent.lossyScale.y : 1f;

        if (Mathf.Approximately(parentScaleY, 0f) || Mathf.Approximately(localTopDistance, 0f))
        {
            return;
        }

        Vector3 scale = transform.localScale;
        scale.y = targetTopDistance / (localTopDistance * Mathf.Abs(parentScaleY));
        transform.localScale = scale;

        Vector3 position = transform.localPosition;
        position.y = GetParentLocalY(fixedSpawnWorldY) - spawnPointLocalY * transform.localScale.y;
        transform.localPosition = position;
    }

    private float GetWorldYForLocalSpriteY(float localSpriteY)
    {
        Vector3 localPoint = new Vector3(0f, localSpriteY, 0f);
        return transform.TransformPoint(localPoint).y;
    }

    private float GetParentLocalY(float worldY)
    {
        if (transform.parent == null)
        {
            return worldY;
        }

        Vector3 worldPoint = new Vector3(transform.position.x, worldY, transform.position.z);
        return transform.parent.InverseTransformPoint(worldPoint).y;
    }
}
