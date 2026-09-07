using UnityEngine;

/// <summary>Lightweight looping sprite-sheet animation for ambient decor (trees, bushes, ...).
/// Cycles through a fixed frame array on a timer instead of using an Animator/Mecanim state
/// machine, which is unnecessary overhead at the hundreds-of-instances scale decor runs at.</summary>
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
