using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttachZoneFeedback : MonoBehaviour
{
    [SerializeField] private float flashDuration = 1f;
    [SerializeField] private float brightnessMultiplier = 1.8f;

    private const float ScaleMultiplier = 1.08f;

    private SpriteRenderer[] renderers;
    private Color[] baseColors;
    private Vector3[] baseScales;
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
        SpriteRenderer[] allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        renderers = GetVisualRenderers(allRenderers);
        baseColors = new Color[renderers.Length];
        baseScales = new Vector3[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            baseColors[i] = renderers[i].color;
            baseScales[i] = renderers[i].transform.localScale;
        }
    }

    private SpriteRenderer[] GetVisualRenderers(SpriteRenderer[] allRenderers)
    {
        List<SpriteRenderer> visualRenderers = new List<SpriteRenderer>();

        for (int i = 0; i < allRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = allRenderers[i];

            if (spriteRenderer == null || spriteRenderer.transform == transform)
            {
                continue;
            }

            visualRenderers.Add(spriteRenderer);
        }

        if (visualRenderers.Count > 0)
        {
            return visualRenderers.ToArray();
        }

        return allRenderers;
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
                renderers[i].transform.localScale = Vector3.Lerp(
                    baseScales[i] * ScaleMultiplier,
                    baseScales[i],
                    t
                );
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
                renderers[i].transform.localScale = baseScales[i] * ScaleMultiplier;
            }
        }
    }

    private Color GetBrightColor(Color color)
    {
        return new Color(
            color.r * brightnessMultiplier,
            color.g * brightnessMultiplier,
            color.b * brightnessMultiplier,
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
        if (renderers == null || baseColors == null || baseScales == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].color = baseColors[i];
                renderers[i].transform.localScale = baseScales[i];
            }
        }
    }
}
