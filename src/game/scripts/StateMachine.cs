using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class StateMachine : Node
{
    [Signal] delegate void StateEnterEventHandler(string stateName);
    [Signal] delegate void StateExitEventHandler(string stateName);

    [Export] public string onReadyState = "";

    private string currentState = "";


    public override void _Ready()
    {
        currentState = onReadyState;
        
        if (!Multiplayer.IsServer())
        {
            requestReceive();
        }
    }

    private void requestReceive()
    {
        Rpc(MethodName.send);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void send()
    {
        Godot.Collections.Dictionary<string, Variant> data = new Godot.Collections.Dictionary<string, Variant>
        {
            { "currentState", currentState }
        };

        int sender_id = Multiplayer.GetRemoteSenderId();
        RpcId(sender_id, "receive", data);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void receive(Godot.Collections.Dictionary<string, Variant> data)
    {
        currentState = data.ContainsKey("currentState") ? data["currentState"].AsString() : "";
    }

    public string CurrentState()
    {
        return currentState;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void localSwitchState(string stateName)
    {
        EmitSignal(SignalName.StateExit, currentState);
        EmitSignal(SignalName.StateEnter, stateName);
        currentState = stateName;
    }

    public void SwitchState(string stateName)
    {
        if (CurrentState() == stateName)
            return;
        

        Rpc("localSwitchState", stateName);
    }

}