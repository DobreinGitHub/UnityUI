using UnityEngine;
using PrimeTween;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem; // Required for Keyboard input
using UnityEngine.UI;

public class BubbleSwapManager : MonoBehaviour
{
    // === CONFIGURATION ===

    [Tooltip("List of all bubble containers (RectTransforms) in order.")]
    public RectTransform[] bubbleTransforms;

    [Tooltip("Duration for the smooth position swap animation.")]
    public float swapDuration = 0.3f;

    [Header("Cooldown Settings")]
    [Tooltip("Minimum time (in seconds) between consecutive swap clicks or key presses.")]
    public float swapCooldown = 1.0f;

    [Header("Visual Depth Settings")]
    [Tooltip("Alpha (Opacity) for bubbles that are NOT in the center/focus spot (80%).")]
    public float rearAlpha = 0.8f;

    // === RUNTIME DATA ===
    private List<Vector3> currentPositions;
    private int bubbleCount;
    private int completedSwaps = 0;

    private float nextSwapTime = 0f;

    void Awake()
    {
        if (bubbleTransforms == null || bubbleTransforms.Length == 0) return;

        bubbleCount = bubbleTransforms.Length;

        currentPositions = new List<Vector3>();

        for (int i = 0; i < bubbleCount; i++)
        {
            var rt = bubbleTransforms[i];
            currentPositions.Add(new Vector3(rt.anchoredPosition.x, rt.anchoredPosition.y, rt.localPosition.z));

            // Ensure every bubble has a CanvasGroup for alpha control
            if (rt.GetComponent<CanvasGroup>() == null)
            {
                rt.gameObject.AddComponent<CanvasGroup>();
            }
        }

        // Apply initial visual states instantly
        UpdateVisualStates(instant: true);
    }

    void Update()
    {
        // === KEYBOARD INPUT CHECKING ===
        var keyboard = Keyboard.current;

        if (keyboard == null) return;

        if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            PerformSwap(isRight: false);
        }

        if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            PerformSwap(isRight: true);
        }

        // NOTE: Escape key logic has been removed as requested.
    }

    // =========================================================
    // === PUBLIC FUNCTIONS FOR UI BUTTONS ===
    // =========================================================

    // 📢 Connect your "Swap Left" UI button here.
    public void SwapLeft() => PerformSwap(isRight: false);

    // 📢 Connect your "Swap Right" UI button here.
    public void SwapRight() => PerformSwap(isRight: true);

    private void PerformSwap(bool isRight)
    {
        // === COOLDOWN CHECK ===
        if (Time.time < nextSwapTime)
        {
            return;
        }

        // 1. Set the next time a swap can occur
        nextSwapTime = Time.time + swapCooldown;

        // 2. Calculate the new target positions (Shifting the List)
        List<Vector3> targetPositions = new List<Vector3>(currentPositions);

        if (isRight)
        {
            // Move the first element to the end 
            Vector3 temp = targetPositions[0];
            targetPositions.RemoveAt(0);
            targetPositions.Add(temp);
        }
        else
        {
            // Move the last element to the start
            Vector3 temp = targetPositions[targetPositions.Count - 1];
            targetPositions.RemoveAt(targetPositions.Count - 1);
            targetPositions.Insert(0, temp);
        }

        // 3. Force UI Layout Update for stability
        foreach (var rt in bubbleTransforms)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        // Reset counters
        completedSwaps = 0;

        // 4. Start the visual state animation (alpha)
        UpdateVisualStates(instant: false, targetPositions);

        // 5. Animate every bubble's position (X, Y, and Z)
        for (int i = 0; i < bubbleCount; i++)
        {
            RectTransform rt = bubbleTransforms[i];
            Vector3 targetPos = targetPositions[i];

            // Animate X and Y position
            Tween.UIAnchoredPosition(rt, new Vector2(targetPos.x, targetPos.y), swapDuration);

            // Animate Z position
            Tween.LocalPositionZ(rt, targetPos.z, swapDuration)
                .OnComplete(() => OnSwapComplete());
        }

        // 6. Update the coordinate list for the next click
        currentPositions = targetPositions;
    }

    private void OnSwapComplete()
    {
        completedSwaps++;

        if (completedSwaps >= bubbleCount)
        {
            // All animations complete. Ready for next swap.
        }
    }

    // --- Visual States (Alpha/Depth) ---

    private void UpdateVisualStates(bool instant, List<Vector3> futurePositions = null)
    {
        List<Vector3> positionsToAnalyze = futurePositions ?? currentPositions;

        int centerIndex = -1;
        float minZ = float.MaxValue;

        // Find the index of the bubble with the smallest Z value (closest to the camera/center)
        for (int i = 0; i < bubbleCount; i++)
        {
            if (positionsToAnalyze[i].z < minZ)
            {
                minZ = positionsToAnalyze[i].z;
                centerIndex = i;
            }
        }

        for (int i = 0; i < bubbleCount; i++)
        {
            RectTransform rt = bubbleTransforms[i];
            CanvasGroup cg = rt.GetComponent<CanvasGroup>();

            if (cg == null) continue;

            bool isCenter = (i == centerIndex);
            float duration = instant ? 0f : swapDuration;

            float targetAlpha = isCenter ? 1f : rearAlpha;

            if (Mathf.Approximately(cg.alpha, targetAlpha))
            {
                cg.blocksRaycasts = isCenter;
                continue;
            }

            if (duration > 0f)
            {
                Tween.Alpha(cg, targetAlpha, duration);
            }
            else
            {
                cg.alpha = targetAlpha;
            }

            // Only the center bubble should block raycasts (be clickable)
            cg.blocksRaycasts = isCenter;
        }
    }
}