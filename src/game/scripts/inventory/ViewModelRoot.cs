using Godot;

[GlobalClass]
public partial class ViewModelRoot : Node3D
{
    [Export] public Inventory inventory;
    private Node itemInstanceRef;

    public override void _Ready()
    {
        if (!IsMultiplayerAuthority() && !Multiplayer.IsServer()) return;
        
        if (inventory != null)
        {
            if (inventory.IsSynchronized()) OnInventorySynchronized();
            else inventory.Synchronized += OnInventorySynchronized;
        }
    }

    private void OnInventorySynchronized()
    {
        
        if (inventory.SelectedSlotIdx >= 0) OnInventorySlotSelected(inventory.SelectedSlotIdx);
        inventory.SlotSelected += OnInventorySlotSelected;
        
        inventory.Synchronized -= OnInventorySynchronized;
    }

    private void OnInventorySlotSelected(short idx)
    {
        FreeItemInstance();
        
        InventorySlot slot = inventory.GetSlot(idx);
        if (slot == null) return;
        if (slot.ItemStack == null) return;
        if (slot.ItemStack.ItemData == null) return;
        if (slot.ItemStack.ItemData.ViewModel == null) return;

        itemInstanceRef = slot.ItemStack.ItemData.ViewModel.InstantiateLocalView();  
        if (itemInstanceRef == null) return;

        itemInstanceRef.SetMultiplayerAuthority(GetMultiplayerAuthority());
        slot.ItemStack.ItemData.SetIn(itemInstanceRef);

        AddChild(itemInstanceRef);
    }

    private void OnInventorySlotDeselected(short idx)
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