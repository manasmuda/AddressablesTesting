using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelAttire : MonoBehaviour {
    public SkinnedMeshRenderer body;
    public Transform rig;
    public SkinDetailsSO skinData;

    public void RenderBody(int id) {
        var skin = skinData.GetSkinData(id);
        RenderBody(body, skin.body, rig);
    }

    private void RenderBody(SkinnedMeshRenderer mesh, BodyPart body, Transform rig_parent) {
        if(mesh != null && body != null) {
            if(!string.IsNullOrEmpty(body.hierarchy_name)) {
                mesh.transform.name = body.hierarchy_name;
            }
            if(body.mesh != null) {
                mesh.sharedMesh = body.mesh;
                MapBones(mesh, rig_parent, body.rig_map);
                SetMaterials(mesh, body.materials);
            }
        } else {
            Debug.LogError("Mesh or Body Part data is null");
        }
    }

    private void MapBones(SkinnedMeshRenderer mesh, Transform root, List<string> bone_mapping) {
        if(mesh != null && root != null && bone_mapping != null) {
            List<Transform> bones = new List<Transform>();
            for(int i = 0; i < bone_mapping.Count; i++) {
                Transform bone = root.Find(bone_mapping[i]);
                if(bone != null) {
                    bones.Add(bone);
                } else {
                    Debug.LogWarning("Bone path not found in root " + root.name + " path : " + bone_mapping[i]);
                }
            }
            if(bones.Count > 0) {
                mesh.bones = bones.ToArray();
            }
        } else {
            Debug.LogError("Cannot map bones, something is null and I am too lazy to check");
        }
    }
    private void SetMaterials(SkinnedMeshRenderer mesh, Material[] materials) {
        if(mesh != null && materials != null && materials.Length > 0) {
            mesh.sharedMaterials = materials;
        }
    }
}
