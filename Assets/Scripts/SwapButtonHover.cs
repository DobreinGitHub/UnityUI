using UnityEngine;
using UnityEngine.EventSystems;
using PrimeTween;

// This script must be attached to the RectTransform of the left and right arrow buttons.
public class SwapButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // === Configuration ===

    [Tooltip("Amount to offset the button by on hover (e.g., 10 for slight movement).")]
    public float hoverOffsetAmount = 10f;

    [Tooltip("Duration of the hover animation.")]
    public float hoverDuration = 0.15f;

    // Set in the inspector to determine if this is the left or right button
    [Header("Behavior")]
    public bool isRightButton = false;

    private RectTransform rectTransform;
    private Tween currentTween;
    private Vector2 originalPosition;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            originalPosition = rectTransform.anchoredPosition;
        }
    }

    // Called when the mouse pointer enters the UI element.
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Stop any currently running tween (like a previous hover or a return tween)
        currentTween.Stop();

        // Calculate the target X position
        float targetX;
        if (isRightButton)
        {
            // Right button moves slightly to the right
            targetX = originalPosition.x + hoverOffsetAmount;
        }
        else
        {
            // Left button moves slightly to the left
            targetX = originalPosition.x - hoverOffsetAmount;
        }

        // Animate the movement
        currentTween = Tween.UIAnchoredPositionX(
            rectTransform,
            targetX,
            hoverDuration,
            Ease.OutSine // Smooth deceleration
        );
    }

    // Called when the mouse pointer exits the UI element.
    public void OnPointerExit(PointerEventData eventData)
    {
        // Stop the current hover tween
        currentTween.Stop();

        // Animate the button back to its original position
        currentTween = Tween.UIAnchoredPosition(
            rectTransform,
            originalPosition,
            hoverDuration,
            Ease.OutSine
        );
    }

    void OnDisable()
    {
        // Ensure tweens stop and position resets when the object is disabled or destroyed
        currentTween.Stop();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalPosition;
        }
    }
}