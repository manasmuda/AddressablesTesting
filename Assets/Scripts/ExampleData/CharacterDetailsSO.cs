using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class CharacterDetailsSO :ScriptableObject
{
	public string nickname;
	public int image_id;
	public PlayerClassification classification;
	public Sprite image;
	public Sprite bust_image;
	public Sprite action_image;
	public CharacterType character_type;
	public BodyPart head_data;
	public Texture baseJerseyTexture;
	public SkinTone skin_tone;
}


public enum CharacterType {
	MALE,
	FEMALE
}

public enum PlayerClassification {
	BATSMAN,
	BOWLER
}

[System.Serializable]
public class Character3DSkinData {
	public List<BodyPart> body_parts;
}

[System.Serializable]
public class MeshData {
	public Mesh mesh;
	public Material[] materials;
	public string head_name;
}
