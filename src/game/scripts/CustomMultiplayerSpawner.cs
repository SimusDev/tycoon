using Godot;
using System;


[GlobalClass]
public partial class CustomMultiplayerSpawner : MultiplayerSpawner
{
    [Export] public PackedScene PlayerPrefab;
    [Export] public Godot.Collections.Array<Node3D> SpawnPoints;

    public override void _Ready()
    {
        SpawnFunction = new Callable(this, MethodName.CustomSpawn);

        Multiplayer.PeerDisconnected += OnPeerDisconnected;

        //RequestSpawn();

    }

    
    public void RequestSpawn()
    {
        RpcId(GetMultiplayerAuthority(), MethodName.SpawnPlayer, -1);
    }

    private void OnPeerDisconnected(long id)
    {
        Node spawnNode = GetNodeOrNull(SpawnPath);
        Node player = spawnNode?.GetNodeOrNull(id.ToString());

        player?.QueueFree();   
    }
    
    
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SpawnPlayer(long id = -1)
    {
        if (id == -1) { id = Multiplayer.GetRemoteSenderId(); }
        

        if (SpawnPoints.Count == 0) { return; }
        Vector3 spawnPosition = SpawnPoints.PickRandom().GlobalPosition;

        var spawnData = new Godot.Collections.Dictionary
        {
            ["peer_id"] = (int)id,
            ["position"] = spawnPosition
        };

        Spawn(spawnData);
    }

    private Node CustomSpawn(Godot.Collections.Dictionary data)
    {
        if (!data.ContainsKey("peer_id") || !data.ContainsKey("position"))
        {
            return null;
        }

        if (!IsMultiplayerAuthority()) { GD.Print(12); }

        int peerId = (int)data["peer_id"];
        Vector3 position = (Vector3)data["position"];

        Node playerInstance = PlayerPrefab.Instantiate();
        playerInstance.Name = peerId.ToString();
        playerInstance.SetMultiplayerAuthority(peerId);

        playerInstance.TreeEntered += () => InitSpawnedPlayer(peerId, position);

        return playerInstance;
    }

    //[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void InitSpawnedPlayer(long id, Vector3 pos)
    {
        Node spawnNode = GetNodeOrNull(SpawnPath); if (spawnNode == null) { return; }
        Node3D playerNode = spawnNode.GetNodeOrNull<Node3D>(id.ToString()); if (playerNode == null) { return; }
        playerNode.GlobalPosition = pos;
    }
}