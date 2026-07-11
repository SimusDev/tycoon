using Godot;
using System;


[GlobalClass]
public partial class CustomMultiplayerSpawner : MultiplayerSpawner
{
    [Export]
    public PackedScene PlayerPrefab;
    [Export]
    public Godot.Collections.Array<Node3D> SpawnPoints;

    public override void _Ready()
    {
        SpawnFunction = new Callable(this, MethodName.CustomSpawn);

        if (Multiplayer.IsServer())
        {
            Multiplayer.PeerConnected += onPeerConnected;
            spawnPlayer(1);
        }

        Multiplayer.PeerDisconnected += onPeerDisconnected;
    }

    private void onPeerConnected(long id)
    {
        GD.Print($"peer connected: {id}");
        spawnPlayer(id);
    }

    private void onPeerDisconnected(long id)
    {
        Node spawnNode = GetNodeOrNull(SpawnPath);
        Node player = spawnNode?.GetNodeOrNull(id.ToString());
        if (player != null)
        {
            player.QueueFree();
        }
    }

    private void spawnPlayer(long id)
    {
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