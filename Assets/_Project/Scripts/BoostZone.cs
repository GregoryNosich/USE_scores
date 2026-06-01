using UnityEngine;

public class BoostZone : MonoBehaviour
{
    [SerializeField] private float launchMultiplier = 1.5f;
    [SerializeField] private bool consumeAfterUse = false;

    public float LaunchMultiplier => launchMultiplier;

    public void Collect()
    {
        if (consumeAfterUse)
        {
            gameObject.SetActive(false);
        }
    }
}