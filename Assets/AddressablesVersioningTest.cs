using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using UnityEngine.AddressableAssets;

public class AddressablesVersioningTest : MonoBehaviour
{
    public Transform content;

    private AssetDeliveryManager _deliveryManager;

    public List<AssetReferenceGameObject> testObjectReferences;

    [Inject]
    public void Construct(AssetDeliveryManager deliveryManager) {
        _deliveryManager = deliveryManager;
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.unityLogger.logEnabled = true;
        _deliveryManager.SetDownloadCompleteListener(delegate {
            InstantiateTestPrefabs();
        });
    }

    private void InstantiateTestPrefabs() {
        for(int i = 0; i < testObjectReferences.Count; i++) {
            AddressableAssetManager.TryLoadAndInstatiate(testObjectReferences[i], content);
        }
    }
}
