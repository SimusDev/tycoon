extends RefCounted
class_name GDNetObjectSerializationTest

var field1: String = "hello!"
var field2: int = 100
var field3: bool = false

func GDNetSerialize(buffer: PackedByteArray) -> void:
	var gdnet := GDNetBuffer.new()
	gdnet.SetBytes(buffer)
	gdnet.Write(field1)
	gdnet.Write(field2)
	gdnet.Write(field3)

func GDNetDeserialize(buffer: PackedByteArray) -> GDNetObjectSerializationTest:
	var myclass := GDNetObjectSerializationTest.new()
	var gdnet := GDNetBuffer.new()
	gdnet.SetBytes(buffer)
	myclass.field1 = gdnet.Read()
	myclass.field2 = gdnet.Read()
	myclass.field3 = gdnet.Read()
	return myclass
