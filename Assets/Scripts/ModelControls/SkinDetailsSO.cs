using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkinData", menuName = "CharacterData/Skin Data", order = 1)]

public class SkinDetailsSO :ScriptableObject {
    public SkinData baseSkin;
    public List<SkinData> skins;
    public SkinData GetSkinData(int id) {
        if(id == 0)
            return baseSkin;
        else {
            if(skins != null) {
                int index = skins.FindIndex(x => x.id == id);
                if(index >= 0)
                    return skins[index];
            }
        }

        Debug.LogError("Skin not found for ID : " + id + " returning base skin.");
        return baseSkin;
    }
}

[System.Serializable]
public class SkinData {
    public int id;
    public BodyPart body;
    public BodyPart head;
    public Sprite skin_2d_image;
    public bool disable_protection_kit;
    public bool disable_skin_tone;

    public static SkinType GetSkinType(string type) {
        if(!string.IsNullOrEmpty(type)) {
            if(type.ToLower().Contains("epic"))
                return SkinType.EPIC;
            if(type.ToLower().Contains("rare"))
                return SkinType.RARE;
            if(type.ToLower().Contains("uncommon"))
                return SkinType.UNCOMMON;
        }
        return SkinType.BASE;
    }
}

public enum SkinTone {
    TONE_1_NORMAL,
    TONE_2_DARK
}
public enum SkinType {
    EPIC,
    RARE,
    UNCOMMON,
    BASE
}