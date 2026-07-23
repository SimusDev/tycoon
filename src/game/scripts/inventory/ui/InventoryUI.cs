using Godot;

[GlobalClass]
public partial class InventoryUI : Control
{
    [Export] public Inventory Inventory;

    [Export] private Container _container;
    [Export] private PackedScene _prefabSlotUI;


    public override void _Ready()
    {
        if (Inventory.IsSynchronized()) Update();
        else Inventory.Synchronized += OnInventorySynchronized;
    }

    private void OnInventorySynchronized()
    {
        Update();
        Inventory.Synchronized -= OnInventorySynchronized;
    }

    private void Update()
    {
        Clear();


        if (Inventory != null)
        {
            Inventory.SlotAdded += OnSlotAdded;
            Inventory.SlotRemoved += OnSlotRemoved;

            if (_container != null)
            {
                foreach (InventorySlot newSlot in Inventory.Slots)
                {
                    AddSlotUI(newSlot);
                }
            }

            
        }
    }

    private void Clear()
    {
        if (_container == null) return; 
        foreach (Node node in _container.GetChildren())
        {
            _container.RemoveChild(node);
            node.QueueFree();
        }
    }

    private InventorySlotUI AddSlotUI(InventorySlot slot)
    {

        InventorySlotUI newSlot = _prefabSlotUI?.Instantiate<InventorySlotUI>();
        if (newSlot == null)
        {
            return null;
        }

        newSlot.Init(this, slot);
        _container.AddChild(newSlot);

        return newSlot;
    }

    private bool RemoveSlotUI(InventorySlot slot)
    {
        if (_container == null) return false;

        InventorySlotUI removedSlot = null;
        foreach(Node node in _container.GetChildren())
        {
            if (node is InventorySlotUI inventorySlotUI)
            {
                if (inventorySlotUI.GetSlot() == slot)
                {
                    removedSlot = inventorySlotUI;
                    break;
                }
            }
        }

        if (removedSlot != null)
        {
            removedSlot.QueueFree();
            return true;
        }


        return false;
    }

    private void OnSlotAdded(InventorySlot slot)
    {
        AddSlotUI(slot);
    }

    private void OnSlotRemoved(InventorySlot slot)
    {
        RemoveSlotUI(slot);
    }

}