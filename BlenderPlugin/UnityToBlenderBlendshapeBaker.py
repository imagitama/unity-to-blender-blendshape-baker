

import textwrap
import bpy
import os

bl_info = {
    # required
    'name': 'Unity To Blender Blendshape Baker',
    'blender': (3, 1, 0),
    'category': 'Object',
    # optional
    'version': (1, 0, 0),
    'author': 'Jared Williams',
    'description': 'Bake blendshapes from Unity.',
}


class ShapeKeyItem():
    def __init__(self, mesh_name, shape_key_name, amount, isBaking):
        self.mesh_name = mesh_name
        self.shape_key_name = shape_key_name
        self.amount = amount
        self.isBaking = isBaking


class UnityToBlenderBlendshapeBakerPanel(bpy.types.Panel):
    bl_label = 'Unity To Blender Blendshape Baker'
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'

    def draw(self, context):
        layout = self.layout
        wrapper = textwrap.TextWrapper()
        list = wrapper.wrap(
            text='Select the armature that contains your meshes, the file you exported from Unity then click Apply to rename your shape keys.')
        for text in list:
            row = layout.row(align=True)
            row.alignment = 'EXPAND'
            row.label(text=text)
        col = layout.column()
        for (prop_name, _) in PROPS:
            row = col.row()
            row.prop(context.scene, prop_name)
        layout.operator(
            'utbbb.unity_to_blender_blendshape_baker', text='Apply')


class UnityToBlenderBlendshapeBaker(bpy.types.Operator):
    bl_idname = "utbbb.unity_to_blender_blendshape_baker"
    bl_label = "Unity To Blender Blendshape Baker"
    bl_options = {"UNDO"}

    def invoke(self, context, event):
        absolute_file_path = bpy.path.abspath(context.scene.FilePath)
        filename, extension = os.path.splitext(absolute_file_path)

        print('Selected file:', absolute_file_path)
        print('File name:', filename)
        print('File extension:', extension)

        shape_key_items = []
        mesh_names = []

        with open(absolute_file_path) as f:
            for line in f:
                chunks = line.rstrip('\n').split(" ")
                print(chunks)
                mesh_name_and_shape_key_name_chunks = ''.join(
                    chunks[0:-2]).split("/")
                mesh_name = mesh_name_and_shape_key_name_chunks[0]
                shape_key_name = mesh_name_and_shape_key_name_chunks[1]
                amount = float(chunks.pop(-2)) / 100
                isBaking = True if chunks.pop(-1) == 'Y' else False
                shape_key_items.append(
                    ShapeKeyItem(mesh_name, shape_key_name, amount, isBaking))
                if (mesh_name not in mesh_names):
                    mesh_names.append(mesh_name)

        for item in shape_key_items:
            print('Item:', item.mesh_name, item.shape_key_name,
                  item.amount, 'Y' if item.isBaking else 'N')

        armature = context.scene.Armature

        if not armature.type == 'ARMATURE':
            raise ValueError("You must select an armature")

        meshes = []

        for obj in bpy.data.objects:
            if obj.parent == armature:
                print('Detected mesh:', obj.name)
                meshes.append(obj)
                pass

        for mesh in meshes:
            shape_keys = mesh.data.shape_keys.key_blocks
            for shape_key in shape_keys:
                print('Shape Key:', mesh.name, shape_key.name)
                if (shape_key.name == 'Basis' or '_bake' in shape_key.name):
                    continue
                matches = list(filter(
                    lambda x: x.mesh_name == mesh.name and x.shape_key_name == shape_key.name, shape_key_items))
                if len(matches) == 0:
                    print('Warning: Not found in list, ignoring...')
                    continue
                item = matches[0]
                shape_key.value = item.amount
                if item.isBaking:
                    print('Adding suffix...')
                    shape_key.name = shape_key.name + "_bake"

        return {"FINISHED"}


CLASSES = [
    UnityToBlenderBlendshapeBaker,
    UnityToBlenderBlendshapeBakerPanel
]

PROPS = [
    ('Armature', bpy.props.PointerProperty(name="Armature", type=bpy.types.Object)),
    ('FilePath', bpy.props.StringProperty(
        name="Text File Path", subtype="FILE_PATH")),
]


def register():
    for (prop_name, prop_value) in PROPS:
        setattr(bpy.types.Scene, prop_name, prop_value)

    for klass in CLASSES:
        bpy.utils.register_class(klass)


def unregister():
    for (prop_name, _) in PROPS:
        delattr(bpy.types.Scene, prop_name)

    for klass in CLASSES:
        bpy.utils.unregister_class(klass)


if __name__ == '__main__':
    register()
