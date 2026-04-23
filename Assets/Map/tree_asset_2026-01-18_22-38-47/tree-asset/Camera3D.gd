extends Camera3D

var limit := deg_to_rad(25.0)
var smoothness := 6.0

func _process(delta):
	var m_pos = get_viewport().get_mouse_position()
	var size = get_viewport().get_visible_rect().size
	
	var target_x = -((m_pos.y / size.y) * 2 - 1) * limit
	var target_y = -((m_pos.x / size.x) * 2 - 1) * limit
	
	rotation.x = lerp_angle(rotation.x, target_x, smoothness * delta)
	rotation.y = lerp_angle(rotation.y, target_y, smoothness * delta)
