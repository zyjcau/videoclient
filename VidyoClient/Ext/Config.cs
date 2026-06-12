using System;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace VideoClient.Util
{
    public class Config
    {
        private static readonly object UserConfigLock = new object();

        public static string GetStringConfig(string key)
        {
            if (String.IsNullOrEmpty(key))
            {
                return null;
            }

            string userSetting = GetUserStringConfig(key);
            if (userSetting != null)
            {
                return userSetting;
            }

            KeyValueConfigurationElement appSetting = GetConfig(ConfigurationUserLevel.None)
                .AppSettings.Settings[key];
            return appSetting == null ? null : appSetting.Value;
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

            SaveUserStringConfig(key, value);
        }

        private static Configuration GetConfig(ConfigurationUserLevel userLevel)
        {
            return ConfigurationManager.OpenExeConfiguration(userLevel);
        }

        private static string GetUserStringConfig(string key)
        {
            lock (UserConfigLock)
            {
                XmlDocument doc = LoadUserConfigDocument(false);
                if (doc == null)
                {
                    return null;
                }

                XmlElement element = FindUserConfigElement(doc, key);
                return element == null ? null : element.GetAttribute("value");
            }
        }

        private static void SaveUserStringConfig(string key, string value)
        {
            lock (UserConfigLock)
            {
                XmlDocument doc = LoadUserConfigDocument(true);
                XmlElement root = doc.DocumentElement;
                XmlElement element = FindUserConfigElement(doc, key);
                if (element == null)
                {
                    element = doc.CreateElement("add");
                    element.SetAttribute("key", key);
                    root.AppendChild(element);
                }

                element.SetAttribute("value", value);
                string path = GetUserConfigPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                doc.Save(path);
            }
        }

        private static XmlElement FindUserConfigElement(XmlDocument doc, string key)
        {
            XmlNodeList nodes = doc.SelectNodes("/appSettings/add");
            foreach (XmlNode node in nodes)
            {
                XmlElement element = node as XmlElement;
                if (element != null && element.GetAttribute("key") == key)
                {
                    return element;
                }
            }

            return null;
        }

        private static XmlDocument LoadUserConfigDocument(bool createIfMissing)
        {
            string path = GetUserConfigPath();
            XmlDocument doc = new XmlDocument();
            if (File.Exists(path))
            {
                doc.Load(path);
                if (doc.DocumentElement != null && doc.DocumentElement.Name == "appSettings")
                {
                    return doc;
                }
            }

            if (!createIfMissing)
            {
                return null;
            }

            doc.LoadXml("<appSettings />");
            return doc;
        }

        private static string GetUserConfigPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RemoteConsultation",
                "VideoClient",
                "user.config"
            );
        }

        public static bool IsNumeric(string value)
        {
            return Regex.IsMatch(value, @"^[+-]?\d*[.]?\d*$");
        }
    }
}
