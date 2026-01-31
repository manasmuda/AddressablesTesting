using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class MainInstaller : MonoInstaller
{
    public MainSceneAssetHelper mainSceneAssetHelper;
    public MainSceneAssetHelper mainSceneAssetHelper1;

    public override void InstallBindings() {
        Container.BindInterfacesAndSelfTo<AssetDeliveryManager>().AsSingle();
        //Container.BindInstance(mainSceneAssetHelper).AsSingle();
        Container.Bind<ICharacterAssetsFetcher>().WithId("main_scene").FromInstance(mainSceneAssetHelper);
        ///Container.Bind<ICharacterAssetsFetcher>().FromInstance(mainSceneAssetHelper1);
        
    }
}
