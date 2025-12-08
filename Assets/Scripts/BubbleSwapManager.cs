using UnityEngine;
using PrimeTween;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BubbleSwapManager : MonoBehaviour
{
    // === CONFIGURATION ===

    [Tooltip("List of all bubble containers (RectTransforms) in order.")]
    public RectTransform[] bubbleTransforms;

    [Tooltip("Duration for the smooth position swap animation.")]
    public float swapDuration = 0.3f;

    [Header("Cooldown Settings")]
    public float swapCooldown = 1.0f;

    [Header("Visual Depth Settings")]
    public float rearAlpha = 0.8f;

    // === RUNTIME DATA ===
    private List<Vector3> currentPositions;
    private int bubbleCount;

    // CRITICAL: Reference the independent float controller script
    private BubbleFloaterIndependent[] floaters;

    private int completedSwaps = 0;
    private float nextSwapTime = 0f;
    private int centerBubbleIndex = -1;

    void Awake()
    {
        if (bubbleTransforms == null || bubbleTransforms.Length == 0) return;

        bubbleCount = bubbleTransforms.Length;
        currentPositions = new List<Vector3>();
        // Initialize the floater array
        floaters = new BubbleFloaterIndependent[bubbleCount];

        for (int i = 0; i < bubbleCount; i++)
        {
            var rt = bubbleTransforms[i];
            currentPositions.Add(new Vector3(rt.anchoredPosition.x, rt.anchoredPosition.y, rt.localPosition.z));

            // GET THE FLOATER SCRIPT (Must be attached to the same object as the RectTransform)
            floaters[i] = rt.GetComponent<BubbleFloaterIndependent>();

            if (rt.GetComponent<CanvasGroup>() == null)
            {
                rt.gameObject.AddComponent<CanvasGroup>();
            }
        }

        UpdateVisualStates(instant: true);
    }

    void Update()
    {
        // === KEYBOARD INPUT CHECKING ===
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.rightArrowKey.wasPressedThisFrame) PerformSwap(isRight: true);
        if (keyboard.leftArrowKey.wasPressedThisFrame) PerformSwap(isRight: false);
    }

    // === PUBLIC FUNCTIONS ===
    public void SwapLeft() => PerformSwap(isRight: false);
    public void SwapRight() => PerformSwap(isRight: true);

    private void PerformSwap(bool isRight)
    {
        if (Time.time < nextSwapTime) return;
        nextSwapTime = Time.time + swapCooldown;

        // 1. STOP FLOATING & RESET OFFSET FOR ALL BUBBLES
        foreach (var floater in floaters)
        {
            // This calls BubbleFloaterIndependent.StopFloat(), resetting the child position to (0,0)
            floater?.StopFloat();
        }

        // 2. Calculate Targets (Shift list)
        List<Vector3> targetPositions = new List<Vector3>(currentPositions);
        if (isRight)
        {
            Vector3 temp = targetPositions[0];
            targetPositions.RemoveAt(0);
            targetPositions.Add(temp);
        }
        else
        {
            Vector3 temp = targetPositions[targetPositions.Count - 1];
            targetPositions.RemoveAt(targetPositions.Count - 1);
            targetPositions.Insert(0, temp);
        }

        // 3. Force Layout Update
        foreach (var rt in bubbleTransforms)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        completedSwaps = 0;

        // 4. Animate Visuals
        UpdateVisualStates(instant: false, targetPositions);

        // 5. Animate Positions (Parent movement)
        for (int i = 0; i < bubbleCount; i++)
        {
            RectTransform rt = bubbleTransforms[i];
            Vector3 targetPos = targetPositions[i];

            Tween.UIAnchoredPosition(rt, new Vector2(targetPos.x, targetPos.y), swapDuration);
            Tween.LocalPositionZ(rt, targetPos.z, swapDuration)
                .OnComplete(() => OnSwapComplete());
        }

        currentPositions = targetPositions;
    }

    private void OnSwapComplete()
    {
        completedSwaps++;

        if (completedSwaps >= bubbleCount)
        {
            // 6. RESTART FLOATING after swap is totally done
            foreach (var floater in floaters)
            {
                // This calls BubbleFloaterIndependent.StartFloat(), re-engaging the animation
                floater?.StartFloat();
            }
        }
    }

    private void UpdateVisualStates(bool instant, List<Vector3> futurePositions = null)
    {
        List<Vector3> positionsToAnalyze = futurePositions ?? currentPositions;
        int newCenterIndex = -1;
        float minZ = float.MaxValue;

        for (int i = 0; i < bubbleCount; i++)
        {
            if (positionsToAnalyze[i].z < minZ)
            {
                minZ = positionsToAnalyze[i].z;
                newCenterIndex = i;
            }
        }

        centerBubbleIndex = newCenterIndex;

        for (int i = 0; i < bubbleCount; i++)
        {
            RectTransform rt = bubbleTransforms[i];
            CanvasGroup cg = rt.GetComponent<CanvasGroup>();
            if (cg == null) continue;

            bool isCenter = (i == centerBubbleIndex);
            float duration = instant ? 0f : swapDuration;
            float targetAlpha = isCenter ? 1f : rearAlpha;

            // FIX: Prevent redundant tween warnings
            if (Mathf.Approximately(cg.alpha, targetAlpha))
            {
                cg.blocksRaycasts = isCenter;
                continue;
            }

            if (duration > 0f) Tween.Alpha(cg, targetAlpha, duration);
            else cg.alpha = targetAlpha;

            cg.blocksRaycasts = isCenter;
        }
    }
}