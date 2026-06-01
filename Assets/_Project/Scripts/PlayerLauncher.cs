using UnityEngine;

public class PlayerLauncher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform visual;

    [Header("Launch Settings")]
    [SerializeField] private float maxDragDistance = 2.5f;
    [SerializeField] private float launchPower = 12f;

    [Header("Attach Settings")]
    [SerializeField] private float attachedGravityScale = 0f;
    [SerializeField] private float flyingGravityScale = 1.5f;
    [SerializeField] private float attachCheckRadius = 0.35f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask boostLayer;

    [Header("Stretch Settings")]
    [SerializeField] private float maxStretchY = 1.7f;
    [SerializeField] private float minStretchX = 0.75f;
    [SerializeField] private float stretchReturnSpeed = 12f;

    private bool isAttached = true;
    private bool isDragging = false;

    private Vector2 dragStartWorld;
    private Vector2 currentDragWorld;

    private float attachX;
    private float nextLaunchMultiplier = 1f;

    private Vector3 baseVisualScale;

    private bool inputEnabled = true;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (visual == null)
        {
            Transform foundVisual = transform.Find("Visual");

            if (foundVisual != null)
            {
                visual = foundVisual;
            }
        }

        if (visual != null)
        {
            baseVisualScale = visual.localScale;
        }

        attachX = transform.position.x;

        AttachToPoleWithoutChecks();
    }

    private void Update()
    {
        if (!inputEnabled)
        {
            ReturnVisualToNormal();
            return;
        }

        if (isAttached)
        {
            HandleDragLaunch();
        }
        else
        {
            HandleAirAttach();
            ReturnVisualToNormal();
        }
    }

    private void HandleDragLaunch()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragStartWorld = GetMouseWorldPosition();
            currentDragWorld = dragStartWorld;
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            currentDragWorld = GetMouseWorldPosition();

            float dragPower01 = GetDragPower01();
            UpdateVisualStretch(dragPower01);
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            currentDragWorld = GetMouseWorldPosition();
            isDragging = false;

            float dragDistance = GetDragDistance();
            Launch(dragDistance);
        }

        if (!isDragging)
        {
            ReturnVisualToNormal();
        }
    }

    private void HandleAirAttach()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryAttachToPole();
        }
    }

    private void Launch(float dragDistance)
    {
        if (dragDistance <= 0.05f)
        {
            return;
        }

        isAttached = false;

        rb.gravityScale = flyingGravityScale;
        rb.velocity = Vector2.zero;

        float force = dragDistance / maxDragDistance * launchPower * nextLaunchMultiplier;
        rb.velocity = new Vector2(0f, force);

        nextLaunchMultiplier = 1f;
    }

    private void TryAttachToPole()
    {
        Vector2 attachPoint = new Vector2(attachX, transform.position.y);

        bool hitObstacle = Physics2D.OverlapCircle(
            attachPoint,
            attachCheckRadius,
            obstacleLayer
        );

        if (hitObstacle)
        {
            return;
        }

        Collider2D boostCollider = Physics2D.OverlapCircle(
            attachPoint,
            attachCheckRadius,
            boostLayer
        );

        if (boostCollider != null)
        {
            BoostZone boostZone = boostCollider.GetComponent<BoostZone>();

            if (boostZone != null)
            {
                nextLaunchMultiplier = boostZone.LaunchMultiplier;
                boostZone.Collect();
            }
        }

        AttachToPoleWithoutChecks();
    }

    private void AttachToPoleWithoutChecks()
    {
        isAttached = true;

        rb.velocity = Vector2.zero;
        rb.gravityScale = attachedGravityScale;

        transform.position = new Vector3(attachX, transform.position.y, transform.position.z);

        ReturnVisualToNormalInstantly();
    }

    private float GetDragDistance()
    {
        float dragDownDistance = Mathf.Max(0f, dragStartWorld.y - currentDragWorld.y);
        return Mathf.Clamp(dragDownDistance, 0f, maxDragDistance);
    }

    private float GetDragPower01()
    {
        float dragDistance = GetDragDistance();
        return dragDistance / maxDragDistance;
    }

    private void UpdateVisualStretch(float power01)
    {
        if (visual == null)
        {
            return;
        }

        float targetScaleY = Mathf.Lerp(baseVisualScale.y, baseVisualScale.y * maxStretchY, power01);
        float targetScaleX = Mathf.Lerp(baseVisualScale.x, baseVisualScale.x * minStretchX, power01);

        visual.localScale = new Vector3(
            targetScaleX,
            targetScaleY,
            baseVisualScale.z
        );
    }

    private void ReturnVisualToNormal()
    {
        if (visual == null)
        {
            return;
        }

        visual.localScale = Vector3.Lerp(
            visual.localScale,
            baseVisualScale,
            stretchReturnSpeed * Time.deltaTime
        );
    }

    private void ReturnVisualToNormalInstantly()
    {
        if (visual == null)
        {
            return;
        }

        visual.localScale = baseVisualScale;
    }

    private Vector2 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPosition = Input.mousePosition;
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);

        return new Vector2(mouseWorldPosition.x, mouseWorldPosition.y);
    }

    private void OnDrawGizmosSelected()
    {
        float checkX = Application.isPlaying ? attachX : transform.position.x;
        Vector3 checkPosition = new Vector3(checkX, transform.position.y, transform.position.z);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(checkPosition, attachCheckRadius);
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (!inputEnabled)
        {
            isDragging = false;
            ReturnVisualToNormalInstantly();
        }
    }
}