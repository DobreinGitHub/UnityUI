using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BubbleMenuAnimator : MonoBehaviour
{
    // --- Bubble Button Configuration ---
    [Header("Bubble Buttons")]
    [Tooltip("Array of RectTransforms for all bubble containers.")]
    [SerializeField] private RectTransform[] bubbleRects;

    // --- Control Button Configuration ---
    [Header("Control Buttons")]
    [SerializeField] private RectTransform controlLeftRect;
    [SerializeField] private RectTransform controlRightRect;

    private CanvasGroup controlLeftGroup;
    private CanvasGroup controlRightGroup;

    // --- Animation Settings ---
    [Header("Animation Settings")]
    [Tooltip("How long the position transition should last.")]
    [SerializeField] public float duration = 1.5f;
    [Tooltip("The delay between each bubble starting its move.")]
    [SerializeField] public float staggerDelay = 0.1f;

    [Tooltip("The vertical distance from the final position where the bubbles start.")]
    [SerializeField] private float verticalOffset = 200f;

    [Tooltip("The time (in seconds) the scale tween takes.")]
    [SerializeField] private float scaleSpeedFactor = 0.4f;

    [Tooltip("The time (in seconds) the alpha fade tween takes.")]
    [SerializeField] public float fadeSpeedFactor = 0.8f;

    [Tooltip("The initial size the bubbles start from.")]
    [SerializeField] private float initialScaleFactor = 0.2f;

    [Tooltip("Uses 'OutBack' ease for a controlled overshoot ('pop').")]
    [SerializeField] private bool usePopScaleEffect = false;

    [Header("Control Animation")]
    [SerializeField] private float controlsAppearDelay = 0.5f;
    [SerializeField] private float controlsAnimationDuration = 0.5f;

    // INTEGRATION FIELD - Must be linked in Inspector
    [Header("Integration")]
    [Tooltip("Reference to the BubbleSwapManager for state restoration.")]
    [SerializeField] private BubbleSwapManager swapManager;

    // Runtime storage
    private Vector3[] targetPositions;
    private float[] initialLocalZ; // Store original Z for a perfect reset
    private BubbleFloaterIndependent[] floaters;
    private CanvasGroup[] canvasGroups;


    private void Start()
    {
        if (bubbleRects == null || bubbleRects.Length == 0)
        {
            Debug.LogError("Bubble Rects array is empty.");
            enabled = false;
            return;
        }

        int numBubbles = bubbleRects.Length;

        // 1. Initialize arrays and get components for bubbles
        floaters = new BubbleFloaterIndependent[numBubbles];
        canvasGroups = new CanvasGroup[numBubbles];
        targetPositions = new Vector3[numBubbles];
        initialLocalZ = new float[numBubbles];

        for (int i = 0; i < numBubbles; i++)
        {
            targetPositions[i] = bubbleRects[i].anchoredPosition;
            initialLocalZ[i] = bubbleRects[i].localPosition.z;

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

            controlLeftGroup.alpha = 0f;
            controlRightGroup.alpha = 0f;
            controlLeftGroup.blocksRaycasts = false;
            controlRightGroup.blocksRaycasts = false;
        }

        // 3. Set initial state and start the animation
        ResetBubblesToStartPosition();
        AnimateMenuIn();
    }

    private CanvasGroup GetOrAddCanvasGroup(RectTransform rect)
    {
        CanvasGroup cg = rect.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = rect.gameObject.AddComponent<CanvasGroup>();
        }
        return cg;
    }

    // Calculates the default, unswapped, off-screen position for bubble i
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
            // Snaps X/Y to the original unswapped off-screen location
            rect.anchoredPosition = GetStartPosition(i);

            // Snaps Z to the original depth
            Vector3 currentLocalPos = rect.localPosition;
            rect.localPosition = new Vector3(currentLocalPos.x, currentLocalPos.y, initialLocalZ[i]);

            canvasGroups[i].alpha = 0f;
        }
    }


    // --- Animation Logic ---

    public void AnimateMenuIn()
    {
        // 0. Immediately disable interaction at the start of the sequence
        SetMenuInteractive(false);

        int numBubbles = bubbleRects.Length;
        Ease scaleInEase = usePopScaleEffect ? Ease.OutBack : Ease.OutCubic;

        // 1. Determine the final positions (swapped or unswapped)
        List<Vector3> finalPositions;
        if (swapManager != null && swapManager.GetCurrentPositions().Count == numBubbles)
        {
            finalPositions = swapManager.GetCurrentPositions();
        }
        else
        {
            finalPositions = new List<Vector3>();
            for (int i = 0; i < numBubbles; i++)
            {
                finalPositions.Add(new Vector3(targetPositions[i].x, targetPositions[i].y, initialLocalZ[i]));
            }
        }

        // 2. Determine the target alpha values
        float rearAlphaValue = 1f;
        if (swapManager != null)
        {
            rearAlphaValue = swapManager.rearAlpha;
        }

        // Find the center bubble (lowest Z in the target state)
        float minZ = float.MaxValue;
        int centerIndex = -1;
        for (int i = 0; i < numBubbles; i++)
        {
            if (finalPositions[i].z < minZ)
            {
                minZ = finalPositions[i].z;
                centerIndex = i;
            }
        }

        // 3. Stop all previous tweens and the float effect
        for (int i = 0; i < numBubbles; i++)
        {
            Tween.StopAll(bubbleRects[i]);
            floaters[i]?.StopFloat();
        }

        for (int i = 0; i < numBubbles; i++)
        {
            RectTransform rect = bubbleRects[i];

            float startTime = i * staggerDelay;
            Vector3 finalPosition = finalPositions[i];

            // 4. Position Tweens (Vertical Lift Up)
            // FIX: Instantly snap X to the final target X to enable vertical-only movement.
            rect.anchoredPosition = new Vector2(finalPosition.x, rect.anchoredPosition.y);

            // Tween the Y position upwards.
            Tween.UIAnchoredPosition(rect, new Vector2(finalPosition.x, finalPosition.y), duration, Ease.OutCubic, startDelay: startTime);

            // OPTIMIZATION FIX: Only tween Z if the target Z is different from the current Z
            if (!Mathf.Approximately(rect.localPosition.z, finalPosition.z))
            {
                // Animate Z position to the stored swapped depth
                Tween.LocalPositionZ(rect, finalPosition.z, duration, Ease.OutCubic, startDelay: startTime);
            }

            // 5. Scale and Alpha Tweens
            Tween.Scale(rect, Vector3.one, scaleSpeedFactor, scaleInEase, startDelay: startTime);

            // Tween to the correct final alpha based on whether it is the center bubble.
            float targetAlpha = (i == centerIndex) ? 1f : rearAlphaValue;
            Tween.Alpha(canvasGroups[i], targetAlpha, fadeSpeedFactor, Ease.Linear, startDelay: startTime);
        }

        // 6. Schedule the restart of the floating animation AND button appearance
        float maxSingleTweenTime = Mathf.Max(duration, scaleSpeedFactor, fadeSpeedFactor);
        float totalBubbleAnimationTime = (numBubbles - 1) * staggerDelay + maxSingleTweenTime;

        float controlsStartTime = totalBubbleAnimationTime + controlsAppearDelay;

        // A. Restart floaters and REFRESH INTERACTIVITY after bubbles finish
        Tween.Delay(totalBubbleAnimationTime).OnComplete(() =>
        {
            foreach (var floater in floaters)
            {
                floater?.StartFloat();
            }

            // CRITICAL: Tell the swap manager to re-check and set the correct raycast state 
            // (i.e., enable raycasts ONLY on the center bubble).
            swapManager?.UpdateVisualStates(instant: true);
        });

        // B. Animate Controls In
        if (controlLeftGroup != null)
        {
            Vector2 finalLeftPos = controlLeftRect.anchoredPosition;
            Vector2 finalRightPos = controlRightRect.anchoredPosition;

            // Start 20 units below final position for a slight lift effect
            controlLeftRect.anchoredPosition = finalLeftPos + Vector2.down * 20f;
            controlRightRect.anchoredPosition = finalRightPos + Vector2.down * 20f;

            Tween.UIAnchoredPosition(controlLeftRect, finalLeftPos, controlsAnimationDuration, Ease.OutCubic, startDelay: controlsStartTime);
            Tween.Alpha(controlLeftGroup, 1f, controlsAnimationDuration, Ease.Linear, startDelay: controlsStartTime)
                .OnComplete(() => controlLeftGroup.blocksRaycasts = true);

            Tween.UIAnchoredPosition(controlRightRect, finalRightPos, controlsAnimationDuration, Ease.OutCubic, startDelay: controlsStartTime);
            Tween.Alpha(controlRightGroup, 1f, controlsAnimationDuration, Ease.Linear, startDelay: controlsStartTime)
                .OnComplete(() => controlRightGroup.blocksRaycasts = true);
        }
    }

    public void AnimateMenuOut()
    {
        int numBubbles = bubbleRects.Length;
        Ease scaleOutEase = usePopScaleEffect ? Ease.InBack : Ease.InCubic;

        // 0. Immediately disable interaction at the start of the sequence
        SetMenuInteractive(false);

        // 1. Hide Controls Instantly
        if (controlLeftGroup != null)
        {
            controlLeftGroup.blocksRaycasts = false;
            controlRightGroup.blocksRaycasts = false;
            Tween.Alpha(controlLeftGroup, 0f, 0.2f);
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

            // Calculates the destination position using the CURRENT X position (vertical-only exit).
            Vector2 currentAnchoredPos = rect.anchoredPosition;
            Vector2 destinationPosition = new Vector2(
                currentAnchoredPos.x,
                currentAnchoredPos.y - verticalOffset
            );

            // 1. Position Tween (Move Down Off-Screen)
            Tween.UIAnchoredPosition(rect, destinationPosition, duration, Ease.InCubic, startDelay: startTime);

            // 2. Scale Tween (Shrink back to initial size)
            Tween.Scale(rect, Vector3.one * initialScaleFactor, scaleSpeedFactor, scaleOutEase, startDelay: startTime);

            // 3. Alpha Tween (Fade Out)
            Tween tween = Tween.Alpha(canvasGroups[i], 0f, fadeSpeedFactor, Ease.Linear, startDelay: startTime);

            // OnComplete action for the very last bubble's tween (when i == 0)
            if (i == 0)
            {
                tween.OnComplete(() => ResetBubblesToStartPosition());
            }
        }
    }

    // --- Testing / Debug Utility ---
    public void ReplayMenuAnimation()
    {
        AnimateMenuOut();

        int numBubbles = bubbleRects.Length;
        float maxSingleDuration = Mathf.Max(duration, scaleSpeedFactor, fadeSpeedFactor);
        float totalExitTime = (numBubbles - 1) * staggerDelay + maxSingleDuration;
        float delayBeforeReplay = totalExitTime + 0.1f;

        Tween.Delay(delayBeforeReplay).OnComplete(() =>
        {
            AnimateMenuIn();
        });
    }

    // --- Interaction Helper ---
    public void SetMenuInteractive(bool isInteractive)
    {
        // Set blocksRaycasts for all elements managed by the animator (bubbles and controls)

        // 1. Temporarily disable ALL bubble raycasts if we are NOT interactive.
        for (int i = 0; i < canvasGroups.Length; i++)
        {
            canvasGroups[i].blocksRaycasts = isInteractive;
        }

        // 2. For Controls: Handle the control buttons explicitly.
        if (controlLeftGroup != null)
        {
            controlLeftGroup.blocksRaycasts = isInteractive;
        }
        if (controlRightGroup != null)
        {
            controlRightGroup.blocksRaycasts = isInteractive;
        }
    }
}