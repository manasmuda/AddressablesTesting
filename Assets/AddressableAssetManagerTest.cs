using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableAssetManagerTest : MonoBehaviour
{

    public AssetReferenceGameObject[] assets;
    public int pos;

    public InputField number_field;
    public Text current_pos;

    public Button load;
    public Button unload;
    public Button destroy;
    public Button setNumber;

    public Transform objectPlace;

    // Start is called before the first frame update
    void Start()
    {
        load.onClick.RemoveAllListeners();
        load.onClick.AddListener(delegate {
            AddressableAssetManager.TryLoadAndInstatiate(assets[pos], objectPlace , pos.ToString()+"_object");
        });
        unload.onClick.RemoveAllListeners();
        unload.onClick.AddListener(delegate {
            AddressableAssetManager.Unload(assets[pos]);
        });
        destroy.onClick.RemoveAllListeners();
        destroy.onClick.AddListener(delegate {
            GameObject destroy_object = GameObject.Find(pos.ToString() + "_object");
            AddressableAssetManager.Unload(assets[pos]);
            Destroy(destroy_object);
        });
        setNumber.onClick.RemoveAllListeners();
        setNumber.onClick.AddListener(delegate {
            pos = System.Convert.ToInt32(number_field.text);
            number_field.text = "";
            current_pos.text = pos.ToString();
        });
    }
}
