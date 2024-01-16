using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureTransDoc.ClsDB
{
    public class ClsJson
    {
        public static string GetJsonParam(JObject json, string key, bool isMust = true, string AvlValList = "")
        {
            string r = json[key].ToString_Mars();
            if (r == "" && isMust)
            {
                throw new Exception("json 參數 :" + key + " 找不到或錯誤的 json 格式\n\njson =" + json.ToString());
            }
            if (AvlValList != "")
            {
                string[] aryAvlVal = AvlValList.Split(new char[] { ',' });
                if (!r.In(aryAvlVal))
                {
                    throw new Exception("json 參數 :" + key + " 參數值不在可接受的清單裡面:" + key + " =" + r + "\n可接受的清單:" + AvlValList + "\njson =" + json.ToString());
                }
            }
            return r;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <param name="key"></param>
        /// <param name="isCheckEmpty">檢查陣列是否為空陣列</param>
        /// <returns></returns>
        public static T[] GetJsonParamArray<T>(JObject json, string key, bool isCheckEmpty)
        {
            string s1 = json[key].ToString_Mars();
            if (s1 == "")
            {
                throw new Exception("json 參數 :" + key + " 找不到或錯誤的 json 格式\n\njson =" + json.ToString());
            }
            JArray a = JArray.Parse(s1);
            T[] r = a.ToObject<T[]>();
            if (isCheckEmpty)
            {
                if (r.Length == 0)
                {
                    throw new Exception("陣列:" + key + " 不得為空陣列\n" + json.ToString());

                }
            }

            return r;
        }

    }
}
