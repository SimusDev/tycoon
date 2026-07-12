extends HBoxContainer

@export var line: LineEdit
@export var btn: Button

var show_line:bool = false :
	set(val):
		show_line = val
		_update()

func _update() -> void:
	line.secret = !show_line
	
	if show_line:
		btn.text = "o_o"
	else:
		btn.text = "-_-"

func _ready() -> void:
	_update()
	btn.pressed.connect(func(): show_line = !show_line)


#func _on_btn_pressed() -> void:
	#show_line = !show_line
