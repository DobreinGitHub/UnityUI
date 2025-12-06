using UnityEngine;
using UnityEngine.UI;

public class BobbingBubbleButton : MonoBehaviour
{
    // Adjust these in the Inspector to control the bobbing
    public float bobbingHeight = 10f; // How many pixels up and down it moves
    public float bobbingSpeed = 0.05f;   // How fast it bobs (1 = 1 cycle per second)

    private Vector2 startPosition;

    void Start()
    {
        // Store the original starting position of the bubble
        startPosition = GetComponent<RectTransform>().anchoredPosition;
    }

    void Update()
    {
        // Calculate the new Y position using a sine wave:
        // Time.time gives a continuous count of seconds since the game started.
        // Math.Sin creates a smooth oscillation between -1 and 1.
        float newY = startPosition.y + Mathf.Sin(Time.time * bobbingSpeed * Mathf.PI) * bobbingHeight;

        // Apply the new position
        GetComponent<RectTransform>().anchoredPosition = new Vector2(startPosition.x, newY);
    }
}

