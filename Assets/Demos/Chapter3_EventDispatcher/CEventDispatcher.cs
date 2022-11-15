using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void CEventListenerDelegate(CBaseEvent evt);

public class CEventDispatcher
{
    private static CEventDispatcher _instance;

    public static CEventDispatcher Instance
    {
        get
        {
            if (_instance == null)
                _instance = new CEventDispatcher();
            return _instance;
        }
    }

    private Hashtable _listeners = new Hashtable();

    /// <summary>
    /// 增加事件监听
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="listener"></param>
    public void AddEventListener(CEventType eventType, CEventListenerDelegate listener)
    {
        CEventListenerDelegate cEventListenerDelegate = this._listeners[eventType] as CEventListenerDelegate;
        cEventListenerDelegate = (CEventListenerDelegate)Delegate.Combine(cEventListenerDelegate, listener);
        this._listeners[eventType] = cEventListenerDelegate;
    }

    /// <summary>
    /// 移除事件监听
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="listener"></param>
    public void RemoveEventListener(CEventType eventType, CEventListenerDelegate listener)
    {
        CEventListenerDelegate cEventListenerDelegate = this._listeners[eventType] as CEventListenerDelegate;
        if (cEventListenerDelegate != null)
        {
            cEventListenerDelegate = (CEventListenerDelegate)Delegate.Remove(cEventListenerDelegate, listener);
        }

        this._listeners[eventType] = cEventListenerDelegate;
    }

    /// <summary>
    /// 分发事件消息
    /// </summary>
    /// <param name="evt"></param>
    public void DispatchEvent(CBaseEvent evt)
    {
        CEventListenerDelegate cEventListenerDelegate = this._listeners[evt.Type] as CEventListenerDelegate;
        if (cEventListenerDelegate != null)
        {
            try
            {
                cEventListenerDelegate(evt);
            }
            catch (Exception e)
            {
                throw new System.Exception( string.Concat(new string[]
                {
                    "Error dispatching event",
                    evt.Type.ToString(),
                    ": ",
                    e.Message,
                    " ",
                    e.StackTrace
                }),e);
            }
        }
    }

    public void RemoveAll()
    {
        this._listeners.Clear();
    }
}
