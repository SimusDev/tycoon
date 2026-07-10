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
        }
    }

    private void onPeerConnected(long id)
    {
        spawnPlayer(id);
    }

    private void spawnPlayer(long id)
    {
        try
        {
            Node spawnNode = GetNodeOrNull(SpawnPath);
            Node playerInstance = PlayerPrefab.Instantiate();
            playerInstance.Name = id.ToString();
            

        }
        catch
        {
            // sas
        }
    }

    private void onPlayerInstanceTreeEntered(long id)
    {
        
        Rpc("initSpawnedPlayer", id);
    }


    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private async Task<Error> initSpawnedPlayer(long id)
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
