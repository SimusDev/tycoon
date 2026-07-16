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
        _peerId = GetMultiplayerAuthority();
        
        GameServer.Instance.PlayerNicknameReceived += OnPlayerNicknameReceived;
        GameServer.Instance.RequestPlayerNickname(_peerId);
    }


    private void OnPlayerNicknameReceived(string nickname)
    {
        GameServer.Instance.PlayerNicknameReceived -= OnPlayerNicknameReceived;
        Text = nickname;
    }

}