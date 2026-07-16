using Godot;

[GlobalClass]
public partial class NetSceneReplicator : Node
{
    [Export] protected Node spawnNode;
    private GDNetBuffer _buffer = new();

    private bool _isSynchronized = false;
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

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
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
    protected virtual byte[] SerializeNode(Node node)
    {
        Godot.Collections.Dictionary data = new();

        string name = (string)node.Name;
        node.Name = name.ValidateNodeName();

        data["filepath"] = node.SceneFilePath;
        data["name"] = node.Name;
        data["auth"] = node.GetMultiplayerAuthority();

        if (node is Node3D || node is Node2D)
            data["transform"] = node.Get("transform");

        return GD.VarToBytes(data);
    }

    protected virtual Node DeserializeNode(byte[] bytes)
    {
        Godot.Collections.Dictionary data = (Godot.Collections.Dictionary)GD.BytesToVar(bytes);

        Node node = GD.Load<PackedScene>(data["filepath"].AsString()).Instantiate();

        node.Name = data["name"].AsString();
        node.SetMultiplayerAuthority(data["auth"].AsInt32());

        if (data.TryGetValue("transform", out var transform))
            node.Set("transform", transform);

        return node;
    }

    #endregion


    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
    private void SpawnFromBytes(byte[] bytes)
    {
        Node node = DeserializeNode(bytes);
        spawnNode.AddChild(node);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
    private void DespawnFromName(string name)
    {
        Node node = spawnNode.GetNode(name);
        node?.QueueFree();
    }
}