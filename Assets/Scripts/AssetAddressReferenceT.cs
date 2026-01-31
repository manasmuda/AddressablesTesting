using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

[Serializable]
public class AssetAddressReferenceT<T> : IKeyEvaluator where T: Object
{
    [SerializeField]
    private string address;

    public AssetAddressReferenceT(string address) {
        this.address = address;
    }

    AsyncOperationHandle<T> m_Operation;
    
      /// <summary>
    /// The AsyncOperationHandle currently being used by the AssetReference.
    /// For example, if you call AssetReference.LoadAssetAsync, this property will return a handle to that operation.
    /// </summary>
    public AsyncOperationHandle<T> OperationHandle {
        get {
            return m_Operation;
        }
    }

    /// <summary>
    /// The actual key used to request the asset at runtime. RuntimeKeyIsValid() can be used to determine if this reference was set.
    /// </summary>
    public virtual object RuntimeKey {
        get {
            return address;
        }
    }

    public bool RuntimeKeyIsValid() {
        if(RuntimeKey is string) {
            return !string.IsNullOrEmpty(RuntimeKey as string);
        } else {
            return false;
        }
    }

    /// <summary>
    /// Returns the state of the internal operation.
    /// </summary>
    /// <returns>True if the operation is valid.</returns>
    public bool IsValid() {
        return m_Operation.IsValid();
    }

    /// <summary>
    /// Get the loading status of the internal operation.
    /// </summary>
    public bool IsDone {
        get {
            return m_Operation.IsDone;
        }
    }

    /// <summary>
    /// Load the referenced asset as type TObject.
    /// This cannot be used a second time until the first load is released. If you wish to call load multiple times
    /// on an AssetReference, use <see cref="Addressables.LoadAssetAsync{TObject}(object)"/> and pass your AssetReference in as the key.
    /// </summary>
    /// <typeparam name="TObject">The object type.</typeparam>
    /// <returns>The load operation if there is not a valid cached operation, otherwise return default operation.</returns>
    public virtual AsyncOperationHandle<T> LoadAssetAsync() {
        AsyncOperationHandle<T> result = default(AsyncOperationHandle<T>);
        if(m_Operation.IsValid())
            Debug.LogError("Attempting to load AssetReference that has already been loaded. Handle is exposed through getter OperationHandle");
        else {
            result = Addressables.LoadAssetAsync<T>(RuntimeKey);
            m_Operation = result;
        }
        return result;
    }
    
    /// <summary>
    /// The loaded asset.  This value is only set after the AsyncOperationHandle returned from LoadAssetAsync completes.
    /// It will not be set if only InstantiateAsync is called.  It will be set to null if release is called.
    /// </summary>
    public virtual UnityEngine.Object Asset {
        get {
            if(!m_Operation.IsValid())
                return null;

            return m_Operation.Result as UnityEngine.Object;
        }
    }

    

    /// <summary>
    /// Release the internal operation handle.
    /// </summary>
    public virtual void ReleaseAsset() {
        if(!m_Operation.IsValid()) {
            Debug.LogWarning("Cannot release a null or unloaded asset.");
            return;
        }
        Addressables.Release(m_Operation);
        m_Operation = default(AsyncOperationHandle<T>);
    }

    public bool IsLoaded() {
        return IsValid() && Asset != null;
    }

    public bool IsLoading() {
        return IsValid() && OperationHandle.Status == AsyncOperationStatus.None && Asset == null;
    }

}
