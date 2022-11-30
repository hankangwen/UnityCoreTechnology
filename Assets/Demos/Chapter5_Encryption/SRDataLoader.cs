using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SRDataLoader : MonoBehaviour
{
    public SRDataLoader Instance;
    
    void Awake()
    {
        this.Instance = this;
        
        DontDestroyOnLoad(gameObject);
    }

    
}
