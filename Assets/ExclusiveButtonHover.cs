using UnityEngine;
using UnityEngine.EventSystems; // Essential for IPointerEnterHandler
using UnityEngine.UI;

// We only need to implement IPointerEnterHandler to force the selection upon entry.
public class ExclusiveButtonFocus : MonoBehaviour, IPointerEnterHandler
{
    // We no longer need the 'otherButton' slot, but you can leave it for now
    // if you don't want to re-link your components.

    // Called automatically when the mouse moves over this button
    public void OnPointerEnter(PointerEventData eventData)
    {
        // When the mouse enters THIS button, tell the Event System to SELECT it.
        // This is the only line needed for the exclusive hover effect.
        // When this button is selected, the previously selected button automatically deselects.
        gameObject.GetComponent<Selectable>().Select();
    }

    // We REMOVE the OnPointerExit function completely!
    /*
    public void OnPointerExit(PointerEventData eventData)
    {
        // This is the code that was forcing the focus back to the Exit button.
        // We delete it!
    }
    */
}