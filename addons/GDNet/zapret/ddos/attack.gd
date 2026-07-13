extends Node

@export var ip: String = ""
@export var port: int = 0
@export var packet_count: int = 2000
@export var packet: String = "SPAM"

var _socket: PacketPeerUDP

signal on_connected()

var test_packet: PackedByteArray = []

func _ready() -> void:
	test_packet.resize(1300)
	
	_socket = PacketPeerUDP.new()
	var err: Error = _socket.connect_to_host(ip, port)
	_socket.set_broadcast_enabled(true)
	
	if err == OK:
		print("connected to server: %s:%s" % [ip, port])
		on_connected.emit()
	
	set_process(err == OK)

func _process(_delta) -> void:
	WorkerThreadPool.add_task(_attack_task)

func _attack_task() -> void:
	for i in packet_count:
		var fake_message: String = packet + str(randi())
		if is_instance_valid(_socket):
			_socket.put_packet(fake_message.to_ascii_buffer())
