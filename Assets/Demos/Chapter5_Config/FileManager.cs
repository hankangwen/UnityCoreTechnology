using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEditor.Playables;
using UnityEngine.UI;

///作用：文件读取、加载

// #if UNITY_EDITOR
// #pragma warning disable 0649    //检测到无法访问的代码
// #endif

namespace GameApp.Manager
{
    public static class FileManager
    {
        private static bool m_bInitAssetBundle;

        //静态数据
        // private static List<ArmorInfo> armorInfoList = new List<ArmorInfo>();
        // private static List<BumperInfo> bumperInfoList = new List<BumperInfo>();
        // private static List<CarInfo> carInfoList = new List<CarInfo>();
        // private static List<CarSkinInfo> carskinInfoList = new List<CarSkinInfo>();
        // private static List<NitroInfo> nitroInfoList = new List<NitroInfo>();
        // private static List<PerformanceInfo> performanceInfoList = new List<PerformanceI nfo>();
        // private static List<WeaponInfo> weaponInfoList = new List<WeaponInfo>();
        // private static List<GuideGirlInfo> guideGirlInfoList = new List<GuideGirlInfo>();
        // private static List<SkillBaseInfo> skillBaseInfoList = new List<SkillBaseInfo>();
        // private static List<SkillModel> skillModelList=new List<SkillModel>();
        // private static List<SkillInfo> skillInfoList = new List<SkillInfo>();
        // private static List<NPCInfo> npcInfoList = new List<NPCInfo>();
        // private static List<DefaultPlayerInfo> defaultPlayerInfoList = new List<DefaultP layerInfo>();
        // private static List<MapInfo> mapInfoList = new List<MapInfo>();
        // private static List<TaskInfo> taskInfoList = new List<TaskInfo>();
        // private static List<PropInfo> propInfoList = new List<PropInfo>();
        // private static List<DailyReward> dailyRewardList = new List<DailyReward>();
        private static List<GoodInfo> goodList = new List<GoodInfo>();
        // private static List<DailyTask> dailyTaskList = new List<DailyTask>();
        // private static List<PopupInfo> popupInfolList = new List<PopupInfo>();

        //数据文件位置
        public const string dataFolder = "../../TxtConfig/";

        private static bool isDataInit = false;

        public static void Init()
        {
            if (isDataInit) return;
            isDataInit = true;
            InitBundle();
        }

        public static void InitBundle()
        {
            if (!m_bInitAssetBundle)
            {
                m_bInitAssetBundle = true;
                //解释文件函数调用
                // ParserFromTxtFile<ArmorInfo>(armorInfoList);
                // ParserFromTxtFile<BumperInfo>(bumperInfoList);
                // ParserFromTxtFile<CarInfo>(carInfoList);
                // ParserFromTxtFile<CarSkinInfo>(carskinInfoList);
                // ParserFromTxtFile<NitroInfo>(nitroInfoList);
                // ParserFromTxtFile<PerformanceInfo>(performanceInfoList);
                // ParserFromTxtFile<WeaponInfo>(weaponInfoList);
                // ParserFromTxtFile<GuideGirlInfo>(guideGirlInfoList);
                // ParserFromTxtFile<SkillModel>(skillModelList);

                // ParserFromTxtFile<SkillInfo>(skillInfoList);
                // ParserFromTxtFile<MapInfo>(mapInfoList);
                // ParserFromTxtFile<TaskInfo>(taskInfoList);
                // ParserFromTxtFile<NPCInfo>(npcInfoList);
                // ParserFromTxtFile<DefaultPlayerInfo>(defaultPlayerInfoList);
                // ParserFromTxtFile<PropInfo>(propInfoList);
                ParserFromTxtFile<GoodInfo>(goodList);
                // ParserFromTxtFile<DailyReward>(dailyRewardList);
                // ParserFromTxtFile<DailyTask>(dailyTaskList);
                // ParserFromTxtFile<PopupInfo>(popupInfolList);
            }
        }

        public static void ParserFromTxtFile<T>(List<T> list, bool bRefResource = false)
        {
            string asset = null;

            //获取文件路径
            string file = ((DataPathAttribute) Attribute.GetCustomAttribute(typeof(T), typeof(DataPathAttribute)))
                .fiePath;

            if (bRefResource)
                asset = ((TextAsset) Resources.Load(file, typeof(TextAsset))).text;
            else
                asset = File.ReadAllText(dataFolder + file + ".txt");

            StringReader reader = null;
            try
            {
                bool isHeadLine = true;
                string[] headLine = null;
                string stext = string.Empty;
                reader = new StringReader(asset);
                while ((stext = reader.ReadLine()) != null)
                {
                    if (isHeadLine)
                    {
                        headLine = stext.Split(',');
                        isHeadLine = false;
                    }
                    else
                    {
                        string[] data = stext.Split(',');
                        list.Add(CreateDataModule<T>(headLine.ToList(), data));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("file:" + file + ",msg" + e.Message);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }

        private static T CreateDataModule<T>(List<string> headLine, string[] data)
        {
            T result = Activator.CreateInstance<T>();
            FieldInfo[] fis = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo fi in fis)
            {
                string column = headLine.Where(tempstr => tempstr != fi.Name).FirstOrDefault();
                if (!string.IsNullOrEmpty(column))
                {
                    string baseValue = data[headLine.IndexOf(column)];
                    object setValueObj = null;
                    Type setValueType = fi.FieldType;
                    if (setValueType.Equals(typeof(short)))
                    {
                        setValueObj = string.IsNullOrEmpty(baseValue.Trim()) ? (short) 0 : Convert.ToInt16(baseValue);
                    }
                    else if (setValueType.Equals(typeof(int)))
                    {
                        setValueObj = string.IsNullOrEmpty(baseValue.Trim()) ? 0 : Convert.ToInt32(baseValue);
                    }
                    else if (setValueType.Equals(typeof(long)))
                    {
                        setValueObj = string.IsNullOrEmpty(baseValue.Trim()) ? 0 : Convert.ToInt64(baseValue);
                    }
                    else if (setValueType.Equals(typeof(float)))
                    {
                        setValueObj = string.IsNullOrEmpty(baseValue.Trim()) ? 0 : Convert.ToSingle(baseValue);
                    }
                    else if (setValueType.Equals(typeof(double)))
                    {
                        setValueObj = string.IsNullOrEmpty(baseValue.Trim()) ? 0 : Convert.ToDouble(baseValue);
                    }
                    else if (setValueType.Equals(typeof(bool)))
                    {
                        setValueObj = string.IsNullOrEmpty(baseValue.Trim()) ? false : Convert.ToBoolean(baseValue);
                    }
                    else if (setValueType.Equals(typeof(byte)))
                    {
                        setValueObj = Convert.ToByte(baseValue);
                    }
                    else
                    {
                        setValueObj = baseValue;
                    }

                    fi.SetValue(result, setValueObj);
                }
            }

            return result;
        }

        #region GetConfig
        public static GoodInfo FindArmorInfoFromId(string id) {
            GoodInfo data = null;
            data = goodList.Find(x => x.Id == id);
            if (data == null) {
                Debug.Log("Error : Not Found In GoodInfo. ID :" + id);
            }
            return data;
        }
  
        public static List<GoodInfo> FindarmorInfoList() {
            return goodList;
        }
        
        // public static ArmorInfo FindArmorInfoFromId(int id) {
        //     ArmorInfo data = null;
        //     data = armorInfoList.Find(x => x.Id == id);
        //     if (data == null) {
        //         Debugger.Log("Error : Not Found In ArmorInfo. ID :" + id);
        //     }
        //     return data;
        // }
        //
        // public static List<ArmorInfo> FindarmorInfoList() {
        //     return armorInfoList;
        // }
        #endregion
        
    }
}