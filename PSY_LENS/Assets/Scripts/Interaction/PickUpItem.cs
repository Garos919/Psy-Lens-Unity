// PickupItem.cs
// Interactable that adds an item ID to the player's inventory
// Psy-Lens

using UnityEngine;

public class PickupItem : Interactable
{
    [Header("Pickup Settings")]
    [SerializeField] private string itemId = "Item_Example";
    [SerializeField] private bool destroyOnPickup = true;

    public override void Interact()
    {
        // base class may log, but we respect the IsInteractable flag here
        if (!IsInteractable)
        {
            Debug.Log($"[PickupItem] {name} is not interactable right now.");
            return;
        }

        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        if (inventory == null)
        {
            Debug.LogWarning("[PickupItem] No PlayerInventory found in scene.");
            return;
        }

        inventory.AddItem(itemId);

        // prevent further interactions
        IsInteractable = false;

        if (destroyOnPickup)
        {
            Debug.Log($"[PickupItem] {name} picked up and removed from scene.");
            gameObject.SetActive(false);
        }
        else
        {
            Debug.Log($"[PickupItem] {name} picked up but left in scene.");
        }
    }
}
