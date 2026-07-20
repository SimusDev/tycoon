using Godot;

[GlobalClass]
public partial class InventoryUI : Control
{
    private Inventory _inventory;
    [Export] public Inventory Inventory
    {
        get => _inventory;
        set
        {
            Inventory old = _inventory;
            _inventory = value;
            if (IsNodeReady()) Update(old);
        }
    }

    [Export] private Container _container;
    [Export] private PackedScene _prefabSlotUI;


    public override void _Ready()
    {
        if (_inventory.IsSynchronized()) Update();
        else _inventory.Synchronized += OnInventorySynchronized;
    }

    private void OnInventorySynchronized()
    {
        Update();
        _inventory.Synchronized -= OnInventorySynchronized;
    }

    private void Update(Inventory old = null)
    {
        Clear();

        if (old != null)
        {
            old.SlotAdded -= OnSlotAdded;
            old.SlotRemoved -= OnSlotRemoved;

            //foreach (InventorySlot oldSlot in old.Slots)
            
        }

        if (_inventory != null)
        {
            _inventory.SlotAdded += OnSlotAdded;
            _inventory.SlotRemoved += OnSlotRemoved;

            if (_container != null)
            {
                foreach (InventorySlot newSlot in _inventory.Slots)
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

        newSlot.Init(slot);
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