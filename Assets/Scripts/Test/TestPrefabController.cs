using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class TestPrefabController : MonoBehaviour
{

    public string index;
    public AssetAddressReference addressReference;

    public Image image;
    public Button button1, button3;

    private ICharacterAssetsFetcher _assetHelper;

    //[Inject]
    public void Construct(ICharacterAssetsFetcher mainSceneAssetHelper) {
        _assetHelper = mainSceneAssetHelper;
    }

    void Start()
    {
        button1.onClick.RemoveAllListeners();
        button1.onClick.AddListener(delegate {
            Debug.LogError("button 1 "+index);
        });


        button3.onClick.RemoveAllListeners();
        button3.onClick.AddListener(delegate {
            Debug.LogError("button 3 " + index);
        });
        button1.onClick.Invoke();
        button3.onClick.Invoke();
        _assetHelper.LoadImage(image);
        LoadImage();
    }

    private async void LoadImage() {
        //image.sprite = await AddressableAssetManager.LoadLatestObjectAsync<Sprite>(addressReference);
    }

    
}
