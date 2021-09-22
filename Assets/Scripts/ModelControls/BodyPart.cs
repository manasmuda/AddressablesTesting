using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BodyPartData", menuName = "CharacterData/BpdyPart")]
public class BodyPart :ScriptableObject {
    public string hierarchy_name;
    public BodyPartType type;
    public Mesh mesh;
    public List<string> rig_map;
    public Material[] materials;
}

public enum BodyPartType {
    HEAD,
    BODY
}
