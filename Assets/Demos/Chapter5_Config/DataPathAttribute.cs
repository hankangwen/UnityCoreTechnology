using System;

///<summary>
///注释，各个数据对应的文件
///</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class DataPathAttribute : Attribute {
    public string fiePath { get; set; }
    public DataPathAttribute(string _fiePath) {
        fiePath = _fiePath;
    }
}