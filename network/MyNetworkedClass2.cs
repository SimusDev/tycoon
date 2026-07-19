
using Godot;
using Godot.Collections;
using System;

public partial class MyNetworkedClass2 : RefCounted
{

    private GDNetRpc _rpc = new();

    public void InitWithId(long uniqueId)
    {
        _rpc.SynchronizeNetworkIDByUniqueID(uniqueId);
        
        _rpc.Invoke(myRpc, "sdfsdffsd", "Qsdsadsa");
    }

    [GDNetRpc(Permission.Any)]
    private void myRpc(string sas1, string sas2)
    {

    }

    public byte[] Serialize()
    {
        Godot.Collections.Dictionary data = new();
        data["id"] = _rpc.GetNetworkID();

        return GD.VarToBytes(data);
    }

    public static MyNetworkedClass Deserialize(byte[] bytes)
    {
        Godot.Collections.Dictionary data = (Godot.Collections.Dictionary)GD.BytesToVar(bytes);
        var result = new MyNetworkedClass();
        result.InitOnClient(data["id"].As<uint>());
        return result;;

    }

}

