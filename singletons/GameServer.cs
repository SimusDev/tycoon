using Godot;
using Godot.Collections;
using System.Linq;

public partial class GameServer : Node
{
    private static GameServer _instance;
    public static GameServer Instance => _instance;
    
    private Dictionary<long, ServerPlayerData> _usersById = [];
    private Dictionary<long, long> _peerToUserId = [];
    private Dictionary<long, long> _userIdToPeer = [];
    private Dictionary<string, long> _loginToUserId = [];

    public System.Collections.Generic.IReadOnlyDictionary<long, ServerPlayerData> ConnectedUsers => _usersById;

    [Signal] public delegate void LoginErrorEventHandler(string error);
    [Signal] public delegate void RegisterErrorEventHandler(string error);
    [Signal] public delegate void UserDataReceivedEventHandler(Dictionary<string, Variant> data);
    [Signal] public delegate void PlayerDisconnectedEventHandler(long userId);
    [Signal] public delegate void PlayerConnectedEventHandler(long userId);

    [Signal] public delegate void PlayerNicknameReceivedEventHandler(string nickname);

    public override void _Ready()
    {
        if (_instance != null)
        {
            QueueFree();
            return;
        }
        
        _instance = this;
        ProcessMode = ProcessModeEnum.Always;
        
        string userDir = OS.GetUserDataDir().PathJoin("server/players/");
        if (!DirAccess.DirExistsAbsolute(userDir))
        {
            DirAccess.MakeDirRecursiveAbsolute(userDir);
        }
        
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
    }

    public override void _ExitTree()
    {
        foreach (var peerId in _peerToUserId.Keys.ToList())
        {
            RemoveConnectedUser(peerId);
        }
        
        Multiplayer.PeerDisconnected -= OnPeerDisconnected;
        
        if (_instance == this)
        {
            _instance = null;
        }
    }


    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RequestPlayerNickname(long peerId)
    {
        long senderId = Multiplayer.GetRemoteSenderId();
        
        string nickname = GetPlayerNickname(peerId);
        
        RpcId(senderId, MethodName.ReceivePlayerNickname, nickname);
    }

    private string GetPlayerNickname(long peerId)
    {
        if (_peerToUserId.TryGetValue(peerId, out long userId))
        {
            if (_usersById.TryGetValue(userId, out ServerPlayerData userData))
            {
                return userData.nickname ?? $"Player {peerId}";
            }
        }
        return $"Player {peerId}";
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceivePlayerNickname(string nickname)
    {
        EmitSignal(SignalName.PlayerNicknameReceived, nickname);
    }

    private void OnPeerDisconnected(long peerId)
    {        
        if (_peerToUserId.TryGetValue(peerId, out long userId))
        {
            RemoveConnectedUser(peerId);
            Rpc(MethodName.BroadcastPlayerDisconnected, userId);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void BroadcastPlayerDisconnected(long userId)
    {
        EmitSignal(SignalName.PlayerDisconnected, userId);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void BroadcastPlayerConnected(long userId)
    {
        EmitSignal(SignalName.PlayerConnected, userId);
    }

    private void AddConnectedUser(long peerId, string login, ServerPlayerData userData)
    {
        if (_loginToUserId.TryGetValue(login, out long existingUserId))
        {
            if (_userIdToPeer.TryGetValue(existingUserId, out long oldPeerId))
            {
                RpcId(oldPeerId, MethodName.ForceDisconnect, "LoggedInElsewhere");
                RemoveConnectedUser(oldPeerId);
            }
        }
        
        if (_peerToUserId.TryGetValue(peerId, out long existingPeerUserId))
        {
            RemoveConnectedUser(peerId);
        }
        
        long userId = (long)userData.GetInstanceId();


        _usersById[userId] = userData;
        _peerToUserId[peerId] = userId;
        _userIdToPeer[userId] = peerId;
        _loginToUserId[login] = userId;
        
        
        Rpc(MethodName.BroadcastPlayerConnected, userId);
    }

    public void RemoveConnectedUser(long peerId)
    {
        if (!_peerToUserId.TryGetValue(peerId, out long userId))
            return;
        
        if (_usersById.TryGetValue(userId, out ServerPlayerData userData))
        {
            userData.Save();
            
            _peerToUserId.Remove(peerId);
            _userIdToPeer.Remove(userId);
            _usersById.Remove(userId);
            _loginToUserId.Remove(userData.Login);
            
            
            Rpc(MethodName.BroadcastPlayerDisconnected, userId);
        }
    }


    public void RequestRegister(string login, string password) 
    { 
        RpcId(1, MethodName.Register, login, password); 
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void Register(string login, string password)
    {
        long peerId = Multiplayer.GetRemoteSenderId();
        
        if (!ValidateLogin(login))
        {
            SendRegisterError(peerId, "InvalidLogin");
            return;
        }
        
        if (!ValidatePassword(password))
        {
            SendRegisterError(peerId, "InvalidPassword");
            return;
        }
        
        if (UserExists(login))
        {
            SendRegisterError(peerId, "UserAlreadyExists");
            return;
        }

        ServerPlayerData userData = ServerPlayerData.GetOrCreate(login);
        userData.Password = password;
        userData.nickname = login;
        userData.Save();
        
        SendRegisterSuccess(peerId);
    }


    public void RequestLogin(string login, string password) 
    { 
        RpcId(1, MethodName.Login, login, password); 
    }
    
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void Login(string login, string password)
    {
        long peerId = Multiplayer.GetRemoteSenderId();
        
        if (!ValidateLogin(login) || !ValidatePassword(password))
        {
            SendLoginError(peerId, "InvalidCredentials");
            return;
        }
        
        if (!UserExists(login))
        {
            SendLoginError(peerId, "UserNotFound");
            return;
        }
        
        ServerPlayerData userData = LoadUserData(login);
        
        if (userData.Password != password)
        {
            SendLoginError(peerId, "WrongPassword");
            return;
        }
        
        if (IsUserLoggedIn(login))
        {
            long oldPeerId = _userIdToPeer[_loginToUserId[login]];
            ForceDisconnectUser(oldPeerId, "LoggedInElsewhere");
            CallDeferred("AddConnectedUser", peerId, login, userData);
            SendLoginSuccess(peerId, userData);
            return;
        }
        
        AddConnectedUser(peerId, login, userData);
        SendLoginSuccess(peerId, userData);
    }


    public void RequestUserData(string login, string password)
    {
        long peerId = Multiplayer.GetRemoteSenderId();
        Dictionary<string, Variant> data = [];
        
        if (!UserExists(login))
        {
            data["error"] = "UserDoesNotExist";
            RpcId(peerId, MethodName.ReceiveUserData, GD.VarToBytes(data));
            return;
        }
        
        ServerPlayerData userData = LoadUserData(login);
        
        if (userData.Password != password)
        {
            data["error"] = "WrongPassword";
            RpcId(peerId, MethodName.ReceiveUserData, GD.VarToBytes(data));
            return;
        }

        data["nickname"] = userData.nickname;
        data["login"] = userData.Login;
        data["data"] = userData.data ?? new Dictionary<string, Variant>();
        data["error"] = "Ok";

        RpcId(peerId, MethodName.ReceiveUserData, GD.VarToBytes(data));
    }


    private bool ValidateLogin(string login)
    {
        return !string.IsNullOrEmpty(login) && 
               login.Length >= 3 && 
               login.Length <= 20 && 
               System.Text.RegularExpressions.Regex.IsMatch(login, @"^[a-zA-Z0-9_]+$");
    }

    private bool ValidatePassword(string password)
    {
        return !string.IsNullOrEmpty(password) && password.Length >= 6;
    }

    private bool UserExists(string login)
    {
        string path = ServerPlayerData.GetSavePathByLogin(login);
        return ResourceLoader.Exists(path);
    }

    private ServerPlayerData LoadUserData(string login)
    {
        return ServerPlayerData.GetOrCreate(login);
    }

    public bool IsUserLoggedIn(string login)
    {
        return _loginToUserId.ContainsKey(login);
    }

    public bool IsUserLoggedIn(long userId)
    {
        return _usersById.ContainsKey(userId);
    }

    public ServerPlayerData GetUserByPeerId(long peerId)
    {
        if (_peerToUserId.TryGetValue(peerId, out long userId))
        {
            _usersById.TryGetValue(userId, out ServerPlayerData userData);
            return userData;
        }
        return null;
    }

    public ServerPlayerData GetUserByLogin(string login)
    {
        if (_loginToUserId.TryGetValue(login, out long userId))
        {
            _usersById.TryGetValue(userId, out ServerPlayerData userData);
            return userData;
        }
        return null;
    }

    public void ForceDisconnectUser(long peerId, string reason = "Disconnected")
    {
        if (_peerToUserId.TryGetValue(peerId, out long userId))
        {
            RpcId(peerId, MethodName.ForceDisconnect, reason);
            RemoveConnectedUser(peerId);
        }
    }


    private void SendLoginError(long peerId, string error)
    {
        RpcId(peerId, MethodName.EmitError, SignalName.LoginError, error);
    }

    private void SendLoginSuccess(long peerId, ServerPlayerData userData)
    {
        RpcId(peerId, MethodName.LoginSuccess, userData.Login);
    }

    private void SendRegisterError(long peerId, string error)
    {
        RpcId(peerId, MethodName.EmitError, SignalName.RegisterError, error);
    }

    private void SendRegisterSuccess(long peerId)
    {
        RpcId(peerId, MethodName.EmitError, SignalName.RegisterError, "Ok");
    }


    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
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

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void LoginSuccess(string login)
    {
        
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void ForceDisconnect(string reason)
    {
        EmitSignal(SignalName.PlayerDisconnected, 0);
    }
}