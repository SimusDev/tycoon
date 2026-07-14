using Godot;

[GlobalClass]
public partial class PlayerNickname : Label3D
{
    private long _peerId;
    
    public PlayerNickname()
    {
        Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
    }
    
    public override void _Ready()
    {
        GDNet.Instance.OnNetworkReady += OnNetworkReady;
        if (GDNet.Instance.IsConnectedToServer()) { OnNetworkReady(); }
    }

    public void OnNetworkReady()
    {
        _peerId = GetMultiplayerAuthority();
        
        GameServer.Instance.PlayerNicknameReceived += OnPlayerNicknameReceive;
        GameServer.Instance.RequestPlayerNickname(_peerId);
    }

    private void OnPlayerNicknameReceive(string nickname)
    {
        Text = nickname;
    }

}