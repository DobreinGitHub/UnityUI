using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PrimeTween;
using System.Collections;
using System.Diagnostics;
using TMPro;

public class LoadingManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The CanvasGroup of the black screen element (Fader) in this Loading Scene.")]
    [SerializeField] private CanvasGroup faderGroup;

    [Tooltip("The UI Slider element used to display loading progress.")]
    [SerializeField] private Slider progressBar;

    [Header("Chomper Animation")]
    [Tooltip("The Image component of the animated Chomper object (should be the Slider's Handle Rect child).")]
    [SerializeField] private Image chomperImage;

    [Tooltip("An array of the Chomper's running sprites (f1, f2, f3, f4).")]
    [SerializeField] private Sprite[] runningFrames;

    // Pan Effect References
    [Header("Pan Effect References")]
    [Tooltip("The RectTransform of the background image.")]
    [SerializeField] private RectTransform backgroundRect;

    [Tooltip("The RectTransform of the Boy image.")]
    [SerializeField] private RectTransform boyRect;

    // Tooltip Fields
    [Header("Tooltip Settings")]
    [Tooltip("The UI Text component to display the random tooltip.")]
    [SerializeField] private TextMeshProUGUI tooltipText;

    [Tooltip("List of messages to display randomly.")]
    [SerializeField]
    private string[] tooltips = new string[]
    {
        "Tip 1: Always check your flanks!",
        "Tip 2: Elemental weaknesses are key.",
        "Tip 3: Don't forget to save your game!"
    };

    // Icon References
    [Header("Loading Icon References")]
    [Tooltip("The RectTransform of the Outer Diamond Icon.")]
    [SerializeField] private RectTransform outerIconRect;

    [Tooltip("The RectTransform of the Inner Diamond Icon.")]
    [SerializeField] private RectTransform innerIconRect;


    [Header("Pan Effect Settings")]
    [Tooltip("Duration (in seconds) for one half of the movement cycle. Higher = Slower.")]
    [SerializeField] private float panDuration = 25f;

    [Tooltip("The maximum local X-distance the background and boy will move.")]
    [SerializeField] private float panMovementRange = 5f;

    [Header("Rotation Settings - Manual")]
    [Tooltip("Speed in degrees per second for the outer icon (Clockwise is positive).")]
    [SerializeField] private float outerIconSpeed = 120f; // CW (e.g., 120 degrees/sec)

    [Tooltip("Speed in degrees per second for the inner icon (Counter-Clockwise is negative).")]
    [SerializeField] private float innerIconSpeed = -180f; // CCW (e.g., -180 degrees/sec)


    [Header("Settings")]
    [Tooltip("The actual scene to load (e.g., 'Game_UI').")]
    [SerializeField] private string targetGameScene = "Game_UI";

    [Tooltip("Duration of the fade-in (reveal) and fade-out (switch) transitions.")]
    [SerializeField] private float fadeDuration = 0.1f;

    [Tooltip("The minimum time (in seconds) the user must spend on the loading screen.")]
    [SerializeField] private float minLoadTime = 5.0f;

    [Tooltip("The number of frames per second for the running animation.")]
    [SerializeField] private float framesPerSecond = 10f;

    // Runtime data
    private Stopwatch loadTimer;
    private float frameTimer;
    private int currentFrameIndex = 0;

    // Variables to store the running infinite tweens
    private Tween backgroundTween;
    private Tween boyTween;

    // We are no longer using these for rotation, but keep them for organization
    private Tween outerIconTween;
    private Tween innerIconTween;


    void Start()
    {
        // 1. Core Initialization
        loadTimer = new Stopwatch();
        loadTimer.Start();

        if (progressBar != null)
        {
            progressBar.value = 0f;
        }

        // Initialize the Chomper's sprite and timer state
        if (chomperImage != null && runningFrames.Length > 0)
        {
            currentFrameIndex = 0;
            chomperImage.sprite = runningFrames[currentFrameIndex];
            frameTimer = 0f;
        }

        // 2. Start Parallax Pan animation (still using PrimeTween)
        StartPanAnimation();

        // 3. Display the random tooltip
        DisplayRandomTooltip();


        // 4. Handle Fade-In Logic
        if (faderGroup == null)
        {
            UnityEngine.Debug.LogError("Fader Group reference missing. Loading will proceed immediately.");
            StartCoroutine(LoadTargetSceneAsync());
            return;
        }

        faderGroup.alpha = 1f;
        faderGroup.blocksRaycasts = true;

        Tween.Alpha(faderGroup, endValue: 0f, duration: fadeDuration, ease: Ease.InCubic)
            .OnComplete(() =>
            {
                faderGroup.blocksRaycasts = false;
                StartCoroutine(LoadTargetSceneAsync());
            });
    }

    private void DisplayRandomTooltip()
    {
        if (tooltipText == null || tooltips.Length == 0)
        {
            UnityEngine.Debug.LogWarning("Tooltip Text component is missing or the tooltip list is empty.");
            return;
        }

        int randomIndex = Random.Range(0, tooltips.Length);
        tooltipText.text = tooltips[randomIndex];
    }

    private void StartPanAnimation()
    {
        int cycles = -1; // Infinite loop

        if (backgroundRect != null)
        {
            backgroundTween = Tween.LocalPositionX(
                backgroundRect,
                endValue: panMovementRange,
                duration: panDuration,
                ease: Ease.InOutSine,
                cycles: cycles,
                cycleMode: CycleMode.Yoyo
            );
        }

        if (boyRect != null)
        {
            boyTween = Tween.LocalPositionX(
                boyRect,
                endValue: -panMovementRange,
                duration: panDuration,
                ease: Ease.InOutSine,
                cycles: cycles,
                cycleMode: CycleMode.Yoyo
            );
        }
    }

    // The previous PrimeTween rotation method is now empty because we use Update()
    private void StartIconRotation()
    {
        // Rotation is now handled in Update()
    }


    void Update()
    {
        // --- 1. Chomper Animation Frame Swap ---
        if (loadTimer != null && loadTimer.IsRunning && chomperImage != null && runningFrames.Length > 0)
        {
            frameTimer += Time.deltaTime;
            float timePerFrame = 1f / framesPerSecond;

            if (frameTimer >= timePerFrame)
            {
                currentFrameIndex = (currentFrameIndex + 1) % runningFrames.Length;
                chomperImage.sprite = runningFrames[currentFrameIndex];

                frameTimer -= timePerFrame;
            }
        }

        // --- 2. Manual Diamond Rotation (Guaranteed to Work) ---
        if (loadTimer != null && loadTimer.IsRunning)
        {
            // Outer Icon: Rotate Clockwise (using outerIconSpeed)
            if (outerIconRect != null)
            {
                // RectTransform.Rotate(x, y, z) is the most robust way to rotate a UI element
                outerIconRect.Rotate(0f, 0f, outerIconSpeed * Time.deltaTime);
            }

            // Inner Icon: Rotate Counter-Clockwise (using innerIconSpeed)
            if (innerIconRect != null)
            {
                innerIconRect.Rotate(0f, 0f, innerIconSpeed * Time.deltaTime);
            }
        }
    }


    private IEnumerator LoadTargetSceneAsync()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(targetGameScene);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            // --- 1. Calculate Progress ---
            float actualProgress = Mathf.Clamp01(operation.progress / 0.9f);
            float timeProgress = Mathf.Clamp01((float)loadTimer.Elapsed.TotalSeconds / minLoadTime);
            float visualProgress = Mathf.Max(actualProgress, timeProgress);

            // --- 2. Update Slider Position ---
            if (progressBar != null)
            {
                progressBar.value = Mathf.Lerp(progressBar.value, visualProgress, Time.deltaTime * 3f);
            }

            // --- 3. Check Readiness and Transition ---
            bool sceneIsReady = operation.progress >= 0.9f;
            bool minTimeMet = loadTimer.Elapsed.TotalSeconds >= minLoadTime;

            if (sceneIsReady && minTimeMet)
            {
                loadTimer.Stop();

                // Finalize UI appearance
                if (progressBar != null)
                {
                    progressBar.value = 1f;
                }

                if (chomperImage != null && runningFrames.Length > 0)
                {
                    chomperImage.sprite = runningFrames[0];
                }

                // Stop the parallax animation
                if (backgroundTween.isAlive) backgroundTween.Stop();
                if (boyTween.isAlive) boyTween.Stop();

                // NO NEED TO STOP ROTATION, AS IT STOPS WHEN loadTimer.IsRunning IS FALSE

                // Start the fade-OUT to black before switching
                if (faderGroup != null)
                {
                    faderGroup.blocksRaycasts = true;

                    yield return Tween.Alpha(faderGroup, endValue: 1f, duration: fadeDuration, ease: Ease.OutCubic)
                        .ToYieldInstruction();
                }

                // Final step: Allow the fully loaded scene to become active and switch over
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}