using System.Threading.Tasks;
using Godot;


[GlobalClass]
public partial class CustomMultiplayerSpawner : MultiplayerSpawner
{
    [Export]
    public PackedScene PlayerPrefab;
    [Export]
    public Godot.Collections.Array<Node3D> SpawnPoints;

    public override void _Ready()
    {
        if (Multiplayer.IsServer())
        {
            Multiplayer.PeerConnected += onPeerConnected;
            spawnPlayer(1);
        }

        Multiplayer.PeerDisconnected += onPeerDisconnected;
    }

    private void onPeerConnected(long id)
    {
        GD.Print(System.String.Format("peer connected: {0}", id));
        spawnPlayer(id);
    }

    private void onPeerDisconnected(long id)
    {
        try
        {
            Node spawnNode = GetNode(SpawnPath);
            Node player = spawnNode.GetNode(id.ToString());
            player.QueueFree();
        }
        catch
        {
            GD.Print(System.String.Format("failed to free player: {0}", id));
        }
    }

    private void spawnPlayer(long id)
    {
        try
        {
            Node spawnNode = GetNodeOrNull(SpawnPath);
            Node playerInstance = PlayerPrefab.Instantiate();
            playerInstance.Name = id.ToString();
            playerInstance.TreeEntered += () => onPlayerInstanceTreeEntered(id);
            spawnNode.CallDeferred(MethodName.AddChild, playerInstance);
        }
        catch
        {
            GD.Print(System.String.Format("failed to spawn player: {0}", id));
        }
    }

    private void onPlayerInstanceTreeEntered(long id)
    {
        
        Rpc(MethodName.initSpawnedPlayer, id);
    }


    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void initSpawnedPlayer(long id)
    {
        initSpawnedPlayerAsync(id);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private async Task<Error> initSpawnedPlayerAsync(long id)
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        if (SpawnPoints.Count == 0)
        {
            return Error.Failed;
        }

        Node spawnNode = GetNodeOrNull(SpawnPath);
        if (!IsInstanceValid(spawnNode)) { return Error.Failed; }

        Node3D playerNode = spawnNode.GetNodeOrNull<Node3D>(id.ToString());
        if (!IsInstanceValid(playerNode)) { return Error.Failed; }
        
        playerNode.GlobalPosition = SpawnPoints.PickRandom().GlobalPosition;

        return Error.Ok;
    }
    
}
