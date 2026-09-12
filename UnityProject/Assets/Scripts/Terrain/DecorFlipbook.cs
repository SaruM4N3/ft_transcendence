using UnityEngine;

// Lightweight looping sprite animation for ambient decor - cycles a frame array on a timer instead
// of an Animator, which is unnecessary overhead at decor's hundreds-of-instances scale.
public class DecorFlipbook : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 10f;

    private float timer;
    private int frameIndex;

    void OnEnable()
    {
        timer = 0f;
        frameIndex = 0;
        if (frames.Length > 0)
            spriteRenderer.sprite = frames[0];
    }

    void Update()
    {
        if (frames.Length <= 1 || !spriteRenderer.isVisible)
            return;

        timer += Time.deltaTime;
        float frameDuration = 1f / frameRate;
        if (timer < frameDuration)
            return;

        timer -= frameDuration;
        frameIndex = (frameIndex + 1) % frames.Length;
        spriteRenderer.sprite = frames[frameIndex];
    }
}
