using Godot;
using Godot.Collections;

namespace NetNodeSyncronizerResource
{
    [GlobalClass]
    public partial class NetSyncData : Resource
    {
        [Export] public NodePath TargetNode;
        [Export] public Array<NetSyncProperty> Properties = [];
    }
}