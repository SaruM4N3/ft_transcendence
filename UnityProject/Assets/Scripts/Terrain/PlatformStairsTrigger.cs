using UnityEngine;

/// <summary>Marks which side of a stairs gap the player entered.</summary>
public class PlatformStairsTrigger : MonoBehaviour
{
    private ProceduralMapGenerator generator;
    private bool marksOnPlatform;

    public void Initialize(ProceduralMapGenerator mapGenerator, bool isOnPlatformSide)
    {
        generator = mapGenerator;
        marksOnPlatform = isOnPlatformSide;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        generator.SetPlayerOnPlatform(marksOnPlatform);
    }
}
