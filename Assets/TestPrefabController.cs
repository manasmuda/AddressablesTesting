using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestPrefabController : MonoBehaviour
{

    public string index;

    public Button button1, button2;

    void Start()
    {
        button1.onClick.RemoveAllListeners();
        button1.onClick.AddListener(delegate {
            Debug.LogError("button 1 "+index);
        });

        button2.onClick.RemoveAllListeners();
        button2.onClick.AddListener(delegate {
            Debug.LogError("button 2 "+index);
        });
    }

    
}
