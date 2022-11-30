using System.Security.Cryptography;
using System.Text;

namespace Encryption
{
    /// <summary>
    /// 加密算法的函数参数是开发者自定义的字符串，
    /// 在该字符串的基础上通过加密算法生成新的字符串用于压缩文件加密。
    /// </summary>
    public static class Encryptioner
    {
        //OBFS加密算法
        public static string OBFS(string str)
        {
            int length = str.Length;
            char[] array = new char[length];
            for (int i = 0; i < array.Length; i++)
            {
                char c = str[i];
                byte b = (byte) (c ^ length - i);
                byte b2 = (byte) ((c >> 8) ^ i);
                array[i] = (char) (b2 << 8 | b);
            }

            return new string(array);
        }
        
        //SHA512加密算法
        public static string GetSHA512Password(string password)
        {
            byte[] bytes = Encoding.UTF7.GetBytes(password);
            SHA512 shaM = new SHA512Managed();
            byte[] result = shaM.ComputeHash(bytes);
            StringBuilder sb = new StringBuilder();
            foreach (var num in result)
            {
                sb.AppendFormat("{0:x2}", num);
            }

            return sb.ToString();
        }
    }    
}

