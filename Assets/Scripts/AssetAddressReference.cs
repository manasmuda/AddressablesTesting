using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;


[Serializable]
public class AssetAddressReference :IKeyEvaluator, IAddressablesReference {

    [SerializeField]
    public string address;

    AsyncOperationHandle m_Operation;
    AsyncOperationHandle<IList<IResourceLocation>> upgraded_locations_operation;

    /// <summary>
    /// The AsyncOperationHandle currently being used by the AssetReference.
    /// For example, if you call AssetReference.LoadAssetAsync, this property will return a handle to that operation.
    /// </summary>
    public AsyncOperationHandle OperationHandle {
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
    /// See the [Loading Addressable Assets](xref:addressables-api-load-asset-async) documentation for more details.
    /// </summary>
    /// <typeparam name="TObject">The object type.</typeparam>
    /// <returns>The load operation if there is not a valid cached operation, otherwise return default operation.</returns>
    public virtual AsyncOperationHandle<TObject> LoadAssetAsync<TObject>() {
        AsyncOperationHandle<TObject> result = default(AsyncOperationHandle<TObject>);
        if(m_Operation.IsValid())
            Debug.LogError("Attempting to load AssetReference that has already been loaded. Handle is exposed through getter OperationHandle");
        else {
            result = Addressables.LoadAssetAsync<TObject>(RuntimeKey);
            m_Operation = result;
        }
        return result;
    }

    public virtual AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(IResourceLocation resourceLocation) {
        AsyncOperationHandle<TObject> result = default(AsyncOperationHandle<TObject>);
        if(m_Operation.IsValid())
            Debug.LogError("Attempting to load AssetReference that has already been loaded. Handle is exposed through getter OperationHandle");
        else {
            result = Addressables.LoadAssetAsync<TObject>(resourceLocation);
            m_Operation = result;
        }
        return result;
    }

    public virtual AsyncOperationHandle<IList<IResourceLocation>> GetUpgradedResourceLocations() {
        if(!upgraded_locations_operation.IsValid()) {
            upgraded_locations_operation = Addressables.LoadResourceLocationsAsync(new List<object> { RuntimeKey, "Upgraded" }, Addressables.MergeMode.Intersection);
        }
        return upgraded_locations_operation;
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
        m_Operation = default(AsyncOperationHandle);
    }

    public virtual bool ValidateAsset(UnityEngine.Object obj) {
        return true;
    }

    public bool IsLoaded() {
        return this.IsValid() && this.Asset != null;
    }

    public bool IsLoading() {
        return this.IsValid() && this.OperationHandle.Status == AsyncOperationStatus.None && this.Asset == null;
    }
}


[Serializable]
public class AssetAddressReference<TObject>: AssetAddressReference where TObject : UnityEngine.Object {

    /// <summary>
    /// Load the referenced asset as type TObject.
    /// This cannot be used a second time until the first load is released. If you wish to call load multiple times
    /// on an AssetReference, use<see cref="Addressables.LoadAssetAsync{TObject}(object)"/> and pass your AssetReference in as the key.
    /// See the [Loading Addressable Assets](xref:addressables-api-load-asset-async) documentation for more details.
    /// </summary>
    /// <returns>The load operation.</returns>
    public virtual AsyncOperationHandle<TObject> LoadAssetAsync() {
        return LoadAssetAsync<TObject>();
    }

    /// <inheritdoc/>
    public override bool ValidateAsset(UnityEngine.Object obj) {
        var type = obj.GetType();
        return typeof(TObject).IsAssignableFrom(type);
    }

}


#region Asset Refence with interface
[Serializable]
public class AssetGUIDReference : AssetReference , IAddressablesReference {
    
    public bool IsLoaded() {
        return this.IsValid() && this.Asset != null;
    }

    public bool IsLoading() {
        return this.IsValid() && this.OperationHandle.Status == AsyncOperationStatus.None && this.Asset == null;
    }

}

[Serializable]
public class AssetGUIDReferenceT<T> : AssetReferenceT<T>, IAddressablesReference where T:UnityEngine.Object {

    public AssetGUIDReferenceT(string guid) : base(guid) {
    }

    public bool IsLoaded() {
        return this.IsValid() && this.Asset != null;
    }

    public bool IsLoading() {
        return this.IsValid() && this.OperationHandle.Status == AsyncOperationStatus.None && this.Asset == null;
    }

}

#endregion

public interface IAddressablesReference {

    public bool IsLoaded();

    public bool IsLoading();

}
