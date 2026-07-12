using Godot;

// [GlobalClass]
// public partial class PlayerNickname : Label3D
// {
//     private PlayerData _playerData;
//     [Export] public PlayerData PlayerData
//     { 
//         get => _playerData;
//         set
//         {
//             if (_playerData != value)
//             {
//                 _playerData = value;

//             }
//         }
//     }

//     PlayerNickname()
//     {
//         Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
//     } 
    


//     public override void _Ready()
//     {
//         bool is_auth = IsMultiplayerAuthority();
//         if (is_auth)
//         {
//             PlayerData = PlayerData.GetOrCreate();
//             Hide();
//         }

//         else
//         {
//             RequestReceive();
//         }
//     }

//     private void RequestReceive()
//     {
//         RpcId(GetMultiplayerAuthority(),
//             MethodName.Send
//         );
//     }
    
//     [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
//     private void Send()
//     {
//         RpcId(Multiplayer.GetRemoteSenderId(),
//             MethodName.Receive,
//             PlayerData.NetworkSerialize()
//         );
//     }
    
//     [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
//     private void Receive(byte[] bytes)
//     {
//         PlayerData = PlayerData.NetworkDeserialize(bytes);

//         Text = PlayerData.nickname;
//     }
// }