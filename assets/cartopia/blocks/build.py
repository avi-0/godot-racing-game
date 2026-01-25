import bpy
import os
import TmBlockGenerator as tmbg
from TmBlockGenerator.properties import ShapeGeneratorProperties
from TmBlockGenerator.generator.Constants import (SIDE_SHAPE, MIDDLE_SHAPE, SIDE_SHAPE_BASE)

shape_map = {
    SIDE_SHAPE.FLAT.name: "flat",
    SIDE_SHAPE.HALFBANKED_LEFT.name: "tilt-left1",
    SIDE_SHAPE.HALFBANKED_RIGHT.name: "tilt-right1",
    SIDE_SHAPE.BANKED_LEFT.name: "tilt-left2",
    SIDE_SHAPE.BANKED_LEFT.name: "tilt-right2",
    SIDE_SHAPE.BI_SLOPE_UP.name: "slope-up1",
    SIDE_SHAPE.BI_SLOPE_DOWN.name: "slope-down1",
    SIDE_SHAPE.SLOPE_UP.name: "slope-up2",
    SIDE_SHAPE.SLOPE_DOWN.name: "slope-down2",
}

def gen_block_path(object) -> str:
    start = shape_map[object["block_shape"]["start"]]
    end = shape_map[object["block_shape"]["end"]]

    start_dir = f"{start}/" if start != "flat" else ""
    end_dir = f"to-{end}/" if end != start else ""

    xyz = object["block_shape"]["xyz"]
    height_dir = "/" if xyz[2] == 0 else f"{'up' if xyz[2] > 0 else 'down'}{abs(xyz[2])}/"

    middle_shape = object["block_shape"]["middle"]
    direction = "center" if xyz[0] == 0 else f"{'right' if xyz[0] > 0 else 'left'}"
    if middle_shape == MIDDLE_SHAPE.STRAIGHT.name:
        dimensions = f"straight{xyz[1]}"
    elif middle_shape == MIDDLE_SHAPE.CHICANE.name:
        dimensions = f"chicane-{direction}{abs(xyz[0])}x{xyz[1]}"
    elif middle_shape == MIDDLE_SHAPE.TURN.name:
        dimensions = f"turn-{direction}{abs(xyz[0])}x{xyz[1]}"

    return f"{start_dir}{end_dir}{height_dir}{dimensions}.glb"

def export_block(object, path):
    bpy.ops.object.select_all(action='DESELECT')
    object.select_set(True)

    bpy.ops.object.location_clear()
    bpy.ops.object.scale_clear()

    filepath = bpy.path.abspath(path)
    print(f"Exporting to {filepath}")
    os.makedirs(os.path.dirname(filepath), exist_ok=True)
    bpy.ops.export_scene.gltf(
        filepath=filepath,
        use_selection=True,
    )

for object in bpy.context.scene.objects:
    settings: ShapeGeneratorProperties = bpy.context.scene.tm_shape_generator
    settings.custom_object = object

    tmbg.GeneratorSet.generate_default_road_set(settings)
    
    for created_object in (o for o in bpy.context.scene.objects if "block_shape" in o):
        print(dict(created_object["block_shape"]))
        print(list(created_object["block_shape"]["xyz"]))
        print(created_object.name)

        path = f"//{object.name}/{gen_block_path(created_object)}"
        print(path)
        export_block(created_object, path=path)
    
    break