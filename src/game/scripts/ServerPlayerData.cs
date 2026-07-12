using Godot.Collections;
using Godot;
using System.IO;

[GlobalClass]
public partial class ServerPlayerData : Resource
{
    private const string BASEDIR = "server/players/";
    private const string SAVEPATH = "user://" + BASEDIR + "{0}.tres";
    public static string GetSavePathByLogin(string login)
    {
        return System.String.Format(SAVEPATH, login);
    }

    [Export] private string _nickname = "Player";
    public string nickname
    {
        get => _nickname;
        set { if (_nickname != value) { _nickname = value; Save(); } }
    }

    [Export] private string _login = "Login";
    public string Login
    {
        get => _login;
        set { if (_login != value) { _login = value; Save(); } }
    }

    private string _password;
    [Export] public string Password
    {
        get => _password;
        set { if (_password != value) { _password = value; Save(); } }
    }

    [Export] public Dictionary<string, Variant> data;

    public ServerPlayerData() : base() {}
    ServerPlayerData(string l)
    {
        Login = l;
    }

    public static ServerPlayerData GetOrCreate(string login)
    {
        string savePath = GetSavePathByLogin(login);
        if (ResourceLoader.Exists(savePath))
        {
            ServerPlayerData loadedData = ResourceLoader.Load<ServerPlayerData>(savePath);
            if (loadedData != null) { return loadedData; }
        }
        
        ServerPlayerData data = new(login);
        data.Save();
        return data;
    }

    public void Save()
    {
        string savePath = GetSavePathByLogin(Login);

        
        string realDirectory = OS.GetUserDataDir().PathJoin(BASEDIR);
        
        if (!Directory.Exists(realDirectory))
        {
            Directory.CreateDirectory(realDirectory);
        }
        
        ResourceSaver.Save(this, savePath);
        GD.Print("Saved sex");
    }

    public byte[] NetworkSerialize()
    {
        Dictionary buffer = [];
        buffer["login"] = Login;
        buffer["nickname"] = nickname;
        buffer["data"] = data;

        return GD.VarToBytes(buffer);
    }

    public static ServerPlayerData NetworkDeserialize(byte[] bytes)
    {
        Dictionary buffer = (Dictionary)GD.BytesToVar(bytes);
        ServerPlayerData playerData = new((string)buffer["login"])
        {
            nickname = (string)buffer["nickname"],
            data = (Dictionary<string, Variant>)buffer["data"]
        };

        return playerData;
    }
}