extends VehicleBody3D

const MOUSE_SENS = 1.0
const ENGINE_FORCE = 120.0
const BRAKE_FORCE = 0.1
const BRAKE_FW_SLIP = 0.85
const BRAKE_RW_SLIP = 0.6
const NORMAL_SLIP = 0.92
const NORMAL_RW_SLIP = 0.90
const STEERING_MAX = 30.0
const STEERING_SPEED = 200.0

var mouse_sensitivity: float

func _ready():
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

func _process(_delta: float):
	mouse_sensitivity = MOUSE_SENS * 0.25 * 2 * PI / DisplayServer.screen_get_size().y
	
	%CameraStickBase.rotation.y = rotation.y

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey and event.is_released():
		if event.physical_keycode == KEY_ESCAPE:
			if Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
				Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
			else:
				Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
	
	if event is InputEventMouseMotion and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
		var delta = event.relative
		%CameraStick.rotation.x -= delta.y * mouse_sensitivity
		%CameraStick.rotation.y -= delta.x * mouse_sensitivity

func _physics_process(delta: float):
	engine_force = 0
	if Input.is_action_pressed("throttle"):
		engine_force = ENGINE_FORCE
	
	%wheel_fl.wheel_friction_slip = NORMAL_SLIP
	%wheel_fr.wheel_friction_slip = NORMAL_SLIP
	%wheel_bl.wheel_friction_slip = NORMAL_RW_SLIP
	%wheel_br.wheel_friction_slip = NORMAL_RW_SLIP
	brake = 0
	if Input.is_action_pressed("brake"):
		brake = BRAKE_FORCE
		%wheel_fl.wheel_friction_slip = BRAKE_FW_SLIP
		%wheel_fr.wheel_friction_slip = BRAKE_FW_SLIP
		%wheel_bl.wheel_friction_slip = BRAKE_RW_SLIP
		%wheel_br.wheel_friction_slip = BRAKE_RW_SLIP
	
	var target_steering = 0
	if Input.is_action_pressed("steer_left"):
		target_steering += STEERING_MAX
	if Input.is_action_pressed("steer_right"):
		target_steering -= STEERING_MAX
	target_steering = deg_to_rad(target_steering)
	steering = move_toward(steering, target_steering, deg_to_rad(STEERING_SPEED) * delta)
