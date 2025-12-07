using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class ExclusiveButtonFocus : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler
{
    // 1. The button component of the OTHER button (used for forcing selection transfer).
    public Selectable otherButton;

    // 2. The FX container for THIS button (used to turn THIS animation ON).
    public GameObject thisFxContainer;

    // 3. The FX container for the OTHER button (used to explicitly turn the OTHER animation OFF).
    public GameObject otherFxContainer;


    // --- 1. Forces Selection on Hover (Initiates the exclusive highlight) ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 1. Force the selection onto THIS button.
        gameObject.GetComponent<Selectable>().Select();
    }

    // --- 2. Turns THIS Animation ON (When this button gains focus) ---
    public void OnSelect(BaseEventData eventData)
    {
        // A. Turn THIS button's FX container ON.
        if (thisFxContainer != null)
        {
            thisFxContainer.SetActive(true);
        }

        // B. Explicitly turn the OTHER button's FX container OFF.
        // This handles cases where the other button might not fully Deselect instantly.
        if (otherFxContainer != null)
        {
            otherFxContainer.SetActive(false);
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        // A. Turn THIS button's FX container OFF (This is needed only if the other button was selected)
        // We keep this to ensure the animation turns off when focus moves to the other button.
        if (thisFxContainer != null)
        {
            thisFxContainer.SetActive(false);
        }

        // 🛑 B. THE FIX: Force the button to re-select itself after a tiny delay.
        // This allows the Event System to process the click on the background, 
        // but immediately puts the focus back on the button, restoring the Color Tint.
        StartCoroutine(ReSelect(gameObject.GetComponent<Selectable>()));
    }

    // New Coroutine: Re-selects the button after the current frame is finished.
    private IEnumerator ReSelect(Selectable button)
    {
        // Yielding null waits one frame.
        yield return null;

        // Only re-select if no other button has taken the selection.
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            button.Select();
        }
    }

    // NOTE: The OnDeselect logic is not strictly necessary for turning the OTHER FX off, 
    // because the OnSelect of the *next* button handles the hiding. 
    // The OnSelect method above is the definitive controller for the visibility.
}