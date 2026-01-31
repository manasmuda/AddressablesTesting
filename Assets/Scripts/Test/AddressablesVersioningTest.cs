using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;
using UnityEngine.AddressableAssets;

public class AddressablesVersioningTest : MonoBehaviour
{
    public Transform content;

    private AssetDeliveryManager _deliveryManager;
    private DiContainer _container;

    public List<AssetReferenceGameObject> testObjectReferences;

    public AssetReferenceGameObject testObject;
    public AssetAddressReference addressReference;

    [Inject]
    public void Construct(AssetDeliveryManager deliveryManager, DiContainer diContainer) {
        _deliveryManager = deliveryManager;
        _container = diContainer;
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.unityLogger.logEnabled = true;
        _deliveryManager.SetDownloadCompleteListener(delegate {
            //AddressableAssetManager.TryLoadAndInstatiate(addressReference, content);
            //AddressableAssetManager.TryLoadAndInstatiate(addressReference, content);
            //addressReference.LoadAssetAsync<GameObject>().Completed += (op) => { Debug.LogError(op.Result); };
            //testObjectReferences.Add(new AssetReferenceGameObject(""))
            //InstantiateTestPrefabs();
           // Debug.LogError("Before");
            //Addressables.LoadAssetAsync<GameObject>("TestPrefab2").Completed += (op) => { Debug.LogError("Ended"); };
        });
        WaitAndLoad();
    }

    public async void WaitAndLoad() {
        await Task.Delay(2000);
        _deliveryManager.SetDownloadCompleteListener(delegate {
            AddressableAssetManager.TryLoadAndInstantiateGameObject(testObject, content, _container);
        });
    }

    private void InstantiateTestPrefabs() {
        for(int i = 0; i < testObjectReferences.Count; i++) {
            AssetReferenceGameObject currentReference = testObjectReferences[i];
            AddressableAssetManager.TryLoadAndInstatiate(currentReference, content, delegate {
               
            });
        }
    }
}
