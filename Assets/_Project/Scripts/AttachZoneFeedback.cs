using System.Collections;
using UnityEngine;

public class AttachZoneFeedback : MonoBehaviour
{
    [SerializeField] private float flashDuration = 1f;
    [SerializeField] private float brightnessMultiplier = 1.8f;

    private SpriteRenderer[] renderers;
    private Color[] baseColors;
    private Coroutine flashRoutine;

    private void Awake()
    {
        CacheRenderers();
    }

    private void OnDisable()
    {
        StopFlash();
    }

    public void Flash()
    {
        StopFlash();
        CacheRenderers();

        if (renderers.Length == 0)
        {
            return;
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private void CacheRenderers()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            baseColors[i] = renderers[i].color;
        }
    }

    private IEnumerator FlashRoutine()
    {
        ApplyBrightColors();

        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flashDuration);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                {
                    continue;
                }

                Color brightColor = GetBrightColor(baseColors[i]);
                renderers[i].color = Color.Lerp(brightColor, baseColors[i], t);
            }

            yield return null;
        }

        RestoreBaseColors();
        flashRoutine = null;
    }

    private void ApplyBrightColors()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].color = GetBrightColor(baseColors[i]);
            }
        }
    }

    private Color GetBrightColor(Color color)
    {
        return new Color(
            Mathf.Clamp01(color.r * brightnessMultiplier),
            Mathf.Clamp01(color.g * brightnessMultiplier),
            Mathf.Clamp01(color.b * brightnessMultiplier),
            color.a
        );
    }

    private void StopFlash()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        RestoreBaseColors();
    }

    private void RestoreBaseColors()
    {
        if (renderers == null || baseColors == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].color = baseColors[i];
            }
        }
    }
}
