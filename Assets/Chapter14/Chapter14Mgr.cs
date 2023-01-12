using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Chapter14Mgr : MonoBehaviour
{
    private Button _btnGreen;
    private GameObject _imgGreenGO;

    void Awake()
    {
        _btnGreen = transform.Find("ButtonGreen").GetComponent<Button>();
        _imgGreenGO = transform.Find("ImageGreen").gameObject;

        // _btnGreen.onClick.AddListener(() => { Debug.Log("123"); });
        _btnGreen.onClick.AddListener(OnBtnGreenClick);
    }

    void OnBtnGreenClick()
    {
        bool isActive = !_imgGreenGO.activeInHierarchy;
        Debug.Log($"isActive = {isActive}");
        _imgGreenGO.SetActive(isActive);
    }
}
