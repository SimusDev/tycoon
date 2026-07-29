using Godot;
using Godot.Collections;

[GlobalClass]
public partial class InventorySlotUI : Control
{
    private short _slotIdx;
    public short SlotIdx
    {
        get => _slotIdx;
        set
        {
            _slotIdx = value;
            
            if (_inventoryUI.Inventory != null)
            {
                if (_inventoryUI.Inventory.TryGetSlot(_slotIdx, out var slot))
                {
                    _slot = slot;
                }
            }
        }
    }

    private InventorySlot _slot;
    public InventorySlot GetSlot() => _slot;

    private InventoryUI _inventoryUI;

    [Export] private TextureRect iconTextureRect;
    [Export] private Label quantityLabel;

    private ItemStack _currentItemStack;

    public void Init(InventoryUI inventoryUI, short slotIdx)
    {
        _inventoryUI = inventoryUI;
        _slotIdx = slotIdx;

        SubscribeToSlot();
        OnItemStackChanged();
    }

    public override void _Ready()
    {
        if (_slot != null)
        {
            SubscribeToSlot();
            OnItemStackChanged();
        }
    }

    private void SubscribeToSlot()
    {
        if (_slot == null) return;

        _slot.ItemStackChanged += OnItemStackChanged;
    }

    private void UnsubscribeFromSlot()
    {
        if (_slot == null) return;
        _slot.ItemStackChanged -= OnItemStackChanged;
    }

    public override void _ExitTree()
    {
        UnsubscribeFromSlot();

        if (_currentItemStack != null)
            _currentItemStack.QuantityChanged -= OnItemStackQuantityChanged;
    }

    private void RefreshUI()
    {
        if (iconTextureRect != null)
            iconTextureRect.Texture = GetSlotIcon();

        if (quantityLabel != null)
            quantityLabel.Text = GetQuantityText();
    }

    private void OnItemStackChanged()
    {
        if (_currentItemStack != null)
            _currentItemStack.QuantityChanged -= OnItemStackQuantityChanged;

        _currentItemStack = _slot?.ItemStack;

        RefreshUI();

        if (_currentItemStack != null)
            _currentItemStack.QuantityChanged += OnItemStackQuantityChanged;
    }

    private void OnItemStackQuantityChanged()
    {
        if (quantityLabel == null) return;
        quantityLabel.Text = GetQuantityText();
    }

    #region Drag/Drop
    public override Variant _GetDragData(Vector2 atPosition)
    {
        Control previewContainer = new();
        TextureRect preview = new()
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture = _slot?.ItemStack?.ItemData?.Icon,
            Size = Size * 0.8f,
        };

        previewContainer.AddChild(preview);
        previewContainer.Position = -atPosition;

        SetDragPreview(previewContainer);

        Dictionary data = new();
        data["inventory"] = _inventoryUI.Inventory.GetPath();
        data["slot_idx"] = _slotIdx;

        return data;
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data) => true;

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.Dictionary)
            return;

        if (data.Obj is Dictionary dataDict)
        {
            NodePath fromInventory = dataDict["inventory"].As<NodePath>();

            short fromSlotIdx = dataDict["slot_idx"].AsInt16();
            short toSlotIdx = _slotIdx;

            _inventoryUI.Inventory.RequestMoveItem(fromSlotIdx, toSlotIdx, fromInventory);
        }
    }
    #endregion

    private string GetQuantityText()
    {
        if (_currentItemStack == null) return "";
        if (_currentItemStack.Quantity <= 1) return "";
        return _currentItemStack.Quantity.ToString();
    }

    private Texture2D GetSlotIcon()
    {
        if (_currentItemStack == null) return null;
        var itemData = _currentItemStack.ItemData;
        if (itemData == null) return null;

        if (itemData.Icon == null)
            return ResourceLoader.Load<Texture2D>("res://textures/missing.png");

        return itemData.Icon;
    }
}
