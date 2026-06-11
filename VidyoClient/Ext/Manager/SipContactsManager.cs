using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace VideoClient.Manager
{
    public class SipContactsManager
    {
        private string userHardDeviceListJsonFilePath;
        public List<HardDevice> userHardDeviceList = new List<HardDevice>();
        
        public void QueryUserHardDeviceList(string username)
        {
            log("start query " + username + "'s hd list.");

            string cacheDir = System.Windows.Forms.Application.UserAppDataPath + "\\cache\\";
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            userHardDeviceListJsonFilePath = cacheDir + username + "_hd_list.json";

            if (File.Exists(userHardDeviceListJsonFilePath))
            {
                string json = GetJsonFile(userHardDeviceListJsonFilePath);
                try
                {
                    List<HardDevice> list = JsonConvert.DeserializeObject<List<HardDevice>>(json);
                    if (null != list && list.Count > 0)
                    {
                        foreach (HardDevice device in list)
                        {
                            userHardDeviceList.Add(device);
                        }
                    }

                    log("query " + username + "'s hd list success,num -> " + userHardDeviceList.Count);
                }
                catch (Exception e)
                {
                    log("query " + username + "'s hd list failed.json format exception.");
                }
            }
            else
            {
                FileStream createdFile = File.Create(userHardDeviceListJsonFilePath);
                createdFile.Close();
                log("query " + username + "'s hd list failed,file not exist,now create it.");
            }

            // //test code : add some test data
            // HardDevice test1 = new HardDevice();
            // test1.name = "测试终端1";
            // test1.callNumber = "88##192.168.1.18888";
            // AddUserHardDeviceInfo(test1);
            // HardDevice test2 = new HardDevice();
            // test2.name = "测试终端2";
            // test2.callNumber = "88##192.168.1.22222";
            // AddUserHardDeviceInfo(test2);
        }

        public void AddUserHardDeviceInfo(HardDevice hardDeviceInfo)
        {
            if (userHardDeviceListJsonFilePath != null)
            {
                userHardDeviceList.Add(hardDeviceInfo);
                string json = JsonConvert.SerializeObject(userHardDeviceList);
                WriteJsonFile(userHardDeviceListJsonFilePath, json);
                log("AddUserHardDeviceInfo success,The data has been refresh.\n" + json);
            }
            else
            {
                log("AddUserHardDeviceInfo failed, maybe not login.");
            }
        }

        public void RemoveUserHardDeviceInfo(string name)
        {
            if (userHardDeviceListJsonFilePath != null)
            {
                for (int i = 0; i < userHardDeviceList.Count; i++)
                {
                    HardDevice device = userHardDeviceList[i];
                    if (name == device.name)
                    {
                        userHardDeviceList.Remove(device);
                        log("found device info " + device.name);
                    }
                }

                string json = JsonConvert.SerializeObject(userHardDeviceList);

                WriteJsonFile(userHardDeviceListJsonFilePath, json);
                log("RemoveUserHardDeviceInfo success,The data has been refresh.\n" + json);
            }
            else
            {
                log("RemoveUserHardDeviceInfo failed, maybe not login.");
            }
        }

        public void ClearUserHardDeviceList()
        {
            userHardDeviceList.Clear();
            log("ClearUserHardDeviceList on user logout.");
        }

        /**
         * 硬件视频终端设备信息
         */
        public class HardDevice
        {
            public string name;
            public string callNumber;
        }


        public void log(string content)
        {
            // _logger.Info("[SipContactsManager] " + content);
        }


        /// <summary>
        /// 将序列化的json字符串内容写入Json文件，并且保存
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="jsonConents">Json内容</param>
        private void WriteJsonFile(string path, string jsonConents)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite,
                FileShare.ReadWrite))
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonConents);
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush();

                // byte[] re = new byte[(int)fs.Length];
                // int r = fs.Read(re, 0, re.Length);
                // string myStr = System.Text.Encoding.UTF8.GetString(re);
                // log("read:\n"+myStr);

                fs.Close();


                // using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                // {
                //     sw.WriteLine(jsonConents, 0, jsonConents.Length);
                //     sw.Flush();
                //     sw.Close();
                // }
            }
        }

        /// <summary>
        /// 获取到本地的Json文件并且解析返回对应的json字符串
        /// </summary>
        /// <param name="filepath">文件路径</param>
        /// <returns></returns>
        private string GetJsonFile(string filepath)
        {
            string json = string.Empty;
            using (FileStream fs = new FileStream(filepath, FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite,
                FileShare.ReadWrite))
            {
                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                {
                    json = sr.ReadToEnd().ToString();
                }
            }

            return json;
        }
    }
}