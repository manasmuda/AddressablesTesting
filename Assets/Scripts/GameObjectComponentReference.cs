using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

[Serializable]
public class GameObjectComponentReference<T> : AssetReferenceGameObject where T : Component {

    private T _component;
    
    public GameObjectComponentReference(string guid) : base(guid) { }

    public void SetComponent(T component) {
        _component = component;
    }

    public T GetComponent() {
        return _component;
    }
    
}
