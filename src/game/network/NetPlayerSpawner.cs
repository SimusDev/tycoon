using System.Numerics;
using Godot;
using Godot.Collections;



[GlobalClass]
public partial class NetPlayerSpawner : NetSceneReplicator
{
    [Export] private Array<Node3D> spawnPoints = [];
    public void RequestSpawn(string prefabPath)
    {
        if (Mulpaper.IsServer())
        {
            Spawn(prefabPath);
            return;
        }
        RpcId(GetMultiplayerAuthority(), MethodName.Spawn, prefabPath);
    }
    
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void Spawn(string prefabPath)
    {
        Node node = GD.Load<PackedScene>(prefabPath)?.Instantiate();
        spawnNode?.SetMultiplayerAuthority(Mulpaper.GetRemoteSenderId());
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