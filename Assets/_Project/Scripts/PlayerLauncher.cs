using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;

public class PlayerLauncher : MonoBehaviour
{
    public event Action OnFirstLaunch;
    public event Action OnFirstAttachAfterLaunch;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform visual;
    [SerializeField] private SpriteRenderer visualRenderer;

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
    [SerializeField] private float flyingVisualScaleMultiplier = 1.5f;

    [Header("Sprite Settings")]
    [SerializeField] private Sprite attachedSprite;
    [SerializeField] private Sprite flyingSprite;

    [Header("Sorting Settings")]
    [SerializeField] private int visualSortingOrder = 1000;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource oneShotAudioSource;
    [SerializeField] private AudioSource flyAudioSource;
    [SerializeField] private AudioSource stretchAudioSource;
    [SerializeField] private AudioClip flyClip;
    [SerializeField] private AudioClip stretchClip;
    [SerializeField] private AudioClip stickClip;
    [SerializeField] private AudioClip greenZoneClip;
    [SerializeField] private AudioClip redZoneClip;
    [Min(0f)]
    [SerializeField] private float flyVolume = 1f;
    [Min(0f)]
    [SerializeField] private float stretchVolume = 1f;
    [Min(0f)]
    [SerializeField] private float stretchLoopTailDuration = 0.25f;
    [Min(0f)]
    [SerializeField] private float stickVolume = 1f;
    [Min(0f)]
    [SerializeField] private float greenZoneVolume = 1f;
    [Min(0f)]
    [SerializeField] private float redZoneVolume = 1f;

    private bool isAttached = true;
    private bool isDragging = false;
    private bool inputEnabled = true;
    private bool hasLaunchedOnce = false;
    private bool hasAttachedAfterFirstLaunch = false;

    private Vector2 dragStartWorld;
    private Vector2 currentDragWorld;

    private float attachX;
    private float nextLaunchMultiplier = 1f;

    private Vector3 baseVisualScale;
    private Vector3 baseVisualLocalPosition;
    private float baseVisualTopLocalY;
    private float visualTopOffset = 0.5f;
    private bool isStretchSoundActive;
    private readonly List<AudioSource> stretchAudioSources = new List<AudioSource>();

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
        EnsureAudioSources();

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

        AttachToPoleWithoutChecks(true, false);
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
            UpdateStretchSound(dragPower01);
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            currentDragWorld = GetMouseWorldPosition();
            isDragging = false;
            StopStretchSound();

            float dragDistance = GetDragDistance();
            Launch(dragDistance);
        }

        if (!isDragging)
        {
            StopStretchSound();
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

        if (!hasLaunchedOnce)
        {
            hasLaunchedOnce = true;
            OnFirstLaunch?.Invoke();
        }

        isAttached = false;
        ApplyFlyingSprite();
        PlayFlySound();

        rb.gravityScale = flyingGravityScale;
        rb.velocity = Vector2.zero;

        float force = dragDistance / maxDragDistance * launchPower * nextLaunchMultiplier;
        rb.velocity = new Vector2(0f, force);

        nextLaunchMultiplier = 1f;
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
            PlayOneShot(redZoneClip, redZoneVolume);
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
            PlayOneShot(greenZoneClip, greenZoneVolume);
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

    private void AttachToPoleWithoutChecks(bool resetVisualInstantly = false, bool playStickSound = true)
    {
        isAttached = true;
        ApplyAttachedSprite();
        StopFlySound();
        StopStretchSound();

        if (playStickSound)
        {
            PlayOneShot(stickClip, stickVolume);
        }

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

    private void EnsureAudioSources()
    {
        oneShotAudioSource = EnsureAudioSource(oneShotAudioSource, false);
        flyAudioSource = EnsureAudioSource(flyAudioSource, false);
        stretchAudioSource = EnsureAudioSource(stretchAudioSource, false);
        stretchAudioSources.Clear();
        stretchAudioSources.Add(stretchAudioSource);
    }

    private AudioSource EnsureAudioSource(AudioSource audioSource, bool loop)
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = loop;

        return audioSource;
    }

    private void PlayOneShot(AudioClip clip, float volume)
    {
        PlayOneShotScaled(oneShotAudioSource, clip, volume);
    }

    private void UpdateStretchSound(float stretchPower01)
    {
        if (stretchPower01 <= 0.01f)
        {
            StopStretchSound();
            return;
        }

        if (stretchClip == null)
        {
            return;
        }

        int activeSourcesCount = EnsureStretchAudioSourcesForVolume();

        if (activeSourcesCount <= 0)
        {
            StopStretchSound();
            return;
        }

        ApplyStretchSourceVolumes(activeSourcesCount);

        if (!isStretchSoundActive)
        {
            StartStretchSound(activeSourcesCount);
            return;
        }

        LoopStretchTailIfNeeded(activeSourcesCount);
    }

    private void PlayFlySound()
    {
        PlayOneShotScaled(flyAudioSource, flyClip, flyVolume);
    }

    private void StopFlySound()
    {
        StopAudioSource(flyAudioSource);
    }

    private void StopStretchSound()
    {
        isStretchSoundActive = false;

        for (int i = 0; i < stretchAudioSources.Count; i++)
        {
            StopAudioSource(stretchAudioSources[i]);
        }
    }

    private void PlayOneShotScaled(AudioSource audioSource, AudioClip clip, float volume)
    {
        if (audioSource == null || clip == null || volume <= 0f)
        {
            return;
        }

        float remainingVolume = volume;

        while (remainingVolume > 0f)
        {
            float layerVolume = Mathf.Min(remainingVolume, 1f);
            audioSource.PlayOneShot(clip, layerVolume);
            remainingVolume -= layerVolume;
        }
    }

    private int EnsureStretchAudioSourcesForVolume()
    {
        int activeSourcesCount = Mathf.CeilToInt(Mathf.Max(0f, stretchVolume));

        if (activeSourcesCount <= 0)
        {
            return 0;
        }

        if (stretchAudioSources.Count == 0 || stretchAudioSources[0] == null)
        {
            stretchAudioSource = EnsureAudioSource(stretchAudioSource, false);
            stretchAudioSources.Clear();
            stretchAudioSources.Add(stretchAudioSource);
        }

        while (stretchAudioSources.Count < activeSourcesCount)
        {
            stretchAudioSources.Add(EnsureAudioSource(null, false));
        }

        return activeSourcesCount;
    }

    private void ApplyStretchSourceVolumes(int activeSourcesCount)
    {
        for (int i = 0; i < stretchAudioSources.Count; i++)
        {
            AudioSource source = stretchAudioSources[i];

            if (source == null)
            {
                continue;
            }

            if (i >= activeSourcesCount)
            {
                StopAudioSource(source);
                continue;
            }

            source.volume = GetLayerVolume(stretchVolume, i);
        }
    }

    private float GetLayerVolume(float totalVolume, int layerIndex)
    {
        return Mathf.Clamp01(totalVolume - layerIndex);
    }

    private void StartStretchSound(int activeSourcesCount)
    {
        isStretchSoundActive = true;
        PlayStretchSourcesFrom(0f, activeSourcesCount);
    }

    private void LoopStretchTailIfNeeded(int activeSourcesCount)
    {
        AudioSource referenceSource = stretchAudioSources[0];
        float loopStartTime = GetStretchLoopStartTime();

        if (referenceSource == null || !referenceSource.isPlaying)
        {
            PlayStretchSourcesFrom(loopStartTime, activeSourcesCount);
            return;
        }

        if (referenceSource.time >= stretchClip.length - 0.02f)
        {
            PlayStretchSourcesFrom(loopStartTime, activeSourcesCount);
            return;
        }

        SyncMissingStretchSources(referenceSource.time, activeSourcesCount);
    }

    private void PlayStretchSourcesFrom(float time, int activeSourcesCount)
    {
        for (int i = 0; i < stretchAudioSources.Count; i++)
        {
            AudioSource source = stretchAudioSources[i];

            if (source == null)
            {
                continue;
            }

            if (i >= activeSourcesCount)
            {
                StopAudioSource(source);
                continue;
            }

            source.loop = false;
            source.clip = stretchClip;
            source.time = time;
            source.Play();
        }
    }

    private void SyncMissingStretchSources(float time, int activeSourcesCount)
    {
        for (int i = 0; i < activeSourcesCount; i++)
        {
            AudioSource source = stretchAudioSources[i];

            if (source == null || source.isPlaying)
            {
                continue;
            }

            source.loop = false;
            source.clip = stretchClip;
            source.time = time;
            source.Play();
        }
    }

    private float GetStretchLoopStartTime()
    {
        if (stretchClip == null || stretchClip.length <= 0f)
        {
            return 0f;
        }

        float tailDuration = Mathf.Max(0.01f, stretchLoopTailDuration);
        float maxLoopStartTime = Mathf.Max(0f, stretchClip.length - 0.01f);

        return Mathf.Clamp(stretchClip.length - tailDuration, 0f, maxLoopStartTime);
    }

    private void StopAudioSource(AudioSource audioSource)
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
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
            StopFlySound();
            StopStretchSound();
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
        StopFlySound();
        StopStretchSound();

        ReturnVisualToNormalInstantly();
    }

}
