using Godot;
using System;

public partial class SynchronizationServer : Node
{
    // public enum DataType: byte
    // {
    //     Default,
    //     Dictionary
    // }

    // private static GDNetBuffer _buffer = new();

    // public SynchronizationServer()
    // {
        
    // }

    // public void Send(GodotObject godot_object, string propertyName, Variant value)
    // {
    //     if (!GameServer.Instance.Multiplayer.IsServer()) return;
    //     GDNetCommunicator communicator = (GDNetCommunicator)(GodotObject)godot_object.Get("_communicator");
    //     if (communicator == null) return;

    //     communicator.OnBytesReceived += (peer, bytes) => OnBytesReceivedFunc(communicator, peer, bytes);

    //     _buffer.Clear();
    //     _buffer.WriteUInt8((byte)DataType.Default);
    //     _buffer.WriteString(propertyName);
    //     _buffer.WriteVar(value);
        
    //     communicator.SendToAll(_buffer.GetBytes());
    // }
    
    // public void Send(GDNetCommunicator communicator, string dictName, string propertyName, Variant value)
    // {
    //     if (!GameServer.Instance.Multiplayer.IsServer()) return;

    //     communicator.OnBytesReceived += (peer, bytes) => OnBytesReceivedFunc(communicator, peer, bytes);

    //     _buffer.Clear();
    //     _buffer.WriteUInt8((byte)DataType.Dictionary); 
    //     _buffer.WriteString(dictName);
    //     _buffer.WriteString(propertyName);
    //     _buffer.WriteVar(value);
        
    //     communicator.SendToAll(_buffer.GetBytes());
    // }
    
    // private void OnBytesReceivedFunc(GDNetCommunicator communicator, int peer, byte[] bytes)
    // {
    //     if (peer != GDNet.ServerID) return;
    //     communicator.OnBytesReceived -= OnBytesReceived;
    //     OnBytesReceived(peer, bytes);
    // }

    // private void OnBytesReceived(int peer, byte[] bytes)
    // {
    //     _buffer.SetBytes(bytes);
    //     _buffer.Seek(0);
    //     byte type = _buffer.ReadUInt8();
    //     switch ((DataType)type)
    //     {
    //         case DataType.Default:
    //             Set(_buffer.ReadString(), _buffer.ReadVar());
    //             break;
    //         case DataType.Dictionary:
    //             Get(_buffer.ReadString()).AsGodotDictionary()[_buffer.ReadString()] = _buffer.ReadVar();
    //             break;
    //     }
    // }

}

