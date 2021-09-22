using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ImagesData", menuName = "ImagesData/New Set", order = 1)]
public class ImagesData : ScriptableObject {
	public ImageDetails default_image;
	public List<SetOfImages> Images;

	public Sprite getIcon(string id, string type) {
		foreach(SetOfImages item in Images) {
			if(item.type.ToLower() == type.ToLower()) {
				if(item.images != null && item.images.Count > 0) {
					foreach(ImageDetails image in item.images) {
						if(image.id == id) {
							return image.icon;
						}
					}
				}
			}
		}
		return default_image.icon;
	}

	public Sprite getIcon(string type) {
		foreach(SetOfImages item in Images) {
			if(item.type.ToLower() == type.ToLower()) {
				if(item.images != null && item.images.Count > 0) {
					return item.images[0].icon;
				}
			}
		}
		return default_image.icon;
	}

	public Sprite getIcon(int id, string type) {
		return getIcon(id.ToString(), type);
	}
}

[System.Serializable]

public class SetOfImages {
	public string type; //CurrencyV2 type
	public List<ImageDetails> images;
}


[System.Serializable]
public class ImageDetails {
	public string id;//CurrencyV2 id
	public string name;//Can be anything
	public Sprite icon;
}
