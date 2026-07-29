using Godot;
using Godot.Collections;

[GlobalClass]
public partial class ItemStack : Resource
{
    [Export] public ItemData ItemData;

    [Signal] public delegate void QuantityChangedEventHandler();
    private ushort _quantity = 1;
    [Export] public ushort Quantity
    {
        get => _quantity;
        set
        {
            _quantity = value;
            EmitSignal(SignalName.QuantityChanged);
        }
    }

    [Signal] public delegate void SkinIdChangedEventHandler();
    private ushort _skinId = 0;
    public ushort SkinId
    {
        get => _skinId;
        set
        {
            _skinId = value;
            EmitSignal(SignalName.SkinIdChanged);
        }
    }

    [Signal] public delegate void StackSizeChangedEventHandler();
    private short stackSize = -1;
    public short StackSize
    {
        get
        {
            if (stackSize == -1) return (short)ItemData?.ItemStackConfig?.StackSize;
            return stackSize;
        }
        set
        {
            stackSize = value;
        }
    }

    
    [Signal] public delegate void DataChangedEventHandler();
    [Signal] public delegate void DataKeyChangedEventHandler(string name);

    protected Dictionary<string, Variant> data = [];

    [Export] public Dictionary<string, Variant> Data
    {
        set
        {
            data = value;
            EmitSignal(SignalName.DataChanged);
        }

        get { return data; }
    }

    public void SetDataKey(string name, Variant value)
    {
        data[name] = value;
        EmitSignal(SignalName.DataKeyChanged, name);
    }

    public Dictionary<string, Variant> GetData() => data;
    public Variant GetDataValue(string name) => data[name];

    private static readonly GDNetBuffer _buffer = new();

    public void SetIn(Node node)
    {
        node.SetMeta("ItemStack", this);
    }

    public static ItemStack FindIn(Node node)
    {
        if (node.HasMeta("ItemData"))
        {
            return node.GetMeta("ItemStack").As<ItemStack>();
        }
        return null;
    }

    public static ItemStack CreateFrom(ItemData itemData)
    {
        if (itemData != null)
        {
            ItemStack itemStack = new()
            {
                ItemData = itemData
            };
            return itemStack;
        }
        return null;
    }


    public byte[] Serialize()
    {
        _buffer.Clear();
        
        _buffer.WriteResource(ItemData);
        _buffer.WriteUInt16(Quantity);
        _buffer.WriteUInt16(SkinId);
        //_buffer.WriteDictionarySimple(data);

        return _buffer.GetBytes();
    }

    public static ItemStack Deserialize(byte[] bytes)
    {
        _buffer.Clear();
        _buffer.SetBytes(bytes);

        ItemStack itemStack = new()
        {
            ItemData = _buffer.ReadResource<ItemData>(),
            _quantity = _buffer.ReadUInt16(),
            _skinId = _buffer.ReadUInt16(),
            //data = _buffer.ReadDictionarySimple<string, Variant>()
        };
        
        return itemStack;
    }
}
