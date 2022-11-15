using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GamePb;
using UnityEngine;

public class TestProtobuf : MonoBehaviour
{
    void Start()
    {
        LoginMsg loginMsg = new LoginMsg
        {
            userName = "test",
            userPsw = "99999"
        };
        
        byte[] bs_proto = EncodeProto(loginMsg);
        Debug.Log(BitConverter.ToString(bs_proto));
        
        // 获取协议名
        string protoName = loginMsg.ToString();
        // 解码
        // ProtoBuf.IExtensible m;
        // m = DecodeProto(protoName, bs_proto, 0, bs_proto.Length);
        // LoginMsg m2 = m as LoginMsg;
        LoginMsg m2 = DecodeProto<LoginMsg>(protoName, bs_proto, 0, bs_proto.Length);
        Debug.Log(m2.userName);
        Debug.Log(m2.userPsw);
    }

    // 生成二进制
    public static byte[] EncodeProto(ProtoBuf.IExtensible msgBase)
    {
        using (var memory = new MemoryStream())
        {
            ProtoBuf.Serializer.Serialize(memory, msgBase);
            return memory.ToArray();
        }
    }
    
    // 解析二进制
    public static ProtoBuf.IExtensible DecodeProto(string protoName, byte[] bytes, int offset, int count)
    {
        using (var memory = new MemoryStream(bytes, offset, count))
        {
            Type t = Type.GetType(protoName);
            var result = ProtoBuf.Serializer.NonGeneric.Deserialize(t, memory);
            return (ProtoBuf.IExtensible)result;
        }
    }
    
    public static T DecodeProto<T>(string protoName, byte[] bytes, int offset, int count)
    {
        using (var memory = new MemoryStream(bytes, offset, count))
        {
            Type t = Type.GetType(protoName);
            var result = ProtoBuf.Serializer.NonGeneric.Deserialize(t, memory);
            return (T)result;
        }
    }
}
