using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DontDestroyOnLoad : MonoBehaviour
{
    static GameObject instance;
    private void Start()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this.gameObject;
        DontDestroyOnLoad(gameObject);
    }
}
