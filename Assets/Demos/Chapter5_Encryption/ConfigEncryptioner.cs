using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Text;
using Encryption;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;

/// <summary>
/// 通过OBFS或者SHA512对文件加密，返回经过加密的字符串，
/// 同时调用SaveConfigXMLToZip对文件进行压缩加密
/// </summary>
public class ConfigEncryptioner
{
    private const string ConfigurationFile = "config.txt";
    private const string LocalizationFile = "translation.txt";
    private /*const*/ string _configurationZipPwd1 = Encryptioner.OBFS("AAA");
    private /*const*/ string _configurationZipPwd2 = Encryptioner.GetSHA512Password("AAA");
    private const string DownLoadURL = "http://127.0.0.1";
    private const string VersionFileName = "version.txt";
    private const string ZipFileName = "zip_name.zip";

    private bool _dataLoaded = false;
    private bool _downloadingVersionFile = false;
    private bool _downloadingZip = false;
    private string _versionString = string.Empty;
    
    private bool _stopDownloading = false;
    public bool StopDownloading
    {
        get => _stopDownloading;
        set => _stopDownloading = value;
    }
    
#if UNITY_EDITOR
    protected void SaveConfigXMLToZip()
    {
        // using (ZipFile zipFile = new ZipFile(Encoding.UTF8))
        // {
        //     zipFile.Password = configurationZipPwd;
        //     zipFile.AddEntry(configurationFile, configuration.bytes);
        //     zipFile.AddEntry(localizationFile, localization.bytes);
        //     string zipPath = Path.Combine(Application.persistentDataPath, configurationZipFile);
        //     LogTool.Log("Saving configuration in \"" + zipPath + "\"");
        //     zipFile.Save(zipPath);
        // }
    }
#endif
    
    #region Coroutines

    IEnumerator DownloadVersionFile()
    {
        _downloadingVersionFile = true;

        UnityWebRequest webRequest = new UnityWebRequest(DownLoadURL + VersionFileName);

        var waitForEndOfFrame = new WaitForEndOfFrame();
        while (!webRequest.isDone)
        {
            yield return waitForEndOfFrame;
        }

        if (webRequest.isDone && string.IsNullOrEmpty(webRequest.error))
        {
            _versionString = webRequest.result.ToString();
        }
        else
        {
            _versionString = "2022113017"; //default version.txt
        }
        
        webRequest.Dispose();
        
        Debug.Log("VERSION NUMBER: " + _versionString);
        _downloadingVersionFile = false;
        PlayerPrefs.SetInt("last_vn", Int32.Parse(_versionString));
    }

    IEnumerator DownloadZip()
    {
        _downloadingZip = true;

        var startTime = Time.realtimeSinceStartup;
        UnityWebRequest webRequest = new UnityWebRequest(DownLoadURL + ZipFileName);

        var waitForEndOfFrame = new WaitForEndOfFrame();
        while (!webRequest.isDone)
        {
            if (_stopDownloading)
            {
                _downloadingZip = false;
                _stopDownloading = false;
                Debug.Log("Download zip STOPPED!");
            }

            yield return waitForEndOfFrame;
        }

        if (webRequest.isDone && string.IsNullOrEmpty(webRequest.error))
        {
            //Success
            Debug.Log("DOWNLOADING ZIP COMPLETED! Duration: " +
                      (Time.realtimeSinceStartup - startTime));
            using (FileStream fs = new FileStream(Path.Combine(Application.persistentDataPath, ConfigurationFile),
                FileMode.Create))
            {
                fs.Seek(0, SeekOrigin.Begin);
                string str = webRequest.result.ToString();
                byte[] bytes = Encoding.UTF8.GetBytes(str);
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush();
            }
            webRequest.Dispose();
            if (!_downloadingZip)
            {
                Debug.Log("Download zip OK!");
                yield break;
            }
            else
            {
                Debug.Log("Download configuration OK, configurations will be loaded from new zip!");
            }
        }
        else
        {
            //Fail
            webRequest.Dispose();
            _downloadingZip = false;
            _stopDownloading = false;
            yield break;
        }
        
        if(!_dataLoaded && !_stopDownloading)
            TryLoadingXMLsFromZip();
        
        _downloadingZip = false;
        _stopDownloading = false;
    }
    
    #endregion

    protected void TryLoadingXMLsFromZip()
    {
        string zipPath = Path.Combine(Application.persistentDataPath, ZipFileName);
        if (!File.Exists(zipPath))
        {
            Debug.Log("File not found!");
            // this.ParseConfigXML()
            // this.ParseLocalizationXML()
            return;
        }

        using (ZipFile.Open(zipPath, ZipArchiveMode.Read))
        {
            
        }
    }
}












