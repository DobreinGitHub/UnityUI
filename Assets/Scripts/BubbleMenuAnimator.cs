using PrimeTween;
using UnityEngine;
using UnityEngine.UI; // Required for RectTransform and CanvasGroup

public class BubbleMenuAnimator : MonoBehaviour
{
    // --- Bubble Button Configuration ---
    [Header("Bubble Buttons")]
    [Tooltip("Array of RectTransforms for all bubble containers.")]
    [SerializeField] private RectTransform[] bubbleRects;

    // --- Control Button Configuration ---
    [Header("Control Buttons")]
    [Tooltip("The RectTransform for the left control button (Control Left).")]
    [SerializeField] private RectTransform controlLeftRect;
    [Tooltip("The RectTransform for the right control button (Control Right).")]
    [SerializeField] private RectTransform controlRightRect;

    private CanvasGroup controlLeftGroup;
    private CanvasGroup controlRightGroup;

    // --- Animation Settings ---
    [Header("Animation Settings")]
    [Tooltip("How long the position transition should last.")]
    [SerializeField] private float duration = 1.5f;
    [Tooltip("The delay between each bubble starting its move. Set to 0.0 for simultaneous start.")]
    [SerializeField] private float staggerDelay = 0.1f;

    [Tooltip("The vertical distance from the final position where the bubbles start (less distance = less aggressive).")]
    [SerializeField] private float verticalOffset = 200f;

    [Tooltip("The time (in seconds) the scale tween takes. Smaller value = faster pop.")]
    [SerializeField] private float scaleSpeedFactor = 0.4f;

    [Tooltip("The time (in seconds) the alpha fade tween takes.")]
    [SerializeField] private float fadeSpeedFactor = 0.8f;

    [Tooltip("The initial size the bubbles start from (0.0 is invisible).")]
    [SerializeField] private float initialScaleFactor = 0.2f;

    [Tooltip("If checked, uses the 'OutBack' ease for a controlled overshoot ('pop'). If unchecked, uses smooth 'OutCubic'.")]
    [SerializeField] private bool usePopScaleEffect = false;

    [Header("Control Animation")]
    [Tooltip("Delay AFTER the main bubble animation finishes before controls appear.")]
    [SerializeField] private float controlsAppearDelay = 0.5f;
    [Tooltip("Duration of the controls' fade/move in animation.")]
    [SerializeField] private float controlsAnimationDuration = 0.5f;

    // Runtime storage
    private Vector3[] targetPositions;
    private BubbleFloaterIndependent[] floaters;
    private CanvasGroup[] canvasGroups;


    private void Start()
    {
        if (bubbleRects == null || bubbleRects.Length == 0)
        {
            Debug.LogError("Bubble Rects array is empty. Please assign your 5 bubble RectTransforms in the Inspector.");
            enabled = false;
            return;
        }

        int numBubbles = bubbleRects.Length;

        // 1. Initialize arrays and get components for bubbles
        floaters = new BubbleFloaterIndependent[numBubbles];
        canvasGroups = new CanvasGroup[numBubbles];
        targetPositions = new Vector3[numBubbles];

        for (int i = 0; i < numBubbles; i++)
        {
            targetPositions[i] = bubbleRects[i].anchoredPosition;
            floaters[i] = bubbleRects[i].GetComponent<BubbleFloaterIndependent>();
            canvasGroups[i] = GetOrAddCanvasGroup(bubbleRects[i]);

            if (floaters[i] == null)
            {
                Debug.LogError($"Bubble {bubbleRects[i].name} is missing BubbleFloaterIndependent.");
                enabled = false;
                return;
            }
        }

        // 2. Initialize Control Buttons
        if (controlLeftRect != null && controlRightRect != null)
        {
            controlLeftGroup = GetOrAddCanvasGroup(controlLeftRect);
            controlRightGroup = GetOrAddCanvasGroup(controlRightRect);

            // Hide and disable interactivity instantly
            controlLeftGroup.alpha = 0f;
            controlRightGroup.alpha = 0f;
            controlLeftGroup.blocksRaycasts = false;
            controlRightGroup.blocksRaycasts = false;
        }

        // 3. Set initial state and start the animation
        ResetBubblesToStartPosition();
        AnimateMenuIn();
    }

    // Helper to ensure CanvasGroup exists
    private CanvasGroup GetOrAddCanvasGroup(RectTransform rect)
    {
        CanvasGroup cg = rect.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = rect.gameObject.AddComponent<CanvasGroup>();
        }
        return cg;
    }

    private Vector3 GetStartPosition(int index)
    {
        float startX = targetPositions[index].x;
        float startY = targetPositions[index].y - verticalOffset;

        return new Vector3(startX, startY, 0);
    }

    private void ResetBubblesToStartPosition()
    {
        for (int i = 0; i < bubbleRects.Length; i++)
        {
            RectTransform rect = bubbleRects[i];

            rect.localScale = Vector3.one * initialScaleFactor;
            rect.anchoredPosition = GetStartPosition(i);
            canvasGroups[i].alpha = 0f;
        }
    }


    // --- Animation Logic ---

    public void AnimateMenuIn()
    {
        int numBubbles = bubbleRects.Length;
        Ease scaleInEase = usePopScaleEffect ? Ease.OutBack : Ease.OutCubic;

        // 1. Stop all previous tweens and the float effect
        for (int i = 0; i < numBubbles; i++)
        {
            Tween.StopAll(bubbleRects[i]);
            floaters[i]?.StopFloat();
        }

        for (int i = 0; i < numBubbles; i++)
        {
            RectTransform rect = bubbleRects[i];

            float startTime = i * staggerDelay;
            Vector3 finalPosition = targetPositions[i];

            // 1. Position Tween (Lift Up)
            Tween.UIAnchoredPosition(rect, finalPosition, duration, Ease.OutCubic, startDelay: startTime);

            // 2. Scale Tween (Pop Out to original size 1.0)
            Tween.Scale(rect, Vector3.one, scaleSpeedFactor, scaleInEase, startDelay: startTime);

            // 3. Alpha Tween (Fade In)
            Tween.Alpha(canvasGroups[i], 1f, fadeSpeedFactor, Ease.Linear, startDelay: startTime);
        }

        // 4. Schedule the restart of the floating animation AND button appearance
        float maxSingleTweenTime = Mathf.Max(duration, scaleSpeedFactor, fadeSpeedFactor);
        float totalBubbleAnimationTime = (numBubbles - 1) * staggerDelay + maxSingleTweenTime;

        // Time when controls start moving
        float controlsStartTime = totalBubbleAnimationTime + controlsAppearDelay;

        // A. Restart floaters after bubbles finish
        Tween.Delay(totalBubbleAnimationTime).OnComplete(() =>
        {
            foreach (var floater in floaters)
            {
                floater?.StartFloat();
            }
        });

        // B. Animate Controls In
        if (controlLeftGroup != null)
        {
            Vector2 finalLeftPos = controlLeftRect.anchoredPosition;
            Vector2 finalRightPos = controlRightRect.anchoredPosition;

            // Start 20 units below final position for a slight lift effect
            controlLeftRect.anchoredPosition = finalLeftPos + Vector2.down * 20f;
            controlRightRect.anchoredPosition = finalRightPos + Vector2.down * 20f;

            // Animate Left Control: Move and Fade
            Tween.UIAnchoredPosition(controlLeftRect, finalLeftPos, controlsAnimationDuration, Ease.OutCubic, startDelay: controlsStartTime);
            Tween.Alpha(controlLeftGroup, 1f, controlsAnimationDuration, Ease.Linear, startDelay: controlsStartTime)
                .OnComplete(() => controlLeftGroup.blocksRaycasts = true);

            // Animate Right Control: Move and Fade
            Tween.UIAnchoredPosition(controlRightRect, finalRightPos, controlsAnimationDuration, Ease.OutCubic, startDelay: controlsStartTime);
            Tween.Alpha(controlRightGroup, 1f, controlsAnimationDuration, Ease.Linear, startDelay: controlsStartTime)
                .OnComplete(() => controlRightGroup.blocksRaycasts = true);
        }
    }

    public void AnimateMenuOut()
    {
        int numBubbles = bubbleRects.Length;
        // Use InBack or InCubic for a quick disappearing effect
        Ease scaleOutEase = usePopScaleEffect ? Ease.InBack : Ease.InCubic;

        // 1. Hide Controls Instantly (or with a small fade if desired)
        if (controlLeftGroup != null)
        {
            controlLeftGroup.blocksRaycasts = false;
            controlRightGroup.blocksRaycasts = false;
            Tween.Alpha(controlLeftGroup, 0f, 0.2f); // Optional small fade out
            Tween.Alpha(controlRightGroup, 0f, 0.2f);
        }

        // 2. Stop all previous tweens and the float effect
        for (int i = 0; i < numBubbles; i++)
        {
            Tween.StopAll(bubbleRects[i]);
            floaters[i]?.StopFloat();
        }

        // Reverse the loop for a "closing" ripple effect
        for (int i = numBubbles - 1; i >= 0; i--)
        {
            RectTransform rect = bubbleRects[i];

            float startTime = (numBubbles - 1 - i) * staggerDelay;
            Vector3 destinationPosition = GetStartPosition(i);

            // 1. Position Tween (Move Down Off-Screen)
            Tween.UIAnchoredPosition(rect, destinationPosition, duration, Ease.InCubic, startDelay: startTime);

            // 2. Scale Tween (Shrink back to initial size)
            Tween.Scale(rect, Vector3.one * initialScaleFactor, scaleSpeedFactor, scaleOutEase, startDelay: startTime);

            // 3. Alpha Tween (Fade Out)
            Tween.Alpha(canvasGroups[i], 0f, fadeSpeedFactor, Ease.Linear, startDelay: startTime);

            // NOTE: Add OnComplete call here if you need to run an action (like showing the quit pop-up) 
            // after the entire menu closes. You'd check for i == 0 (the last bubble to animate).
        }
    }
}