extends Node

var _peer := PacketPeerUDP.new()

var _is_active: bool = false

var PACKET: PackedByteArray = var_to_bytes("hello world!")

var _thread_count: int = OS.get_processor_count()

@onready var _status_label: Label = $VBoxContainer/_StatusLabel
@onready var _packet_count_label: Label = $VBoxContainer/_PacketCountLabel
@onready var _h_slider_packet_count: HSlider = $VBoxContainer/_HSliderPacketCount
@onready var _h_slider_task_count: HSlider = $VBoxContainer/_HSliderTaskCount
@onready var _task_count: Label = $VBoxContainer/_TaskCount
@onready var _connect_button: Button = $VBoxContainer/_ConnectButton
@onready var line_edit_ip: LineEdit = $VBoxContainer/LineEditIp
@onready var line_edi_port: LineEdit = $VBoxContainer/LineEdiPort

func _ready() -> void:
	_h_slider_task_count.value = OS.get_processor_count()
	_h_slider_task_count.max_value = OS.get_processor_count()

func _physics_process(delta: float) -> void:
	flush()

func get_task_count() -> int:
	return int(_h_slider_task_count.value)

func get_packet_count() -> int:
	return int(_h_slider_packet_count.value)

func flush() -> void:
	if !_peer.is_bound():
		return
	
	#print(get_packet_count())
	
	var task_id: int = WorkerThreadPool.add_group_task(_send_packets_thread_task, get_task_count(), -1, true)
	#WorkerThreadPool.wait_for_group_task_completion(task_id)
	
func _send_packets_thread_task(index: int) -> void:
	for i in get_packet_count():
		var fake_message: String = "sas! ohalera!" + str(randi())
		_peer.put_packet(fake_message.to_ascii_buffer())

func get_status() -> String:
	if _peer.is_bound():
		return "Connected!" % [_peer.get_packet_ip(), _peer.get_packet_port()]
	return "Disconnected."

func _process(delta: float) -> void:
	_status_label.text = "Status: %s" % get_status()
	_packet_count_label.text = "Packet count: %s" % get_packet_count()
	_task_count.text = "Thread count: %s" % get_task_count()

func _on__connect_button_pressed() -> void:
	_peer.close()
	_peer.connect_to_host(line_edit_ip.text, int(line_edi_port.text))
	_peer.set_broadcast_enabled(true)
