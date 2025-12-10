using UnityEngine;
using UnityEngine.SceneManagement;
using PrimeTween;
using System.Collections; // Re-introduced for Coroutine

public class SceneTransitionHandler : MonoBehaviour
{
    // Ensure these fields are accessible (public or serialized private) in BubbleMenuAnimator
    [Header("References")]
    [Tooltip("Reference to the BubbleMenuAnimator for playing the exit sequence.")]
    [SerializeField] private BubbleMenuAnimator animator;

    [Tooltip("The CanvasGroup of the full-screen fader (BlackFader) in the Menu Scene.")]
    [SerializeField] private CanvasGroup faderGroup;

    [Header("Scene Settings")]
    [Tooltip("The name of the dedicated Loading Scene to transition to.")]
    [SerializeField] private string loadingSceneName = "LoadingScreenScene";

    [Tooltip("Duration of the black fade-in transition (to hide the scene switch).")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Tooltip("The delay between the bubbles starting their exit and the screen starting to fade to black.")]
    [SerializeField] private float fadeStartDelay = 0.2f;

    // Public method linked to the Ranked Bubble's Button component.
    public void LoadGameScene()
    {
        // --- 1. Pre-Flight Checks ---
        // ... (existing checks)

        // --- 2. Start Animations and Tween Setup ---
        animator.AnimateMenuOut();

        Tween fadeToBlackTween = Tween.Alpha(
            faderGroup,
            endValue: 1f,
            duration: fadeDuration,
            ease: Ease.OutCubic
        );

        // --- 3. Chain the Sequence ---
        Sequence.Create()
            .ChainDelay(fadeStartDelay)
            .Chain(fadeToBlackTween)

            // Step C: After the fade is complete, use the callback to start a Coroutine
            .ChainCallback(() =>
            {
                // Ensure the fader stays black
                faderGroup.blocksRaycasts = true;

                // Start the Coroutine on THIS object
                StartCoroutine(LoadSceneAfterSafetyDelay());
            });
    }

    // NEW/UPDATED METHOD: Waits a small, fixed duration to ensure PrimeTween completes its internal cleanup.
    private IEnumerator LoadSceneAfterSafetyDelay()
    {
        // Wait for a small, fixed duration (e.g., 0.1 seconds). 
        // This is much safer than relying on a single frame, especially if framerate drops.
        yield return new WaitForSeconds(1.0f);

        // Load the scene safely.
        SceneManager.LoadScene(loadingSceneName);
    }
}