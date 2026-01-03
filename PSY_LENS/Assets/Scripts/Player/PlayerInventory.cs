// PlayerInventory.cs
// Stores picked-up item IDs + inventory toggle for UI.
// Uses a dedicated InventoryCamera in its own space.
// Shows the selected item's 3D model, placed at the center of ItemDisplayArea,
// in front of the UI, rotatable with camera-aligned axes.
// Supports stacking: one slot per item ID, with a count.
// Psy-Lens

using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [System.Serializable]
    public class ItemDefinition
    {
        public string id;                // must match PickupItem.itemId (internal key)
        public string displayName;       // shown as item name in UI
        [TextArea] public string description; // shown in details panel
        public GameObject displayPrefab; // 3D prefab to show in the inventory display

        [Header("Inventory Display")]
        [Range(0.1f, 2f)]
        public float displayScale = 1f;  // scale for the inspect model in inventory
    }

    [System.Serializable]
    public class InventoryEntry
    {
        public string itemId;
        public int count;
    }

    [Header("UI")]
    [SerializeField] private GameObject inventoryCanvas;    // root InventoryCanvas (Screen Space - Camera)
    [SerializeField] private RectTransform itemDisplayArea; // center of this rect = where model appears on screen

    [Header("Inventory Camera Space")]
    [SerializeField] private Camera previewCamera;          // InventoryCamera
    [SerializeField] private Transform inspectPivot;        // child of InventoryCamera
    [SerializeField] private float uiPlaneDistance = 10f;   // Canvas plane distance
    [SerializeField] private float modelDistance = 2f;      // model distance from camera (must be < uiPlaneDistance)

    [Header("Item Definitions")]
    [Tooltip("List of items that can be displayed in the inventory (id must match PickupItem.itemId).")]
    [SerializeField] private List<ItemDefinition> itemDefinitions = new List<ItemDefinition>();

    [Header("Debug")]
    [SerializeField] private bool logChanges = true;

    // runtime slots (one per itemId)
    [SerializeField] private List<InventoryEntry> entries = new List<InventoryEntry>();
    [SerializeField] private int selectedIndex = -1; // -1 = none

    private bool isOpen = false;
    private GameObject currentInspectInstance;
    private Canvas inventoryCanvasComponent;

    // read-only list of item IDs (one per slot)
    private readonly List<string> itemIdsBuffer = new List<string>();
    public IReadOnlyList<string> Items
    {
        get
        {
            itemIdsBuffer.Clear();
            for (int i = 0; i < entries.Count; i++)
                itemIdsBuffer.Add(entries[i].itemId);
            return itemIdsBuffer;
        }
    }

    private void Start()
    {
        if (inventoryCanvas != null)
        {
            inventoryCanvasComponent = inventoryCanvas.GetComponent<Canvas>();
            inventoryCanvas.SetActive(false); // start hidden
        }

        if (previewCamera != null)
            previewCamera.enabled = false;    // start off

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Time.timeScale = 1f;

        // Ensure canvas is Screen Space - Camera on the inventory camera
        if (inventoryCanvasComponent != null && previewCamera != null)
        {
            inventoryCanvasComponent.renderMode = RenderMode.ScreenSpaceCamera;
            inventoryCanvasComponent.worldCamera = previewCamera;
            inventoryCanvasComponent.planeDistance = uiPlaneDistance;
        }

        // Ensure pivot is parented and at the correct base distance
        if (previewCamera != null && inspectPivot != null)
        {
            inspectPivot.SetParent(previewCamera.transform, false);
            inspectPivot.localPosition = new Vector3(0f, 0f, modelDistance);
            inspectPivot.localRotation = Quaternion.identity;
            inspectPivot.localScale = Vector3.one;
        }
    }

    private void Update()
    {
        // While inventory is open, keep the inspect model's scale in sync
        // with the selected item's displayScale so you can tweak it live in Play mode.
        if (!isOpen || currentInspectInstance == null)
            return;

        if (TryGetSelectedDefinition(out var def) && def != null)
        {
            float scale = Mathf.Clamp(def.displayScale, 0.1f, 2f);
            currentInspectInstance.transform.localScale = Vector3.one * scale;
        }
    }

    // ---------------------------------------------------------
    // ITEM MANAGEMENT (STACKING)
    // ---------------------------------------------------------

    public void AddItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            Debug.LogWarning("[PlayerInventory] Tried to add empty item ID.");
            return;
        }

        // find existing slot
        int index = FindEntryIndex(itemId);
        if (index >= 0)
        {
            // increment count
            var entry = entries[index];
            entry.count++;
            entries[index] = entry;

            if (logChanges)
                Debug.Log($"[PlayerInventory] Added another {itemId}. Count = {entries[index].count}");
        }
        else
        {
            // new slot
            InventoryEntry entry = new InventoryEntry
            {
                itemId = itemId,
                count = 1
            };
            entries.Add(entry);
            index = entries.Count - 1;

            if (logChanges)
                Debug.Log($"[PlayerInventory] Added new item slot: {itemId} (count = 1)");

            // if nothing selected, select this new slot
            if (selectedIndex < 0)
                selectedIndex = index;
        }

        if (logChanges)
            DebugPrintContents();

        if (isOpen)
            UpdateInspectModelForSelected();
    }

    public bool HasItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return false;

        int index = FindEntryIndex(itemId);
        return index >= 0 && entries[index].count > 0;
    }

    public bool RemoveItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return false;

        int index = FindEntryIndex(itemId);
        if (index < 0)
            return false;

        InventoryEntry entry = entries[index];

        if (entry.count > 1)
        {
            entry.count--;
            entries[index] = entry;

            if (logChanges)
                Debug.Log($"[PlayerInventory] Removed one {itemId}. Count = {entry.count}");
        }
        else
        {
            // remove slot entirely
            entries.RemoveAt(index);

            if (logChanges)
                Debug.Log($"[PlayerInventory] Removed last {itemId}, slot removed.");

            // adjust selection
            if (entries.Count == 0)
            {
                selectedIndex = -1;
            }
            else if (selectedIndex >= entries.Count)
            {
                selectedIndex = entries.Count - 1;
            }
        }

        if (logChanges)
            DebugPrintContents();

        if (isOpen)
            UpdateInspectModelForSelected();

        return true;
    }

    private int FindEntryIndex(string itemId)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].itemId == itemId)
                return i;
        }
        return -1;
    }

    public void SelectNextItem()
    {
        if (entries.Count == 0)
        {
            selectedIndex = -1;
            UpdateInspectModelForSelected();
            return;
        }

        if (selectedIndex < 0)
        {
            selectedIndex = 0;
        }
        else
        {
            selectedIndex = (selectedIndex + 1) % entries.Count;
        }

        if (logChanges)
            Debug.Log($"[PlayerInventory] Next item selected: index = {selectedIndex}, id = {GetSelectedItemId()}");

        if (isOpen)
            UpdateInspectModelForSelected();
    }

    public void SelectPreviousItem()
    {
        if (entries.Count == 0)
        {
            selectedIndex = -1;
            UpdateInspectModelForSelected();
            return;
        }

        if (selectedIndex < 0)
        {
            selectedIndex = 0;
        }
        else
        {
            selectedIndex = (selectedIndex - 1 + entries.Count) % entries.Count;
        }

        if (logChanges)
            Debug.Log($"[PlayerInventory] Previous item selected: index = {selectedIndex}, id = {GetSelectedItemId()}");

        if (isOpen)
            UpdateInspectModelForSelected();
    }

    public string GetSelectedItemId()
    {
        if (selectedIndex < 0 || selectedIndex >= entries.Count)
            return null;

        return entries[selectedIndex].itemId;
    }

    public int GetSelectedCount()
    {
        if (selectedIndex < 0 || selectedIndex >= entries.Count)
            return 0;

        return entries[selectedIndex].count;
    }

    public int GetCountForItem(string itemId)
    {
        int index = FindEntryIndex(itemId);
        if (index < 0)
            return 0;

        return entries[index].count;
    }

    public void DebugPrintContents()
    {
        if (entries.Count == 0)
        {
            Debug.Log("[PlayerInventory] Inventory is empty.");
            return;
        }

        string msg = "[PlayerInventory] Contents:";
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            string marker = (i == selectedIndex) ? "  <SELECTED>" : "";
            msg += $"\n - [{i}] {e.itemId} x{e.count}{marker}";
        }

        Debug.Log(msg);
    }

    // ---------------------------------------------------------
    // LOOKUP FOR UI
    // ---------------------------------------------------------

    public bool TryGetDefinition(string itemId, out ItemDefinition def)
    {
        for (int i = 0; i < itemDefinitions.Count; i++)
        {
            var d = itemDefinitions[i];
            if (d != null && d.id == itemId)
            {
                def = d;
                return true;
            }
        }

        def = null;
        return false;
    }

    public bool TryGetSelectedDefinition(out ItemDefinition def)
    {
        string id = GetSelectedItemId();
        if (string.IsNullOrEmpty(id))
        {
            def = null;
            return false;
        }

        return TryGetDefinition(id, out def);
    }

    // ---------------------------------------------------------
    // TOGGLE INVENTORY
    // ---------------------------------------------------------

    public void ToggleInventory()
    {
        if (inventoryCanvas == null)
        {
            Debug.LogWarning("[PlayerInventory] ToggleInventory called but inventoryCanvas is not assigned.");
            return;
        }

        isOpen = !isOpen;

        Time.timeScale = isOpen ? 0f : 1f;

        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isOpen;

        inventoryCanvas.SetActive(isOpen);

        if (previewCamera != null)
            previewCamera.enabled = isOpen;

        if (isOpen)
        {
            PositionPivotAtDisplayArea();
            UpdateInspectModelForSelected();
        }
        else
        {
            ClearInspectModel();
        }

        if (logChanges)
            Debug.Log("[PlayerInventory] Inventory " + (isOpen ? "OPEN (paused)" : "CLOSED (unpaused)"));
    }

    private void PositionPivotAtDisplayArea()
    {
        if (previewCamera == null || inspectPivot == null)
            return;

        Vector3 screenPos;

        if (itemDisplayArea != null)
        {
            screenPos = RectTransformUtility.WorldToScreenPoint(previewCamera, itemDisplayArea.position);
        }
        else
        {
            screenPos = new Vector3(previewCamera.pixelWidth * 0.5f,
                                    previewCamera.pixelHeight * 0.5f,
                                    0f);
        }

        screenPos.z = modelDistance;

        Vector3 worldPos = previewCamera.ScreenToWorldPoint(screenPos);

        inspectPivot.position = worldPos;
        inspectPivot.rotation = previewCamera.transform.rotation;
        inspectPivot.localScale = Vector3.one;
    }

    // ---------------------------------------------------------
    // 3D DISPLAY
    // ---------------------------------------------------------

    private void UpdateInspectModelForSelected()
    {
        if (!isOpen)
            return;

        if (inspectPivot == null)
        {
            ClearInspectModel();
            return;
        }

        string id = GetSelectedItemId();
        if (string.IsNullOrEmpty(id))
        {
            ClearInspectModel();
            return;
        }

        if (!TryGetDefinition(id, out var def) || def.displayPrefab == null)
        {
            ClearInspectModel();
            return;
        }

        ClearInspectModel();

        // reset pivot rotation so each selected item starts from default orientation
        inspectPivot.localRotation = Quaternion.identity;

        currentInspectInstance = Instantiate(def.displayPrefab, inspectPivot.position, inspectPivot.rotation, inspectPivot);
        currentInspectInstance.transform.localPosition = Vector3.zero;
        currentInspectInstance.transform.localRotation = Quaternion.identity;

        // initial scale (will also be kept in sync in Update)
        float scale = Mathf.Clamp(def.displayScale, 0.1f, 2f);
        currentInspectInstance.transform.localScale = Vector3.one * scale;

        if (currentInspectInstance != null && previewCamera != null)
        {
            int layer = previewCamera.gameObject.layer;
            ApplyLayerRecursively(currentInspectInstance.transform, layer);
        }
    }

    private void ApplyLayerRecursively(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        for (int i = 0; i < t.childCount; i++)
            ApplyLayerRecursively(t.GetChild(i), layer);
    }

    private void ClearInspectModel()
    {
        if (currentInspectInstance != null)
        {
            Destroy(currentInspectInstance);
            currentInspectInstance = null;
        }
    }

    /// <summary>
    /// Rotate the inspect model around camera-aligned X and Y axes (called from UI).
    /// deltaDegrees.x = yaw, deltaDegrees.y = pitch.
    /// </summary>
    public void RotateInspect(Vector2 deltaDegrees)
    {
        if (inspectPivot == null || currentInspectInstance == null || previewCamera == null)
            return;

        Vector3 camUp    = previewCamera.transform.up;
        Vector3 camRight = previewCamera.transform.right;

        inspectPivot.Rotate(camUp,    deltaDegrees.x, Space.World);  // left/right
        inspectPivot.Rotate(camRight, deltaDegrees.y, Space.World);  // up/down
    }
}
