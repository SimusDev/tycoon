
using Godot;
using Godot.Collections;
using System;

public partial class MyNetworkedClass : RefCounted
{

    private NetCommunicator _communicator = new();

    private Dictionary<string, int> _NetVariable = new();

    [Export] public Dictionary<string, int> NetVariable
    
    {
        set
        {
            _NetVariable = value;
            SendVariable("NetVariable", value);
        }

        get { return _NetVariable; }
    }

    enum PacketType: ushort
    {
        VariableSync,
    }

    public void InitOnServer()
    {
        _communicator.Register(NetCommunicator.GenerateUniqueId());
        Init();
    }

    public void InitOnClient(uint id)
    {
        _communicator.Register(id);
        Init();
    }

    public void Init()
    {
        _communicator.MessageReceived += OnMessageReceived;
    }

    public void SendVariable(string propertyName, Variant value)
    {
        if (!GameServer.Instance.Multiplayer.IsServer())
            return;

        var args = new Dictionary<string, Variant>();
        args[propertyName] = value;

        foreach(var pid in GameServer.Instance.Multiplayer.GetPeers())
            _communicator.SendMessageTo(pid, (ushort)PacketType.VariableSync, args);

    }

    private void OnMessageReceived(int fromPeer, ushort packetId, Variant args)
    {
        var packetType = (PacketType)packetId;

        switch (packetId)
        {
            case (ushort)PacketType.VariableSync:
                Godot.Collections.Dictionary<string, Variant> varData = (Godot.Collections.Dictionary<string, Variant>)args;

                foreach(string propertyName in varData.Keys)
                {
                    Set(propertyName, varData[propertyName]);
                }

                break;
        }
    }

    public byte[] Serialize()
    {
        Godot.Collections.Dictionary data = new();
        data["id"] = _communicator.UniqueId;

        return GD.VarToBytes(data);
    }

    public static MyNetworkedClass Deserialize(byte[] bytes)
    {
        Godot.Collections.Dictionary data = (Godot.Collections.Dictionary)GD.BytesToVar(bytes);
        var result = new MyNetworkedClass();
        result.InitOnClient(data["id"].As<uint>());
        return result;;

    }

}

