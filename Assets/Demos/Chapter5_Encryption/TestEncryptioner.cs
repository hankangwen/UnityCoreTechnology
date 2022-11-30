using System.Collections;
using System.Collections.Generic;
using System.IO;
using Encryption;
using UnityEngine;

public class TestEncryptioner : MonoBehaviour
{
    void Start()
    {
        // string password = "mybirthday";
        // string r1 = Encryptioner.OBFS(password);
        // string r2 = Encryptioner.GetSHA512Password(password);
        //
        // Debug.Log(r1);
        // Debug.Log(r2);

        string zipFile = "zipFile.txt";
        Debug.Log(Application.persistentDataPath);
        string zipPath = Path.Combine(Application.persistentDataPath, zipFile);
        Debug.Log(zipPath);
    }
}
