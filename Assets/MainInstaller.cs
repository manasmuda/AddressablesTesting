using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class MainInstaller : MonoInstaller
{
    public override void InstallBindings() {
        Container.BindInterfacesAndSelfTo<AssetDeliveryManager>().AsSingle();
        //
    }
}
