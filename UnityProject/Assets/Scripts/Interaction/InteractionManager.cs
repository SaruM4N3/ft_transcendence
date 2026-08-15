using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class InteractionManager : MonoBehaviour
{
    private static readonly List<InteractableZone> ActiveZones = new();

    private InteractableZone currentZone;
    private PlayerInput playerInput;
    private InputAction interactAction;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        interactAction = playerInput.actions.FindAction("Interact");
        if (interactAction != null)
            interactAction.performed += OnInteractPerformed;
    }

    private void OnDisable()
    {
        if (interactAction != null)
            interactAction.performed -= OnInteractPerformed;

        if (currentZone != null)
            InteractPromptUI.Instance?.Hide();
        currentZone = null;
    }

    private void Update()
    {
        InteractableZone nearest = GetNearestZone();
        if (nearest != currentZone)
        {
            currentZone = nearest;
            if (currentZone != null)
                InteractPromptUI.Instance?.Show(currentZone.PromptText, currentZone.PromptPosition);
            else
                InteractPromptUI.Instance?.Hide();
        }
        else if (currentZone != null)
        {
            InteractPromptUI.Instance?.UpdatePosition(currentZone.PromptPosition);
        }
    }

    private InteractableZone GetNearestZone()
    {
        InteractableZone nearest = null;
        float bestSqrDist = float.MaxValue;

        foreach (InteractableZone zone in ActiveZones)
        {
            float sqrDist = (zone.PromptPosition - transform.position).sqrMagnitude;
            if (sqrDist < bestSqrDist)
            {
                bestSqrDist = sqrDist;
                nearest = zone;
            }
        }

        return nearest;
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (PauseManager.IsPaused)
            return;

        currentZone?.Interact();
    }

    public static void Register(InteractableZone zone)
    {
        if (!ActiveZones.Contains(zone))
            ActiveZones.Add(zone);
    }

    public static void Unregister(InteractableZone zone)
    {
        ActiveZones.Remove(zone);
    }
}
