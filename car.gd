extends VehicleBody3D

const MOUSE_SENS = 1.0
const ENGINE_FORCE = 90.0
const DRIFT_BONUS = 1.0
const BRAKE_FORCE = 0.5
const NORMAL_SLIP = 2.0
const NORMAL_RW_SLIP = NORMAL_SLIP * 0.86
const BRAKE_FW_SLIP = NORMAL_SLIP * 0.8
const BRAKE_RW_SLIP = NORMAL_RW_SLIP * 0.6
const STEERING_MAX = 30.0
const STEERING_SPEED = 200.0
const FRICTION_ADJ_SPEED = 10.0

@onready var wheel_fl: VehicleWheel3D = %wheel_fl
@onready var wheel_fr: VehicleWheel3D = %wheel_fr
@onready var wheel_bl: VehicleWheel3D = %wheel_bl
@onready var wheel_br: VehicleWheel3D = %wheel_br

@export var speed_to_pitch_curve: Curve
@export var speed_to_steering_curve: Curve
@export var skid_to_friction_curve: Curve
const PITCH_MAX_SPEED = 500

var mouse_sensitivity: float
var wheel_target_friction: Dictionary[VehicleWheel3D, float]

func _ready():
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

func _process(delta: float):
	mouse_sensitivity = MOUSE_SENS * 0.25 * 2 * PI / DisplayServer.screen_get_size().y
	
	control_camera()
	update_camera_yaw(delta)
	#print(get_rpm())

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey and event.is_released():
		if event.physical_keycode == KEY_ESCAPE:
			if Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
				Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
			else:
				Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
		elif event.physical_keycode == KEY_R:
			get_tree().reload_current_scene()
	
	if event is InputEventMouseMotion and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
		var delta = event.relative
		%CameraStick.rotation.x -= delta.y * mouse_sensitivity
		%CameraStick.rotation.y -= delta.x * mouse_sensitivity
		
		control_camera()

func _physics_process(delta: float):
	engine_force = 0
	var engine_sound_target: float = 0.5
	if Input.is_action_pressed("throttle"):
		engine_force = ENGINE_FORCE
		engine_sound_target = 1.0
	%EngineSound.volume_linear = move_toward(%EngineSound.volume_linear, engine_sound_target, 2 * delta)
	
	var speediness = get_speediness()
	%EngineSound.pitch_scale = speed_to_pitch_curve.sample(speediness)
	
	wheel_target_friction[wheel_fl] = NORMAL_SLIP
	wheel_target_friction[wheel_fr] = NORMAL_SLIP
	wheel_target_friction[wheel_bl] = NORMAL_RW_SLIP
	wheel_target_friction[wheel_br] = NORMAL_RW_SLIP
	brake = 0
	if Input.is_action_pressed("brake"):
		var apply_slip := false
		if not Input.is_action_pressed("throttle"):
			if get_rpm() > 10:
				brake = BRAKE_FORCE
				apply_slip = true
			else:
				engine_force = -ENGINE_FORCE
		else:
			engine_force *= DRIFT_BONUS
			apply_slip = true
		
		if apply_slip:
			wheel_target_friction[wheel_fl] = BRAKE_FW_SLIP
			wheel_target_friction[wheel_fr] = BRAKE_FW_SLIP
			wheel_target_friction[wheel_bl] = BRAKE_RW_SLIP
			wheel_target_friction[wheel_br] = BRAKE_RW_SLIP
	
	var target_steering = 0
	if Input.is_action_pressed("steer_left"):
		target_steering += 1
	if Input.is_action_pressed("steer_right"):
		target_steering -= 1
	target_steering *= deg_to_rad(speed_to_steering_curve.sample(abs(speediness)))
	steering = move_toward(steering, target_steering, deg_to_rad(STEERING_SPEED) * delta)

	update_wheel(wheel_fl, delta)
	update_wheel(wheel_fr, delta)
	update_wheel(wheel_bl, delta)
	update_wheel(wheel_br, delta)

	#print("%f %f %f %f" % [
		#%wheel_fl.wheel_friction_slip, 
		#%wheel_fr.wheel_friction_slip, 
		#%wheel_bl.wheel_friction_slip, 
		#%wheel_br.wheel_friction_slip])

func _integrate_forces(state: PhysicsDirectBodyState3D) -> void:
	var max_angular_speed = 3.0
	if state.angular_velocity.length() > max_angular_speed:
		state.angular_velocity = state.angular_velocity.normalized() * max_angular_speed
		print("limiting angular velocity")

func get_rpm() -> float:
	var speed_left: float = wheel_bl.get_rpm()
	var speed_right: float = wheel_br.get_rpm()
	return (speed_left + speed_right) / 2

func get_speediness() -> float:
	var rpm = get_rpm()
	rpm /= PITCH_MAX_SPEED
	return clamp(rpm, 0, 1)

func update_wheel(wheel: VehicleWheel3D, delta: float):
	var target := wheel_target_friction[wheel]
	if not wheel.is_in_contact():
		target = 0
	
	wheel.wheel_friction_slip = target
	#wheel.wheel_friction_slip = move_toward(wheel.wheel_friction_slip, target, FRICTION_ADJ_SPEED * delta)
	#wheel.wheel_friction_slip = target * skid_to_friction_curve.sample(wheel.get_skidinfo())

func update_camera_yaw(delta: float):
	if linear_velocity.length() > 1.0:
		var target = -linear_velocity.slide(Vector3.UP).signed_angle_to(Vector3.BACK, Vector3.UP)
		#print(target)
		%CameraStickBase.rotation.y = lerp_angle(target, %CameraStickBase.rotation.y, exp(-5.0 * delta))

func control_camera():
	if Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT):
		%CameraStick.rotation.y = PI
