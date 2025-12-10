using UnityEngine;
using UnityEngine.SceneManagement;
using PrimeTween;

public class ExitToMenuHandler : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The CanvasGroup of the black screen fader in this scene.")]
    [SerializeField] private CanvasGroup faderGroup;

    [Header("Settings")]
    [Tooltip("The name of the main menu scene to return to.")]
    [SerializeField] private string mainMenuSceneName = "Main_Menu";

    [Tooltip("Duration of the fade-out transition.")]
    [SerializeField] private float fadeDuration = 0.5f;

    // This method is called by the UI Button's OnClick() event
    public void ReturnToMainMenu()
    {
        if (faderGroup == null)
        {
            UnityEngine.Debug.LogError("Fader Group reference missing. Cannot play fade-out transition!");
            // Fallback: load synchronously without fade
            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        // 1. Block all further input immediately
        faderGroup.blocksRaycasts = true;

        // 2. Fade the screen to black (Alpha = 1)
        Tween.Alpha(faderGroup, endValue: 1f, duration: fadeDuration, ease: Ease.OutCubic)
            .OnComplete(() =>
            {
                // 3. Once the screen is black, load the target scene synchronously
                SceneManager.LoadScene(mainMenuSceneName);
            });
    }
}
