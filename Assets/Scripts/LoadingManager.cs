using UnityEngine;
using UnityEngine.SceneManagement;
using PrimeTween;
using System.Collections;
using System.Diagnostics;
using UnityEngine.UI; // NEW: Required for Slider component

public class LoadingManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The CanvasGroup of the black screen element (Fader) in this Loading Scene.")]
    [SerializeField] private CanvasGroup faderGroup;

    [Tooltip("The UI Slider element used to display loading progress.")]
    [SerializeField] private Slider progressBar; // NEW FIELD: Reference to the UI Slider

    [Header("Settings")]
    [Tooltip("The actual scene to load (e.g., 'InGameUIScene').")]
    [SerializeField] private string targetGameScene = "InGameUIScene";

    [Tooltip("Duration of the fade-in (reveal) and fade-out (switch) transitions.")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Tooltip("The minimum time (in seconds) the user must spend on the loading screen.")]
    [SerializeField] private float minLoadTime = 3.0f;

    // Runtime data
    private Stopwatch loadTimer;

    void Start()
    {
        if (faderGroup == null)
        {
            UnityEngine.Debug.LogError("Fader Group reference missing on LoadingManager. Loading will proceed without fade.");
            StartCoroutine(LoadTargetSceneAsync());
            return;
        }

        // Initialize and start the timer right when the scene begins
        loadTimer = new Stopwatch();
        loadTimer.Start();

        // Ensure the progress bar starts at 0
        if (progressBar != null)
        {
            progressBar.value = 0f;
        }

        // 1. Scene starts with a black screen (Alpha = 1 from previous scene's transition).
        faderGroup.alpha = 1f;
        faderGroup.blocksRaycasts = true;

        // 2. Fade IN to reveal the Loading Screen content
        Tween.Alpha(faderGroup, endValue: 0f, duration: fadeDuration, ease: Ease.InCubic)
            .OnComplete(() =>
            {
                // Stop blocking input and start the heavy lifting
                faderGroup.blocksRaycasts = false;
                StartCoroutine(LoadTargetSceneAsync());
            });
    }

    private IEnumerator LoadTargetSceneAsync()
    {
        // Start the asynchronous loading operation for the final scene
        AsyncOperation operation = SceneManager.LoadSceneAsync(targetGameScene);

        // Prevent the new scene from activating immediately upon completion (at 90% progress)
        operation.allowSceneActivation = false;

        // Loop while the scene is loading OR the minimum time hasn't passed
        while (!operation.isDone)
        {
            // The loading process is split into three checks and a visual update:

            // 1. Get the actual file loading progress (normalized to 0-1)
            // Note: Load is complete at 0.9f, so we normalize progress across that range.
            float actualProgress = Mathf.Clamp01(operation.progress / 0.9f);

            // 2. Get the time-based progress (normalized to 0-1)
            float timeProgress = Mathf.Clamp01((float)loadTimer.Elapsed.TotalSeconds / minLoadTime);

            // 3. Update the Progress Bar
            if (progressBar != null)
            {
                // The visual progress is the MAXIMUM of the two values.
                // This ensures the bar only moves forward once BOTH the minimum time AND the file load are completed.
                // It moves based on whichever is currently the bottleneck (time or file load).
                float visualProgress = Mathf.Max(actualProgress, timeProgress);

                // Use a slight Lerp for smooth animation, even if the progress jumps instantly
                progressBar.value = Mathf.Lerp(progressBar.value, visualProgress, Time.deltaTime * 3f);
            }


            // 4. Check readiness conditions
            bool sceneIsReady = operation.progress >= 0.9f;
            bool minTimeMet = loadTimer.Elapsed.TotalSeconds >= minLoadTime;

            // If BOTH conditions are met, we are ready for the final transition
            if (sceneIsReady && minTimeMet)
            {
                loadTimer.Stop();

                // Ensure the progress bar visually hits 100% just before the fade out
                if (progressBar != null)
                {
                    progressBar.value = 1f;
                }

                // Start the fade-OUT to black before switching
                if (faderGroup != null)
                {
                    faderGroup.blocksRaycasts = true; // Block input during final fade

                    // Fade back out to black (Alpha = 1)
                    yield return Tween.Alpha(faderGroup, endValue: 1f, duration: fadeDuration, ease: Ease.OutCubic)
                        .ToYieldInstruction();
                }

                // Final step: Allow the fully loaded scene to become active and switch over
                operation.allowSceneActivation = true;
            }

            yield return null; // Wait for the next frame
        }
    }
}