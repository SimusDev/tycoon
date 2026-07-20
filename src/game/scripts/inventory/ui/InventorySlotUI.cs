using System.Runtime.InteropServices.Marshalling;
using Godot;

[GlobalClass]
public partial class InventorySlotUI : Control
{
    private InventorySlot _slot;
    [Export] private TextureRect iconTextureRect;
    [Export] private Label quantityLabel;

    private ItemStack _currentItemStack;

    public void Init(InventorySlot slot)
    {
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
