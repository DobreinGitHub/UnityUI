using UnityEngine;
using UnityEngine.UI; // Required for the Button and Selectable classes

public class ExitConfirmationPopup : MonoBehaviour
{
    // These are the "slots" you drag the actual buttons into in the Inspector
    public Button exitButton;
    public Button closeButton; // The 'X' button
    public Button signOutButton;
    public GameObject popupWindow; // Drag your Popup_Window GameObject here

    public void ShowPopup()
    {
        // 1. Force the Canvas Group Alpha to 0 (Starting point)
        CanvasGroup cg = popupWindow.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 0f;
        }

        // 2. Activate the GameObject.
        // The Animator's Entry -> FadeIn_Popup transition will start immediately, 
        // smoothly animating the Alpha from 0 to 1.
        popupWindow.SetActive(true);

        // 3. Optional: If you want the exclusive focus to start immediately after the fade:
        // Call StartCoroutine(SelectButtonAfterDelay()); and add the coroutine below.
    }

    // --- Public Method to Open the Popup ---

    // This method is called by the button (e.g., "Quit Game") that triggers the popup
    public void OpenPopup()
    {
        // Turns the entire GameObject (the Popup_Window panel) ON
        gameObject.SetActive(true);
    }

    // --- Unity Lifecycle Methods ---

    // Called automatically by Unity when the GameObject is turned ON
    private void OnEnable()
    {
        // 1. SET DEFAULT FOCUS: Forces the Exit button to look highlighted/active immediately.
        if (exitButton != null)
        {
            exitButton.Select();
        }

        // 2. ADD LISTENERS: Connects the methods below to the actual button clicks.
        closeButton.onClick.AddListener(ClosePopup);
        exitButton.onClick.AddListener(OnExitClicked);
        signOutButton.onClick.AddListener(OnSignOutClicked);
    }

    // Called automatically by Unity when the GameObject is turned OFF
    private void OnDisable()
    {
        // 3. REMOVE LISTENERS: Important for cleaning up memory.
        closeButton.onClick.RemoveListener(ClosePopup);
        exitButton.onClick.RemoveListener(OnExitClicked);
        signOutButton.onClick.RemoveListener(OnSignOutClicked);
    }

    // --- Button Action Methods ---

    private void ClosePopup()
    {
        // Hides the popup window
        gameObject.SetActive(false);
        Debug.Log("Popup Closed.");
    }

    private void OnExitClicked()
    {
        Debug.Log("Exiting Application...");
        // This is the actual command to quit the game (only works in a built game)
        // Application.Quit(); 

        ClosePopup();
    }

    private void OnSignOutClicked()
    {
        Debug.Log("Signing Out User...");
        // Add code here to load the Login scene or reset user data.

        ClosePopup();
    }
}