using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CEventDispatcherTest : MonoBehaviour
{
    void Start()
    {
        CEventDispatcher.Instance.AddEventListener(CEventType.GAME_WIN, OnGameWin);
    }

    void OnGameWin(CBaseEvent evt)
    {
        Debug.Log("OnGameWin(CBaseEvent evt)");
        Debug.Log(evt.ToString());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            CEventDispatcher.Instance.DispatchEvent(new CBaseEvent(CEventType.GAME_WIN, this));
        }
    }

    void OnDestroy()
    {
        CEventDispatcher.Instance.RemoveEventListener(CEventType.GAME_WIN, OnGameWin);
    }
}
