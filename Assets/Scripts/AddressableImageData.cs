using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using Cysharp.Threading.Tasks;

[Serializable]
public class AddressableImageData : AssetReferenceT<ImagesData> {

    private int ref_count = 0;

    public AddressableImageData(string guid) : base(guid) {
    }

    public void LoadImageData() {
        AddressableAssetManager.TryGetOrLoadObjectAsync(this, out AsyncOperationHandle<ImagesData> handle);
    }

    public async UniTask<Sprite> LoadImageAsync(string id, string type) {
        ImagesData imagesData = await AddressableAssetManager.TryGetOrLoadObjectAsync<ImagesData>(this);
        if(imagesData != null) {
            ref_count++;
            Sprite result_sprite = imagesData.getIcon(id, type);
            return result_sprite;
        } else {
            Debug.LogError("Image Data is null");
            return null;
        }
    }

    public async UniTask<Sprite> LoadImageAsync(int id, string type) {
        return await LoadImageAsync(Convert.ToString(id), type);
    }

    public async void LoadImageAsync(Image image ,string id, string type) {
        Sprite sprite = await LoadImageAsync(id, type);
        image.sprite = sprite;
    }
    
    public async void LoadImageAsync(Image image ,int id, string type) {
        LoadImageAsync(image, Convert.ToString(id), type);
    }


    public void UnloadImageData() {
        ref_count--;
        if(ref_count == 0) {
            AddressableAssetManager.Unload(this);
        }
    }

    public void ForceUnloadImageData() {
        ref_count = 0;
        AddressableAssetManager.Unload(this);
    }
}

