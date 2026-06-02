using UnityEngine;
using UnityEngine.Rendering;
using System;

public class PlayerLauncher : MonoBehaviour
{
    public event Action OnFirstLaunch;
    public event Action OnFirstAttachAfterLaunch;
    public event Action OnJumpAttemptWithoutJumps;
    public event Action<int> OnJumpsRemainingChanged;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform visual;
    [SerializeField] private SpriteRenderer visualRenderer;

    [Header("Launch Settings")]
    [SerializeField] private int maxJumps = 5;
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
    [SerializeField] private float flyingVisualScaleMultiplier = 1.5f;

    [Header("Sprite Settings")]
    [SerializeField] private Sprite attachedSprite;
    [SerializeField] private Sprite flyingSprite;

    [Header("Sorting Settings")]
    [SerializeField] private int visualSortingOrder = 1000;

    private bool isAttached = true;
    private bool isDragging = false;
    private bool inputEnabled = true;
    private bool hasLaunchedOnce = false;
    private bool hasAttachedAfterFirstLaunch = false;
    private bool jumpCounterInitialized = false;

    private Vector2 dragStartWorld;
    private Vector2 currentDragWorld;

    private float attachX;
    private float nextLaunchMultiplier = 1f;
    private int jumpsRemaining;

    private Vector3 baseVisualScale;
    private Vector3 baseVisualLocalPosition;
    private float baseVisualTopLocalY;
    private float visualTopOffset = 0.5f;

    public int MaxJumps => maxJumps;
    public int JumpsRemaining => jumpCounterInitialized ? jumpsRemaining : Mathf.Max(0, maxJumps);
    public bool HasJumpsRemaining => JumpsRemaining > 0;


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

        if (visualRenderer == null && visual != null)
        {
            visualRenderer = visual.GetComponent<SpriteRenderer>();
        }

        ApplyVisualSortingOrder();

        if (attachedSprite == null && visualRenderer != null)
        {
            attachedSprite = visualRenderer.sprite;
        }

        ApplyAttachedSprite();

        if (visual != null)
        {
            baseVisualScale = visual.localScale;
            baseVisualLocalPosition = visual.localPosition;
            baseVisualTopLocalY = GetVisualTopLocalY(baseVisualScale, baseVisualLocalPosition);
        }

        attachX = transform.position.x;

        ResetJumpLimit();
        AttachToPoleWithoutChecks(true);
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
            ReturnVisualToFlyingScale();
        }
    }

    private void HandleDragLaunch()
    {
        if (!HasJumpsRemaining)
        {
            if (Input.GetMouseButtonDown(0))
            {
                OnJumpAttemptWithoutJumps?.Invoke();
            }

            ReturnVisualToNormal();
            return;
        }

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
        if (!HasJumpsRemaining)
        {
            return;
        }

        if (dragDistance <= 0.05f)
        {
            return;
        }

        SpendJump();

        if (!hasLaunchedOnce)
        {
            hasLaunchedOnce = true;
            OnFirstLaunch?.Invoke();
        }

        isAttached = false;
        ApplyFlyingSprite();

        rb.gravityScale = flyingGravityScale;
        rb.velocity = Vector2.zero;

        float force = dragDistance / maxDragDistance * launchPower * nextLaunchMultiplier;
        rb.velocity = new Vector2(0f, force);

        nextLaunchMultiplier = 1f;
    }

    private void SpendJump()
    {
        jumpsRemaining = Mathf.Max(0, jumpsRemaining - 1);
        OnJumpsRemainingChanged?.Invoke(jumpsRemaining);
    }

    private void TryAttachToPole()
    {
        Vector2 attachPoint = new Vector2(attachX, transform.position.y);

        Collider2D obstacleCollider = Physics2D.OverlapCircle(
            attachPoint,
            attachCheckRadius,
            obstacleLayer
        );

        if (obstacleCollider != null)
        {
            FlashAttachZone(obstacleCollider);
            return;
        }

        Collider2D boostCollider = Physics2D.OverlapCircle(
            attachPoint,
            attachCheckRadius,
            boostLayer
        );

        if (boostCollider != null)
        {
            FlashAttachZone(boostCollider);

            BoostZone boostZone = boostCollider.GetComponent<BoostZone>();

            if (boostZone != null)
            {
                nextLaunchMultiplier = boostZone.LaunchMultiplier;
                boostZone.Collect();
            }
        }

        AttachToPoleWithoutChecks();
        NotifyFirstAttachAfterLaunch();
    }

    private void FlashAttachZone(Collider2D zoneCollider)
    {
        GameObject feedbackTarget = GetAttachZoneFeedbackTarget(zoneCollider);
        AttachZoneFeedback feedback = feedbackTarget.GetComponent<AttachZoneFeedback>();

        if (feedback == null)
        {
            feedback = feedbackTarget.AddComponent<AttachZoneFeedback>();
        }

        feedback.Flash();
    }

    private GameObject GetAttachZoneFeedbackTarget(Collider2D zoneCollider)
    {
        BoostZone boostZone = zoneCollider.GetComponentInParent<BoostZone>();

        if (boostZone != null)
        {
            return boostZone.gameObject;
        }

        Transform current = zoneCollider.transform;
        Transform best = current;

        while (current != null)
        {
            if (IsInLayerMask(current.gameObject.layer, obstacleLayer) ||
                IsInLayerMask(current.gameObject.layer, boostLayer))
            {
                best = current;
            }

            Transform parent = current.parent;

            if (parent == null || parent.GetComponent<SortingGroup>() != null)
            {
                break;
            }

            current = parent;
        }

        return best.gameObject;
    }

    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }

    private void NotifyFirstAttachAfterLaunch()
    {
        if (!hasLaunchedOnce || hasAttachedAfterFirstLaunch)
        {
            return;
        }

        hasAttachedAfterFirstLaunch = true;
        OnFirstAttachAfterLaunch?.Invoke();
    }

    private void AttachToPoleWithoutChecks(bool resetVisualInstantly = false)
    {
        isAttached = true;
        ApplyAttachedSprite();

        rb.velocity = Vector2.zero;
        rb.gravityScale = attachedGravityScale;

        transform.position = new Vector3(attachX, transform.position.y, transform.position.z);

        if (resetVisualInstantly)
        {
            ReturnVisualToNormalInstantly();
        }
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

        KeepVisualTopEdgeFixed();
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

        visual.localPosition = Vector3.Lerp(
            visual.localPosition,
            baseVisualLocalPosition,
            stretchReturnSpeed * Time.deltaTime
        );
    }

    private void ReturnVisualToFlyingScale()
    {
        if (visual == null)
        {
            return;
        }

        Vector3 targetScale = baseVisualScale * flyingVisualScaleMultiplier;

        visual.localScale = Vector3.Lerp(
            visual.localScale,
            targetScale,
            stretchReturnSpeed * Time.deltaTime
        );

        visual.localPosition = Vector3.Lerp(
            visual.localPosition,
            baseVisualLocalPosition,
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
        visual.localPosition = baseVisualLocalPosition;
    }

    private float GetVisualTopLocalY(Vector3 scale, Vector3 localPosition)
    {
        if (visualRenderer != null && visualRenderer.sprite != null)
        {
            visualTopOffset = visualRenderer.sprite.bounds.max.y;
        }

        return localPosition.y + visualTopOffset * scale.y;
    }

    private void ApplyAttachedSprite()
    {
        if (visualRenderer == null || attachedSprite == null)
        {
            return;
        }

        visualRenderer.sprite = attachedSprite;
        baseVisualTopLocalY = GetVisualTopLocalY(baseVisualScale, baseVisualLocalPosition);
    }

    private void ApplyFlyingSprite()
    {
        if (visualRenderer == null || flyingSprite == null)
        {
            return;
        }

        visualRenderer.sprite = flyingSprite;
    }

    private void ApplyVisualSortingOrder()
    {
        if (visualRenderer == null)
        {
            return;
        }

        visualRenderer.sortingOrder = visualSortingOrder;
    }

    private void KeepVisualTopEdgeFixed()
    {
        float targetLocalY = baseVisualTopLocalY - visualTopOffset * visual.localScale.y;

        visual.localPosition = new Vector3(
            baseVisualLocalPosition.x,
            targetLocalY,
            baseVisualLocalPosition.z
        );
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

    public void FreezePlayer()
    {
        inputEnabled = false;
        isDragging = false;
        isAttached = false;

        rb.velocity = Vector2.zero;
        rb.gravityScale = 0f;

        ReturnVisualToNormalInstantly();
    }

    public void ResetJumpLimit()
    {
        jumpsRemaining = Mathf.Max(0, maxJumps);
        jumpCounterInitialized = true;
        OnJumpsRemainingChanged?.Invoke(jumpsRemaining);
    }
}
