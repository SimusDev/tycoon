using Godot;

namespace NetNodeSyncronizerResource
{
    [GlobalClass]
    public partial class NetSyncProperty : Resource
    {

        [Export] public string Name;
        [Export] public MultiplayerPeer.TransferModeEnum TransferMode = MultiplayerPeer.TransferModeEnum.Reliable; 
    

    }
}