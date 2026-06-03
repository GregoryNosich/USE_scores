using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float yOffset = 2f;
    [SerializeField] private float minY = 0f;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        float targetY = Mathf.Max(minY, target.position.y + yOffset);

        Vector3 newPosition = new Vector3(
            transform.position.x,
            Mathf.Lerp(transform.position.y, targetY, smoothSpeed * Time.deltaTime),
            transform.position.z
        );

        transform.position = newPosition;
    }
}
