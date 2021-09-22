using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Logger :MonoBehaviour {

    public Text log;

    public void Log(string text) {
        log.text += "\n" + text;
    }
    public void ClearLog() {
        log.text = "";
    }
}
