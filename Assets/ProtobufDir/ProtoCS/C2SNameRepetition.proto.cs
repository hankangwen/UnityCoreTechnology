//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from: C2SNameRepetition.proto
// Note: requires additional types generated from: common.proto
namespace GamePb
{
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"C2SNameRepetition")]
  public partial class C2SNameRepetition : global::ProtoBuf.IExtensible
  {
    public C2SNameRepetition() {}
    
    private GamePb.msgcharinfo _charinfo = null;
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"charinfo", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue(null)]
    public GamePb.msgcharinfo charinfo
    {
      get { return _charinfo; }
      set { _charinfo = value; }
    }
    private uint _mapid = default(uint);
    [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"mapid", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(uint))]
    public uint mapid
    {
      get { return _mapid; }
      set { _mapid = value; }
    }
    private uint _cityid = default(uint);
    [global::ProtoBuf.ProtoMember(3, IsRequired = false, Name=@"cityid", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(uint))]
    public uint cityid
    {
      get { return _cityid; }
      set { _cityid = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
}