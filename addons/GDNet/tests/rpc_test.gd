extends Node

func _ready() -> void:
	GDNet.Setup()
	
	GDNet.OnNetworkReady.connect(_network_ready)
	GDNet.OnNetworkDisconnected.connect(_network_disconnected)

func _network_ready() -> void:
	$Server.hide()
	$Client.hide()

func _network_disconnected() -> void:
	$Server.show()
	$Client.show()

func _on_button_pressed() -> void:
	_remote_func_rpc.invoke("hello world!")

var _remote_func_rpc := GDNetRpc.config(_remote_func)
func _remote_func(message: String) -> void:
	pass

func _on_server_pressed() -> void:
	var peer := ENetMultiplayerPeer.new()
	peer.create_server(8080)
	multiplayer.multiplayer_peer = peer

func _on_client_pressed() -> void:
	var peer := ENetMultiplayerPeer.new()
	peer.create_client("localhost", 8080)
	multiplayer.multiplayer_peer = peer
