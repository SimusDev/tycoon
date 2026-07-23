using System;
using Godot;

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
        _isDragging = true;

        return _slot;
    }

    public override void _Notification(int what)
    {
        return;
        if (!_isDragging) return;

        if (what == NotificationDragEnd)
        {
            iconTextureRect.Show();
        }
            
        else if (what == NotificationDragBegin)
        {
            iconTextureRect.Hide();
        }
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        return data.Obj is InventorySlot;
    }
    
    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (data.Obj is InventorySlot slot)
        {
            _inventoryUI.Inventory.MoveItem(slot, _slot);
        }
        iconTextureRect.Show();
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
