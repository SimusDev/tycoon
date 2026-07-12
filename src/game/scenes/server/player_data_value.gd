extends LineEdit

func _ready() -> void:
	GameServer.LoginError.connect(_on_login_error)
	

func _on_login_error(error: String) -> void:
	if error == "Ok":
		pass
