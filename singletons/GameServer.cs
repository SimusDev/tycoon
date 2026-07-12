using System.Data.SqlTypes;
using Godot;
using Godot.Collections;

public partial class GameServer : Node
{
    private static GameServer _instance;
    public static GameServer Instance => _instance;
    
    private Dictionary<long, ServerPlayerData> _connectedPlayersData = [];
    public System.Collections.Generic.IReadOnlyDictionary<long, ServerPlayerData> ConnectedPlayers => _connectedPlayersData;

    [Signal] delegate void LoginErrorEventHandler(string error);
    [Signal] delegate void RegisterErrorEventHandler(string error);

    [Signal] delegate void UserDataReceivedEventHandler(Dictionary<string, Variant> data);

    public override void _Ready()
    {
        if (_instance != null)
        {
            QueueFree();
            return;
        }
        
        _instance = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _ExitTree()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public void RequestRegister(string login, string password) { RpcId(1, MethodName.Register, login, password); }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, CallLocal = true)]
    public string Register(string login, string password)
    {
        if (ResourceLoader.Exists(ServerPlayerData.GetSavePathByLogin(login)))
        {
            RpcId(Multiplayer.GetRemoteSenderId(), MethodName.EmitError, SignalName.RegisterError, "UserAlreadyExists");
            return "UserAlreadyExists";
        }

        ServerPlayerData serverPlayerData = ServerPlayerData.GetOrCreate(login);
        serverPlayerData.Login = login;
        serverPlayerData.Password = password;

        GD.Print(System.String.Format("registred: {0}, {1} | res: {2}", login, password, serverPlayerData));

        RpcId(Multiplayer.GetRemoteSenderId(), MethodName.EmitError, SignalName.RegisterError, "Ok");
        return "Ok";
    }

    public void RequestLogin(string login, string password) { RpcId(1, MethodName.Login, login, password); }
    
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, CallLocal = true)]
    public string Login(string login, string password)
    {
        if (!ResourceLoader.Exists(ServerPlayerData.GetSavePathByLogin(login)))
        {
            RpcId(Multiplayer.GetRemoteSenderId(), MethodName.EmitError, SignalName.LoginError, "UserDoesNotExist");
            return "UserDoesNotExist";
        }
       
        ServerPlayerData serverPlayerData = ServerPlayerData.GetOrCreate(login);
        
        if (serverPlayerData.Password != password)
        {
            RpcId(Multiplayer.GetRemoteSenderId(), MethodName.EmitError, SignalName.LoginError, "WrongPassword");
            GD.Print(System.String.Format("Login failed. sent password: '{0}', true password: '{1}'", password, serverPlayerData.Password));
            return "WrongPassword";
        }

        RpcId(Multiplayer.GetRemoteSenderId(), MethodName.EmitError, SignalName.LoginError, "Ok");
        return "Ok";
    }

    public void RequestUserData(string login, string password)
    {
        Dictionary<string, Variant> data = [];
        if (!ResourceLoader.Exists(ServerPlayerData.GetSavePathByLogin(login)))
        {
            data["error"] = "UserDoesNotExist";
            RpcId(Multiplayer.GetRemoteSenderId(),
                MethodName.ReceiveUserData, GD.VarToBytes(data)
            );
            return;
        }
        
        ServerPlayerData serverPlayerData = ServerPlayerData.GetOrCreate(login);
        if (serverPlayerData.Password != password)
        {
            data["error"] = "WrongPassword";
            RpcId(Multiplayer.GetRemoteSenderId(),
                MethodName.ReceiveUserData, GD.VarToBytes(data)
            );
            return;
        }

        data["nickname"] = serverPlayerData.nickname;
        data["login"] = serverPlayerData.Login;
        data["password"] = serverPlayerData.Password;
        data["data"] = serverPlayerData.data;
        data["error"] = "Ok";

        RpcId(Multiplayer.GetRemoteSenderId(),
            MethodName.ReceiveUserData, GD.VarToBytes(data)
        );
    }

    [Rpc(mode: MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveUserData(byte[] bytes)
    {
        Dictionary<string, Variant> data = (Dictionary<string, Variant>)GD.BytesToVar(bytes);
        EmitSignal(SignalName.UserDataReceived, data);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void EmitError(string signalName, string error)
    {
        EmitSignal(signalName, error);
    }
}