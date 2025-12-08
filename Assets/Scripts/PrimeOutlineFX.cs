using UnityEngine;
using UnityEngine.UI;
using PrimeTween;
using System.Collections;
using System.Collections.Generic;

public class PrimeOutlineFX : MonoBehaviour
{
    private RectTransform rectTransform;
    private Vector2 originalAnchoredPosition;
    private Coroutine glitchCoroutine;

    [Header("Glow Parameters (Choppy Fix)")]
    [Tooltip("Target FPS (e.g., 10 FPS means an update every 0.1 seconds).")]
    public float targetFPS = 7f;

    [Tooltip("Max scale difference (e.g., 0.03 means 1.0 to 1.03)")]
    public float scaleRange = 0.02f;

    [Header("Wobble Parameters (Choppy)")]
    [Tooltip("Max random position offset in pixels.")]
    public float positionRange = 1.0f;

    void Awake()
    {
        // 🛑 FINAL FIX: Suppress the zero-duration warnings
        PrimeTweenConfig.warnZeroDuration = false;
        PrimeTweenConfig.warnEndValueEqualsCurrent = false;

        rectTransform = GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            Debug.LogError($"[PrimeOutlineFX] Missing RectTransform component on {gameObject.name}. Animation cannot run!", this);
            return;
        }

        originalAnchoredPosition = rectTransform.anchoredPosition;
        StopVisuals();
    }

    // ... (rest of the script is unchanged and correct)

    private void StopVisuals()
    {
        if (rectTransform == null) return;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.anchoredPosition = originalAnchoredPosition;
    }

    public void PlayAnimation()
    {
        if (rectTransform == null) return;

        StopAllTweens();

        glitchCoroutine = StartCoroutine(ChoppyGlitchLoop());
    }

    private void StopAllTweens()
    {
        if (glitchCoroutine != null)
        {
            StopCoroutine(glitchCoroutine);
            glitchCoroutine = null;
        }
    }

    private IEnumerator ChoppyGlitchLoop()
    {
        float delay = 1f / targetFPS;
        WaitForSeconds waitTime = new WaitForSeconds(delay);

        // This line is now clean, but the warnings are suppressed in Awake()
        Tween.Rotation(rectTransform, Vector3.zero, 0f);

        while (true)
        {
            float randomScale = 1.0f + Random.Range(0, scaleRange);
            Vector2 randomPosition = originalAnchoredPosition + new Vector2(
                Random.Range(-positionRange, positionRange),
                Random.Range(-positionRange, positionRange)
            );

            rectTransform.localScale = new Vector3(randomScale, randomScale, 1f);
            rectTransform.anchoredPosition = randomPosition;

            yield return waitTime;
        }
    }

    public void StopAnimation()
    {
        StopAllTweens();
        StopVisuals();
    }

    private void OnDisable()
    {
        StopAnimation();
    }
}