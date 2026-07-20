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
            Send(nameof(_quantity), value);
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
            Send(nameof(_skinId), value);
            EmitSignal(SignalName.SkinIdChanged);
        }
    }
    
    [Signal] public delegate void DataChangedEventHandler();
    [Export] protected Dictionary data = [];
    public void SetData(string name, Variant value)
    {
        data[name] = value;
        Send(nameof(data), name, value);
        EmitSignal(SignalName.DataChanged);
    }
    public Variant GetData(string name) => data[name];

    private static GDNetBuffer _buffer = new();
    private GDNetCommunicator _communicator = new();
    
    private long _netId = GDNet.GenerateUniqueID();

    public ItemStack()
    {
        _communicator.OnBytesReceived += OnBytesReceived;
        _communicator.SynchronizeNetworkIDByUniqueID(_netId);
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
        if (!GameServer.Instance.Multiplayer.IsServer()) return;

        _buffer.Clear();
        _buffer.WriteUInt8(0); // Default
        _buffer.WriteString(propertyName);
        _buffer.WriteVar(value);
        
        _communicator.SendToAll(_buffer.GetBytes());
    }
    
    private void Send(string dictName, string propertyName, Variant value)
    {
        if (!GameServer.Instance.Multiplayer.IsServer()) return;

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
        _buffer.Clear();
        
        _buffer.WriteResource(ItemData);
        _buffer.WriteUInt16(Quantity);
        _buffer.WriteUInt16(SkinId);
        _buffer.WriteVar(data);

        _buffer.WriteLong(_netId);
        
        return _buffer.GetBytes();
    }

    public static ItemStack Deserialize(byte[] bytes)
    {
        _buffer.Clear();
        _buffer.SetBytes(bytes);

        ItemStack item = new()
        {
            ItemData = _buffer.ReadResource<ItemData>(),
            Quantity = _buffer.ReadUInt16(),
            SkinId = _buffer.ReadUInt16(),
            data = _buffer.ReadVar().AsGodotDictionary()
        };
        
        item._communicator.SynchronizeNetworkIDByUniqueID(_buffer.ReadLong());
        return item;
    }
}
