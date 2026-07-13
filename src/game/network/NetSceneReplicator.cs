using Godot;

[GlobalClass]
public partial class NetSceneReplicator : Node
{
    [Export] protected Node spawnNode;
    private GDNetBuffer _buffer = new();

    public MultiplayerApi Mulpaper => Multiplayer;

    public override void _Ready()
    {
        if (spawnNode == null) { return; }

        GDNet.Instance.OnNetworkReady += OnNetworkReady;
        if (GDNet.Instance.IsConnectedToServer()) { OnNetworkReady(); }
    }

    private void OnSpawnNodeChildEnteredTree(Node node)
    {
        CallDeferred(MethodName.DeferredOnSpawnNodeChildEnteredTree, node);
    }

    private void DeferredOnSpawnNodeChildEnteredTree(Node node)
    {
        if (!IsInstanceValid(node)) { return; }
        if (!CanReplicateNode(node)) { return; }
        Rpc(MethodName.SpawnFromBytes, SerializeNode(node));
    }
    

    private void OnSpawnNodeChildExitingTree(Node node)
    {
        Rpc(MethodName.DespawnFromName, node.Name);
    }

    public static bool CanReplicateNode(Node node)
    {
        return node.SceneFilePath != "";
    }

    private void OnNetworkReady()
    {
        if (IsMultiplayerAuthority())
        {
            spawnNode.ChildEnteredTree += OnSpawnNodeChildEnteredTree;
            spawnNode.ChildExitingTree += OnSpawnNodeChildExitingTree;
        }
        else
        {
            RequestReplicate();
        }
    }

    private void RequestReplicate()
    {
        RpcId(GetMultiplayerAuthority(), MethodName.Send);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void Send()
    {
        long senderId = Mulpaper.GetRemoteSenderId();
    
        foreach (Node node in spawnNode.GetChildren())
        {
            if (!CanReplicateNode(node)) { continue; }
            
            RpcId(senderId, MethodName.SpawnFromBytes, SerializeNode(node));    
        }
    }


    #region Serialize/Deserialize
    private byte[] SerializeNode(Node node)
    {
        _buffer.Clear();
        string name = (string)node.Name;
        name = name.ValidateNodeName();
        
        
        _buffer.WriteString(node.SceneFilePath);
        _buffer.WriteString(name);
        _buffer.WriteInt(node.GetMultiplayerAuthority());

        if (node is Node3D)
        {
            _buffer.WriteString("Node3D");
            _buffer.WriteVector3((node as Node3D).Position);
            _buffer.WriteVector3((node as Node3D).Rotation);
            _buffer.WriteVector3((node as Node3D).Scale);
        }
        else if (node is Node2D)
        {
            _buffer.WriteString("Node2D");
            _buffer.WriteVector2((node as Node2D).Position);
            _buffer.WriteFloat((node as Node2D).Rotation);
            _buffer.WriteVector2((node as Node2D).Scale);
        }
        else
        {
            _buffer.WriteString("Node");
        }

        return _buffer.GetBytes();
    }

    private Node DeserializeNode(byte[] bytes)
    {
        _buffer.SetBytes(bytes);
        _buffer.Seek(0);

        Node node = GD.Load<PackedScene>(_buffer.ReadString()).Instantiate<Node>();
        node.Name =_buffer.ReadString();
        node.SetMultiplayerAuthority((int)_buffer.ReadInt());
        
        string nodeType = _buffer.ReadString();
        switch (nodeType) {
            case "Node3D":
                (node as Node3D).Position = _buffer.ReadVector3();
                (node as Node3D).Rotation = _buffer.ReadVector3();
                (node as Node3D).Scale = _buffer.ReadVector3();
                break;
            case "Node2D":
                (node as Node2D).Position = _buffer.ReadVector2();
                (node as Node2D).Rotation = _buffer.ReadFloat();
                (node as Node2D).Scale = _buffer.ReadVector2();
                break;
            case "Node":
                break;
        }

        return node;
    }

    #endregion


    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SpawnFromBytes(byte[] bytes)
    {
        Node node = DeserializeNode(bytes);
        spawnNode.AddChild(node);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void DespawnFromName(string name)
    {
        Node node = spawnNode.GetNode(name);
        node?.QueueFree();
    }
}