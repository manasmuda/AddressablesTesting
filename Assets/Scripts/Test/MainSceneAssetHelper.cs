using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainSceneAssetHelper : MonoBehaviour, ICharacterAssetsFetcher
{

    public AssetAddressReference addressReference;

    public async void LoadImage(Image image) {
        image.sprite = await AddressableAssetManager.LoadLatestObjectAsync<Sprite>(addressReference);
    }
}

public interface ICharacterAssetsFetcher {
    public void LoadImage(Image image);
}