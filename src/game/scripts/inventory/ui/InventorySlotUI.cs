using Godot;
using Godot.Collections;

[GlobalClass]
public partial class InventorySlotUI : Control
{
    private InventorySlot _slot;
    private InventoryUI _inventoryUI;
    [Export] private TextureRect iconTextureRect;
    [Export] private Label quantityLabel;

    private ItemStack _currentItemStack;

    public void Init(InventoryUI inventoryUI, InventorySlot slot)
    {
        _inventoryUI = inventoryUI;
        _slot = slot;
    }

    public InventorySlot GetSlot() => _slot;

    public override void _Ready()
    {
        OnItemStackChanged();
        if (_slot != null)
        {
            _slot.ItemStackChanged += OnItemStackChanged;
        }
    }

    private void Update()
    {
        iconTextureRect.Texture = GetSlotIcon();
        quantityLabel.Text = GetQuantityText();
    }

    private void OnItemStackChanged()
    {
        if (_currentItemStack != null) _currentItemStack.QuantityChanged -= OnItemStackQuantityChanged;

        _currentItemStack = _slot.ItemStack;
        Update();

        if (_currentItemStack != null) _currentItemStack.QuantityChanged += OnItemStackQuantityChanged;
    }

    private void OnItemStackQuantityChanged()
    {
        quantityLabel.Text = GetQuantityText();
    }

    #region Drag/Drop
    private bool _isDragging;

    public override Variant _GetDragData(Vector2 atPosition)
    {
        Control previewContainer = new();
        TextureRect preview = new()
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,   
            Texture = _slot.ItemStack?.ItemData?.Icon,
            Size = Size * 0.8f,
        };

        previewContainer.AddChild(preview);
        previewContainer.Position = -atPosition;

        SetDragPreview(previewContainer);

        Dictionary data = [];
        data["inventory"] = _inventoryUI.Inventory.GetPath();
        data["slot_idx"] = _inventoryUI.Inventory.IndexOfSlot(_slot);

        return data;
    }


    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        //return data.Obj is long; // годот прикольно конвертирует int в long
        return true;
    }
    
    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (data.Obj is Dictionary dataDict)
        {
            NodePath fromInventory = dataDict["inventory"].As<NodePath>();
            short fromSlotIdx = dataDict["slot_idx"].AsInt16();
            short toSlotIdx = _inventoryUI.Inventory.IndexOfSlot(_slot);
            
            _inventoryUI.Inventory.RequestMoveItem(
                fromSlotIdx,
                toSlotIdx,
                fromInventory
            );

            GD.Print($"Moving: from {fromSlotIdx} to {toSlotIdx}");
        }
    }

    #endregion

    private string GetQuantityText()
    {
        if (_currentItemStack == null) return "";
        if (_currentItemStack.Quantity < 1) return "";

        return _currentItemStack.Quantity.ToString();
    }

    //H
    private Texture2D GetSlotIcon()
    {
        if (_currentItemStack == null) return null;
        if (_currentItemStack.ItemData == null) return null;

        //Test
        if (_currentItemStack.ItemData.Icon == null)
        {
            return ResourceLoader.Load<Texture2D>("res://textures/missing.png");
        }

        return _currentItemStack.ItemData.Icon;
    }
}
