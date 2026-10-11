using System.Collections.Generic;

using System.Windows.Forms;
namespace Common {
    public static class AppSettings {
        public static string CallSign = "VE3ZR";
      //  public static string AccessDBNameCallSign = "VE3ZR";

        
        public static string Grid = "FN03EM";
        public static double Latitude = 43.506220;
        public static double Longitude = -79.651923;  
        public static string ROOT_DIR;
        public static int maxNumOfCharsInQueue = 200;

        public static string DxKeeperDBName = "VA3TNM"; //ACCESS DB

    }


    public class AppSettingDB { // Used to store GUI settings like checkboxes, radiobuttons and textboxes in the database
        public string Code { get; set; }
        public string ValueString { get; set; }
        public string DataType { get; set; }
        public string DefaultValueString { get; set; }
        public static bool? GetBoolSetting(Dictionary<string, AppSettingDB> appSettingsDict,string settingCode, bool? defaultVal) {
            if (appSettingsDict.ContainsKey(settingCode)) {
                return appSettingsDict[settingCode].ValueString == "1";
            }
            return defaultVal;
        }
        public static string GetStringSetting(Dictionary<string,AppSettingDB> appSettingsDict,string settingCode,string defaultVal) {
            if(appSettingsDict.ContainsKey(settingCode)) {
                return appSettingsDict[settingCode].ValueString;
            }
            return defaultVal;
        }
    }
    //public class AppSettingDBContainer {
    //    private Dictionary<string, AppSettingDB> _AppSettingsDict { get; set; } = new Dictionary<string, AppSettingDB>();
    //    public AppSettingDBContainer(Dictionary<string, AppSettingDB> appSettingsDict) {
    //        _AppSettingsDict= appSettingsDict;
    //    }

    //    public  bool? GetBoolSetting(ref System.Windows.Controls.CheckBox checkBox) {
    //        if (_AppSettingsDict.ContainsKey(checkBox.Name)) {
    //            checkBox.IsChecked= appSettingsDict[settingCode].ValueString=="1";
    //        }
    //        return defaultVal;
    //    }


    //    public static bool? GetBoolSetting(Dictionary<string, AppSettingDB> appSettingsDict, string settingCode, bool? defaultVal) {
    //        if (appSettingsDict.ContainsKey(settingCode)) {
    //            return appSettingsDict[settingCode].ValueString=="1";
    //        }
    //        return defaultVal;
    //    }


   // }
}
