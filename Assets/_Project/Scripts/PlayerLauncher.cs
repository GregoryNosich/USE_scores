using UnityEngine;

public class PlayerLauncher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Launch Settings")]
    [SerializeField] private float maxDragDistance = 2.5f;
    [SerializeField] private float launchPower = 12f;

    [Header("Attach Settings")]
    [SerializeField] private float attachedGravityScale = 0f;
    [SerializeField] private float flyingGravityScale = 1.5f;

    private bool isAttached = true;
    private bool isDragging = false;

    private Vector2 dragStartWorld;
    private Vector2 currentDragWorld;

    private float attachX;

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

        attachX = transform.position.x;

        AttachToPole();
    }

    private void Update()
    {
        if (isAttached)
        {
            HandleDragLaunch();
        }
        else
        {
            HandleAirAttach();
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
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;

            float dragDownDistance = Mathf.Max(0f, dragStartWorld.y - currentDragWorld.y);
            dragDownDistance = Mathf.Clamp(dragDownDistance, 0f, maxDragDistance);

            Launch(dragDownDistance);
        }
    }

    private void HandleAirAttach()
    {
        if (Input.GetMouseButtonDown(0))
        {
            AttachToPole();
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

        float force = dragDistance / maxDragDistance * launchPower;
        rb.velocity = new Vector2(0f, force);
    }

    private void AttachToPole()
    {
        isAttached = true;

        rb.velocity = Vector2.zero;
        rb.gravityScale = attachedGravityScale;

        transform.position = new Vector3(attachX, transform.position.y, transform.position.z);
    }

    private Vector2 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPosition = Input.mousePosition;
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);

        return new Vector2(mouseWorldPosition.x, mouseWorldPosition.y);
    }
}