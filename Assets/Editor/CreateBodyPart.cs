using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class CreateBodyPart :MonoBehaviour {
    public string fileName;
    public BodyPartType type;
    public Transform rig_parent;
    public SkinnedMeshRenderer body_part;

    public void Create() {

        BodyPart part = ScriptableObject.CreateInstance<BodyPart>();
        part.rig_map = GenerateBones(body_part);
        part.mesh = body_part.sharedMesh;
        part.materials = body_part.sharedMaterials;
        part.type = type;
        part.hierarchy_name = body_part.transform.name;

        AssetDatabase.CreateAsset(part, "Assets/BodyParts/" + fileName + ".asset");
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = part;
    }

    private List<string> GenerateBones(SkinnedMeshRenderer from_target) {
        if(from_target != null && from_target.bones != null) {
            List<string> bone_paths = new List<string>();
            foreach(Transform bone in from_target.bones) {
                string path = GetHirachy(bone);
                Transform new_bone = rig_parent.Find(path + bone.name);
                if(new_bone != null) {
                    bone_paths.Add(path + bone.name);
                } else {
                    Debug.LogError("RIG Path not found for path : " + path);
                }
            }
            return bone_paths;
        }
        Debug.LogError("Skinned mesh renderer or bones are null");
        return null;
    }
    private string GetHirachy(Transform child) {
        string find = "";
        GetParent(child, ref find);
        return find;
    }

    private void GetParent(Transform transform, ref string current) {
        if(transform.parent != null) {
            if(transform.parent.name != rig_parent.name) {
                current = transform.parent.name + "/" + current;
                GetParent(transform.parent, ref current);
            }
        }
    }

}

[CustomEditor(typeof(CreateBodyPart))]
public class CreateBodyPart_Editor :Editor {

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        CreateBodyPart part = (CreateBodyPart)target;
        if(GUILayout.Button("Generate Body")) {
            part.Create();
        }
    }
}