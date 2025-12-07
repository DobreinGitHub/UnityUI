using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Menu References")]
    public GameObject mainMenuContainerPanel;
    private CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    public float entranceDuration = 1.0f;
    public float startDelay = 1.0f;

    [Header("Positioning")]
    // Adjust this value in the Inspector to raise the starting height:
    // 0 means start one full screen height below center.
    // 500 means start 500 pixels higher than that.
    public float startOffsetY = 0f;


    void Start()
    {
        canvasGroup = mainMenuContainerPanel.GetComponent<CanvasGroup>();

        if (mainMenuContainerPanel != null && canvasGroup != null)
        {
            mainMenuContainerPanel.SetActive(false);
            canvasGroup.alpha = 0f;
        }

        Invoke("OpenMainMenu", startDelay);
    }


    public void OpenMainMenu()
    {
        if (mainMenuContainerPanel != null && !mainMenuContainerPanel.activeSelf)
        {
            mainMenuContainerPanel.SetActive(true);
            StartCoroutine(AnimateMenuEntrance());
        }
    }


    private IEnumerator AnimateMenuEntrance()
    {
        RectTransform rectT = mainMenuContainerPanel.GetComponent<RectTransform>();

        // Calculate start position using the new offset variable
        // We start below the screen height, but move up by the offset amount
        Vector2 startPos = new Vector2(rectT.anchoredPosition.x, -Screen.height + startOffsetY);
        Vector2 endPos = Vector2.zero; // Center of the canvas

        rectT.anchoredPosition = startPos;
        canvasGroup.alpha = 0f;

        float elapsedTime = 0;

        while (elapsedTime < entranceDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / entranceDuration);

            // Animate Position:
            rectT.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

            // Animate Alpha (Fade In):
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rectT.anchoredPosition = endPos;
        canvasGroup.alpha = 1f;
    }
}

