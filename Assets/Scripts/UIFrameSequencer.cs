using UnityEngine;
using UnityEngine.UI;

public class UIFrameSequencer : MonoBehaviour
{
    // Drag ALL your sprite frames (in order) here in the Inspector
    public Sprite[] frames;
    public float framesPerSecond = 10f; // Adjust this for animation speed

    private Image uiImage;
    private int currentFrameIndex = 0;
    private float timeElapsed = 0f;
    private float frameDuration;

    private void Awake()
    {
        uiImage = GetComponent<Image>();
        frameDuration = 1f / framesPerSecond;
    }

    private void OnEnable()
    {
        // Reset the animation every time the object is turned ON (hovered)
        currentFrameIndex = 0;
        timeElapsed = 0f;

        // Ensure the first frame is visible immediately
        if (frames.Length > 0)
        {
            uiImage.sprite = frames[0];
        }
    }

    private void Update()
    {
        if (frames.Length == 0) return;

        timeElapsed += Time.deltaTime;

        if (timeElapsed >= frameDuration)
        {
            timeElapsed -= frameDuration;

            // Move to the next frame, wrapping back to the start (looping)
            currentFrameIndex = (currentFrameIndex + 1) % frames.Length;
            uiImage.sprite = frames[currentFrameIndex];
        }
    }
}