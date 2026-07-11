using Godot;

[GlobalClass]
public partial class PlayerNickname : Label3D
{
    private PlayerData playerData;

    PlayerNickname()
    {
        Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
    } 
    

    public override void _Ready()
    {
        bool is_auth = IsMultiplayerAuthority();
        if (is_auth)
        {
        playerData = PlayerData.GetOrCreate();
        Hide();
        }
        else
        {
            RequestReceive();
        }


    }

    private void RequestReceive()
    {
        RpcId(GetMultiplayerAuthority(),
            MethodName.Send
        );
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Send()
    {
        RpcId(Multiplayer.GetRemoteSenderId(),
            MethodName.Receive,
            playerData.NetworkSerialize()
        );
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Receive(byte[] bytes)
    {
        playerData = PlayerData.NetworkDeserialize(bytes);

        Text = playerData.nickname;
    }
}