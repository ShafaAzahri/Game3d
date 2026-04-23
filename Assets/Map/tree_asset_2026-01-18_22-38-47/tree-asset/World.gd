extends Node3D

@export var font_size := 14
@export var margin := Vector2(10, 10)
@export var outline_size := 3
@export var text_color := Color.WHITE

var label := Label.new()

func _ready():
	var canvas := CanvasLayer.new()
	add_child(canvas)
	canvas.add_child(label)
	
	label.position = margin
	label.add_theme_font_size_override("font_size", font_size)
	label.add_theme_color_override("font_color", text_color)
	label.add_theme_color_override("font_outline_color", Color.BLACK)
	label.add_theme_constant_override("outline_size", outline_size)

func _process(_delta):
	label.text = 'FPS: ' + str(Engine.get_frames_per_second())
