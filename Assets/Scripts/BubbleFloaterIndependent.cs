using UnityEngine;
using PrimeTween;

public class BubbleFloaterIndependent : MonoBehaviour
{
    // === CONFIGURATION ===

    [Header("Vertical Movement")]
    public float verticalAmplitude = 10f; // Max vertical offset (in UI units)
    public float verticalDuration = 3.5f; // Time for one half-cycle (e.g., up)

    [Header("Horizontal Movement")]
    public float horizontalAmplitude = 8f; // Max horizontal offset (in UI units)
    public float horizontalDuration = 5f; // Time for one half-cycle (e.g., right)

    private RectTransform childRectTransform;
    private Tween currentVerticalTween;
    private Tween currentHorizontalTween;
    private bool isFloating = false;

    void Awake()
    {
        // ASSUMPTION: The visuals (the part that floats) are on the IMMEDIATE first child.
        if (transform.childCount > 0)
        {
            childRectTransform = transform.GetChild(0).GetComponent<RectTransform>();
        }

        if (childRectTransform == null)
        {
            Debug.LogError($"Floater on {gameObject.name} requires a child RectTransform to move.");
            enabled = false;
            return;
        }

        // Start floating immediately when the component is active
        StartFloat();
    }

    // Public method called by BubbleSwapManager to START the float effect
    public void StartFloat()
    {
        if (isFloating) return;
        isFloating = true;

        // Reset position to zero before starting
        childRectTransform.anchoredPosition = Vector2.zero;

        // Start the manual loops with a slight random factor for unique movement
        float randomFactor = Random.Range(0.9f, 1.1f);
        RunVerticalLoop(verticalDuration * randomFactor);
        RunHorizontalLoop(horizontalDuration * randomFactor);
    }

    // Public method called by BubbleSwapManager to STOP the float effect
    public void StopFloat()
    {
        currentVerticalTween.Stop();
        currentHorizontalTween.Stop();
        isFloating = false;

        // CRITICAL: Snap child back to (0,0) immediately for stable swapping
        if (childRectTransform != null)
        {
            childRectTransform.anchoredPosition = Vector2.zero;
        }
    }

    // --- Manual Recursive Loops (Reliably creates the Yoyo/PingPong effect) ---

    private void RunVerticalLoop(float duration)
    {
        if (!isFloating) return;

        // 1. Move UP
        currentVerticalTween = Tween.UIAnchoredPositionY(childRectTransform, verticalAmplitude, duration, Ease.InOutSine)
            .OnComplete(() =>
            {
                if (!isFloating) return;
                // 2. Move BACK DOWN
                currentVerticalTween = Tween.UIAnchoredPositionY(childRectTransform, 0f, duration, Ease.InOutSine)
                    .OnComplete(() => RunVerticalLoop(duration)); // 3. Repeat
            });
    }

    private void RunHorizontalLoop(float duration)
    {
        if (!isFloating) return;

        // 1. Move RIGHT
        currentHorizontalTween = Tween.UIAnchoredPositionX(childRectTransform, horizontalAmplitude, duration, Ease.InOutSine)
            .OnComplete(() =>
            {
                if (!isFloating) return;
                // 2. Move BACK LEFT
                currentHorizontalTween = Tween.UIAnchoredPositionX(childRectTransform, 0f, duration, Ease.InOutSine)
                    .OnComplete(() => RunHorizontalLoop(duration)); // 3. Repeat
            });
    }

    void OnDisable() => StopFloat();
    void OnDestroy() => StopFloat();
}