using System.Reflection.Metadata.Ecma335;
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
            Send(nameof(Quantity), value);
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
            Send(nameof(SkinId), value);
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
    [Export] protected Dictionary<string, Variant> data = [];
    public void SetData(string name, Variant value)
    {
        data[name] = value;
        Send(nameof(data), name, value);
        EmitSignal(SignalName.DataChanged);
    }
    public Dictionary<string, Variant> GetData() => data;
    public Variant GetDataValue(string name) => data[name];

    private static readonly GDNetBuffer _buffer = new();
    private static readonly GDNetBuffer _s_buffer = new();

    [Export] private GDNetCommunicator _communicator = new();
    
    private long _netId = GDNet.GenerateUniqueID();

    public ItemStack()
    {
        _communicator.OnBytesReceived += OnBytesReceived;
        _communicator.SynchronizeNetworkIDByUniqueID(_netId);
    }

    public ItemStack(long netId)
    {
        _communicator.OnBytesReceived += OnBytesReceived;
        _communicator.SynchronizeNetworkIDByUniqueID(netId);
    }

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


    private void Send(string propertyName, Variant value)
    {
        if (!GameServer.IsMultiplayerValid() || !GameServer.Instance.Multiplayer.IsServer()) return;

        _buffer.Clear();
        _buffer.WriteUInt8(0); // Default
        _buffer.WriteString(propertyName);
        _buffer.WriteVar(value);
        
        _communicator.SendToAll(_buffer.GetBytes());
    }
    
    private void Send(string dictName, string propertyName, Variant value)
    {
        if (!GameServer.IsMultiplayerValid() || !GameServer.Instance.Multiplayer.IsServer()) return;

        _buffer.Clear();
        _buffer.WriteUInt8(1); // Dictionary
        _buffer.WriteString(dictName);
        _buffer.WriteString(propertyName);
        _buffer.WriteVar(value);
        
        _communicator.SendToAll(_buffer.GetBytes());
    }
    
    private void OnBytesReceived(int peer, byte[] bytes)
    {
        if (peer != GDNet.ServerID) return;

        _buffer.SetBytes(bytes);
        _buffer.Seek(0);
        byte type = _buffer.ReadUInt8();
        switch (type)
        {
            case 0: // Default
                Set(_buffer.ReadString(), _buffer.ReadVar());
                break;
            case 1: // Dictionary
                Get(_buffer.ReadString()).AsGodotDictionary()[_buffer.ReadString()] = _buffer.ReadVar();
                break;
        }
    }


    public byte[] Serialize()
    {
        _s_buffer.Clear();
        
        _s_buffer.WriteLongVar(_netId);
        _s_buffer.WriteResource(ItemData);
        _s_buffer.WriteUInt16(Quantity);
        _s_buffer.WriteUInt16(SkinId);
        //_buffer.WriteDictionarySimple(data);

        return _s_buffer.GetBytes();
    }

    public static ItemStack Deserialize(byte[] bytes)
    {
        _s_buffer.Clear();
        _s_buffer.SetBytes(bytes);

        ItemStack itemStack = new(_s_buffer.ReadLongVar())
        {
            ItemData = _s_buffer.ReadResource<ItemData>(),
            Quantity = _s_buffer.ReadUInt16(),
            SkinId = _s_buffer.ReadUInt16(),
            //data = _buffer.ReadDictionarySimple<string, Variant>()
        };
        
        return itemStack;
    }
}
