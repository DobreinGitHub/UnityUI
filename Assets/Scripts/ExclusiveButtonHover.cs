using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class ExclusiveButtonFocus : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler
{
    [Header("Core Focus Control")]
    public Selectable otherButton;

    [Header("FX Layer Control")]
    public GameObject thisFxContainer;
    public GameObject otherFxContainer;

    private PrimeOutlineFX thisPrimeFX;
    private PrimeOutlineFX otherPrimeFX;

    void Awake()
    {
        if (thisFxContainer != null)
        {
            thisPrimeFX = thisFxContainer.GetComponent<PrimeOutlineFX>();
        }
        if (otherFxContainer != null)
        {
            otherPrimeFX = otherFxContainer.GetComponent<PrimeOutlineFX>();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        gameObject.GetComponent<Selectable>().Select();
    }

    public void OnSelect(BaseEventData eventData)
    {
        // C. STOP the OTHER button's PrimeTween animation and hide it
        if (otherPrimeFX != null)
        {
            otherPrimeFX.StopAnimation();
            otherFxContainer.SetActive(false);
        }

        // A. Set THIS container ACTIVE immediately.
        if (thisFxContainer != null)
        {
            thisFxContainer.SetActive(true);
        }

        // B. FIX: Start the animation on the next frame to avoid the "Tween on inactive target" warning.
        if (thisPrimeFX != null)
        {
            StartCoroutine(StartFXCoroutine(thisPrimeFX));
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        // A. STOP THIS button's PrimeTween animation
        if (thisPrimeFX != null)
        {
            thisPrimeFX.StopAnimation();
            thisFxContainer.SetActive(false);
        }

        // B. Lock the highlight back onto this button if the user clicked outside.
        StartCoroutine(ReSelect(gameObject.GetComponent<Selectable>()));
    }

    // --- Coroutines ---

    private IEnumerator StartFXCoroutine(PrimeOutlineFX fxScript)
    {
        yield return null; // Wait one frame
        fxScript.PlayAnimation();
    }

    private IEnumerator ReSelect(Selectable button)
    {
        yield return null;
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            button.Select();
        }
    }
}