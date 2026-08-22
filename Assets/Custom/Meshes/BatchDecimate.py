import argparse
import os
import sys

import bpy


DEFAULT_DECIMATE_RATIO = 0.20


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)

    for datablocks in (
        bpy.data.meshes,
        bpy.data.materials,
        bpy.data.images,
        bpy.data.armatures,
    ):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def get_triangle_count(obj):
    if obj.type != "MESH":
        return 0

    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated_obj = obj.evaluated_get(depsgraph)
    mesh = evaluated_obj.to_mesh()

    try:
        mesh.calc_loop_triangles()
        return len(mesh.loop_triangles)
    finally:
        evaluated_obj.to_mesh_clear()


def collect_fbx_files(target_path):
    target_path = os.path.abspath(target_path)

    if os.path.isfile(target_path):
        if target_path.lower().endswith(".fbx"):
            return [target_path]
        raise ValueError(f"Not an FBX file: {target_path}")

    if os.path.isdir(target_path):
        fbx_files = []

        for root, dirs, files in os.walk(target_path):
            dirs.sort()

            for filename in sorted(files):
                if filename.lower().endswith(".fbx"):
                    fbx_files.append(os.path.join(root, filename))

        return fbx_files

    raise FileNotFoundError(target_path)


def process_fbx(fbx_path, decimate_ratio):
    print()
    print("=" * 80)
    print(f"[PROCESS] {fbx_path}")

    clear_scene()

    bpy.ops.import_scene.fbx(
        filepath=fbx_path,
        use_image_search=True,
    )

    mesh_objects = [
        obj
        for obj in bpy.context.scene.objects
        if obj.type == "MESH"
    ]

    if not mesh_objects:
        print("[SKIP] Mesh not found.")
        return False

    before_triangles = sum(get_triangle_count(obj) for obj in mesh_objects)
    print(f"[BEFORE] {before_triangles:,} tris")

    for obj in mesh_objects:
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)

        modifier = obj.modifiers.new(
            name="BatchDecimate",
            type="DECIMATE",
        )
        modifier.decimate_type = "COLLAPSE"
        modifier.ratio = decimate_ratio

        bpy.ops.object.modifier_apply(modifier=modifier.name)
        obj.select_set(False)

    after_triangles = sum(get_triangle_count(obj) for obj in mesh_objects)
    print(f"[AFTER ] {after_triangles:,} tris")

    if before_triangles > 0:
        reduction = (1.0 - after_triangles / before_triangles) * 100.0
        print(f"[REDUCE] {reduction:.2f}%")

    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=False,
        axis_forward="-Z",
        axis_up="Y",
        use_mesh_modifiers=True,
        path_mode="AUTO",
        add_leaf_bones=False,
    )

    print(f"[SAVED ] {fbx_path}")
    return True


def parse_args():
    parser = argparse.ArgumentParser(
        description="Overwrite FBX files with Blender decimate results."
    )
    parser.add_argument(
        "--target",
        required=True,
        help="FBX file path or folder path to process.",
    )
    parser.add_argument(
        "--ratio",
        type=float,
        default=DEFAULT_DECIMATE_RATIO,
        help="Decimate keep ratio. 0.20 keeps about 20 percent and reduces about 80 percent.",
    )

    if "--" in sys.argv:
        argv = sys.argv[sys.argv.index("--") + 1 :]
    else:
        argv = sys.argv[1:]

    return parser.parse_args(argv)


def main():
    args = parse_args()

    if not 0.0 < args.ratio <= 1.0:
        raise ValueError("--ratio must be greater than 0 and less than or equal to 1.")

    fbx_files = collect_fbx_files(args.target)

    print("=" * 80)
    print("SinkPoint FBX Batch Decimate")
    print(f"Target: {os.path.abspath(args.target)}")
    print(f"Ratio : {args.ratio}")
    print(f"Files : {len(fbx_files)}")
    print("=" * 80)

    success_count = 0
    fail_count = 0

    for index, fbx_path in enumerate(fbx_files, start=1):
        print()
        print(f"[{index}/{len(fbx_files)}] {fbx_path}")

        try:
            if process_fbx(fbx_path, args.ratio):
                success_count += 1
        except Exception as e:
            fail_count += 1
            print(f"[ERROR] {fbx_path}")
            print(e)

    print()
    print("=" * 80)
    print("FINISHED")
    print(f"Success: {success_count}")
    print(f"Failed : {fail_count}")
    print("=" * 80)

    if fail_count:
        sys.exit(1)


if __name__ == "__main__":
    main()
