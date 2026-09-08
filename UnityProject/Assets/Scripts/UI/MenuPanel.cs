using UnityEngine;

public class MenuPanel : MonoBehaviour
{
    public void Open()
    {
        gameObject.SetActive(true);
        PauseManager.SetExternalPause(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        PauseManager.SetExternalPause(false);
    }
}
