using Godot;
using Godot.Collections;



[GlobalClass]
public partial class NetPlayerSpawner : NetSceneReplicator
{
    [Export] private Array<Node3D> spawnPoints = [];
    
    
    public void RequestSpawn(string prefabPath)
    {
        if (IsMultiplayerAuthority())
        {
            Spawn(prefabPath);
            return;
        }
        RpcId(GetMultiplayerAuthority(), MethodName.Spawn, prefabPath);
    }
    
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void Spawn(string prefabPath)
    {
        int senderId = Mulpaper.GetRemoteSenderId();
        if (senderId == 0) { senderId = GetMultiplayerAuthority(); }

        if (spawnNode.GetNodeOrNull(senderId.ToString()) != null)
            { return; }

        Node node = GD.Load<PackedScene>(prefabPath)?.Instantiate();
        if (node == null)
        {
            GD.Print("Failed to spawn player. ", senderId);
            return;
        }

        node.Name = senderId.ToString();
        node?.SetMultiplayerAuthority(senderId);
        spawnNode?.AddChild(node);
        
        if (node is Node3D)
        {
            if (spawnPoints.Count != 0)
            {
                (node as Node3D).GlobalPosition = spawnPoints.PickRandom().GlobalPosition;
            }
        }
    }
}