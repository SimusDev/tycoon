using Godot;

[GlobalClass]
public partial class InventoryUI : Control
{
    [Export] public Inventory Inventory;

    [Export] private PackedScene _prefabSlotUI;
    
    [Export] private Container _container;
    [Export] private Label _labelTitle; 

    public override void _Ready()
    {
        if (Inventory.IsSlotsInitialized) Update();
        else Inventory.SlotsInitialized += OnSlotsInitialized;
    }

    private void OnSlotsInitialized()
    {
        Update();
        Inventory.SlotsInitialized -= OnSlotsInitialized;
    }

    private void Update()
    {
        _labelTitle.Text = $"Inventory ({Inventory?.GetOwnerId()})";
        Clear();


        if (Inventory != null)
        {
            Inventory.SlotAdded += OnSlotAdded;
            Inventory.SlotRemoved += OnSlotRemoved;

            if (_container != null)
            {
                for (short i = 0; i < Inventory.Slots.Count; i++)
                {
                    AddSlotUI(i);
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

    private InventorySlotUI AddSlotUI(short idx)
    {
        InventorySlotUI newSlot = _prefabSlotUI?.Instantiate<InventorySlotUI>();
        if (newSlot == null)
        {
            return null;
        }

        newSlot.Init(this, idx);
        _container.AddChild(newSlot);

        return newSlot;
    }

    private bool RemoveSlotUI(short idx)
    {
        if (_container == null) return false;

        InventorySlotUI removedSlot = null;
        foreach(Node node in _container.GetChildren())
        {
            if (node is InventorySlotUI inventorySlotUI)
            {
                if (inventorySlotUI.SlotIdx == idx)
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

    private void OnSlotAdded(short idx)
    {
        AddSlotUI(idx);
    }

    private void OnSlotRemoved(short idx)
    {
        RemoveSlotUI(idx);
    }

}