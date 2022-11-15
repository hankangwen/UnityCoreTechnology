namespace GameApp.Manager
{
    #region shop数据模型
    [DataPath(FileManager.dataFolder + "shop")]
    public class GoodInfo{
        public int Id;
        public string Category;
        public string PropId;
        public string PropName;
        public int CurrencyType;
        public string Price;
        public float UpgradePrice;
    }
    #endregion
    
    #region 武器数据模型
    [System.Serializable]
    [DataPath(FileManager.dataFolder + "weapon")]
    public class WeaponInfo {
        public int Id;
        public int Type;
        public string Name;
        public string Description;
        public string Resources;
        public string Quality;
        public int Attack;
        public int UpgradeAttack;
        public int Magazine;
        public int Range;
        public int Speed;
        public float Rateoffire;
    }
    #endregion
    
    #region 保险杠数据模型
    [System.Serializable]
    [DataPath(FileManager.dataFolder + "bumper")]
    public class BumperInfo {
        public int Id;
        public string Name;
        public string Description;
        public string Resources;
        public int Damage;
        public int UpgradeDamage;
        public int Defence;
        public int UpgradeDefence;
        public int ParryHurt;
    }
    #endregion
    
    #region 装甲数据模型
    [System.Serializable]
    [DataPath(FileManager.dataFolder + "armor")]
    public class ArmorInfo {
        public int Id;
        public string Name;
        public string Description;
        public string Resources;
        public int Defence;
        public int UpgradeDefence;
    }
    #endregion
    
    #region 汽车数据模型
    [DataPath(FileManager.dataFolder + "car")]
    public class CarInfo {
        public int Id;
        public string Name;
        public string Description;
        public string Templat;
        public string Quality;
        public string Resources;
        public int Control;
        public int MaxRpm;
        public int Tire;
        public float TireRadius;
        public int Engine;
        public int Turbine;
        public int Drivetrain;
        public float DrivetrainRatio;
        public int Nitrous;
        public int SkinId;
        public string CarSkin;
  
        public int[] CarSkinIds {
            get {
                string[] strs = CarSkin.Split(';');
                int[] result = new int[strs.Length];
                for (int i = 0; i < strs.Length; i++) {
                    result[i] = int.Parse(strs[i]);
                }
                return result;
            }
        }
        public int WheelSkin;
        public int HP;
        public int NPCHP;
    }
    #endregion
}