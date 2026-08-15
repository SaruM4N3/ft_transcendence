using UnityEngine;

public class CharacterCustomizationMenu : MonoBehaviour
{
    [SerializeField] private GameObject[] classPresets;

    public void SelectClass(int index)
    {
        if (index < 0 || index >= classPresets.Length || classPresets[index] == null)
            return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
            return;

        GameObject preset = classPresets[index];
        // Assigning the controller re-applies the animator's current (old) frame synchronously,
        // so the sprite must be set last or it gets clobbered.
        player.GetComponent<Animator>().runtimeAnimatorController = preset.GetComponent<Animator>().runtimeAnimatorController;
        player.GetComponent<SpriteRenderer>().sprite = preset.GetComponent<SpriteRenderer>().sprite;
    }
}
