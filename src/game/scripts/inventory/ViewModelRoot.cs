using Godot;

[GlobalClass]
public partial class ViewModelRoot : Node3D
{
    [Export] public Inventory inventory;
    private Node itemInstanceRef;

    public override void _Ready()
    {
        if (inventory != null)
        {
            inventory.SlotSelected += OnInventorySlotSelected;
            inventory.SlotDeselected += OnInventorySlotDeselected;
        }
    }

    private void OnInventorySlotSelected(int idx)
    {
        FreeItemInstance();
        
        InventorySlot slot = inventory.GetSlot(idx);
        if (slot == null) return;

        
    }

    private void OnInventorySlotDeselected(int idx)
    {
        FreeItemInstance();
    }

    private void FreeItemInstance()
    {
        if (IsInstanceValid(itemInstanceRef))
        {
            RemoveChild(itemInstanceRef);
            itemInstanceRef.QueueFree();
        }
    }
    
}