using UnityEngine;
using System.Collections;

/// <summary>
/// 消息事件的基类
/// </summary>
public class CBaseEvent
{
    protected CEventType type;
    protected Hashtable arguments;
    protected Object sender;

    public CEventType Type
    {
        get => type;
        set => type = value;
    }

    public IDictionary Params
    {
        get => arguments;
        set => arguments = (value as Hashtable);
    }

    public Object Sender
    {
        get => sender;
        set => sender = value;
    }

    public override string ToString()
    {
        return type + "[" + (sender == null ? "null" : sender.ToString()) + "]";
    }

    public CBaseEvent Clone()
    {
        return new CBaseEvent(type, arguments, sender);
    }
    
    public CBaseEvent(CEventType type, Object sender)
    {
        this.Type = type;
        this.Sender = sender;
        if (this.arguments == null)
        {
            this.arguments = new Hashtable();
        }
    }
    
    public CBaseEvent(CEventType type, Hashtable args, Object sender)
    {
        this.Type = type;
        this.arguments = args;
        this.Sender = sender;
        if (this.arguments == null)
        {
            this.arguments = new Hashtable();
        }
    }
}
