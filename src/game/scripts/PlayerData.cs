using Godot;

[GlobalClass]
public partial class PlayerData : Resource
{
    private const string SAVEPATH = "user://player_data.tres";
    private string _nickname = "Player";
    [Export] public string nickname
    { 
        get => _nickname;
        set { if (_nickname != value) { _nickname = value; Save(); } }
    }
    [Export] Godot.Collections.Dictionary<string, Variant> data;

    public static PlayerData GetOrCreate()
    {
        if (ResourceLoader.Exists(SAVEPATH))
        {
            PlayerData loadedData = ResourceLoader.Load<PlayerData>(SAVEPATH);
            if (loadedData != null) { return loadedData; }
        }
        
        PlayerData data = new();
        data.Save();
        return data;
    }

    

    public void Save()
    {
        ResourceSaver.Save(this, SAVEPATH);
    }

}