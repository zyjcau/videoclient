using System;
using System.Configuration;
using System.Text.RegularExpressions;

namespace VideoClient.Util
{
    public class Config
    {
        public static string GetStringConfig(string key)
        {
            string file = System.Windows.Forms.Application.ExecutablePath;
            Configuration config = ConfigurationManager.OpenExeConfiguration(file);
            return config.AppSettings.Settings[key].Value;
        }

        public static int GetIntegerConfig(string key)
        {
            string config = GetStringConfig(key);
            int intValue = IsNumeric(config) ? int.Parse(config) : -1;
            return intValue;
        }

        public static bool GetBooleanConfig(string key)
        {
            string config = GetStringConfig(key);
            return "true".Equals(config);
        }

        public static void SetStringConfig(string key, String value)
        {
            if (String.IsNullOrEmpty(key) || String.IsNullOrEmpty(value))
            {
                return;
            }

            string file = System.Windows.Forms.Application.ExecutablePath;
            Configuration config = ConfigurationManager.OpenExeConfiguration(file);
            config.AppSettings.Settings[key].Value = value;
            config.Save();
            // config.Save(ConfigurationSaveMode.Modified);
            // ConfigurationManager.RefreshSection("appSettings");
        }
        
        public static bool IsNumeric(string value)
        {
            return Regex.IsMatch(value, @"^[+-]?\d*[.]?\d*$");
        }
    }
}