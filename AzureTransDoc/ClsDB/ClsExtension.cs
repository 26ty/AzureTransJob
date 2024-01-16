using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AzureTransDoc.ClsDB
{
    public static class ClsExtension
    {

        public static string RemoveRight(this string s, int cnt)
        {
            return s.Remove(s.Length - cnt);
        }

        public static string JoinBy(this string[] aryStr, string separator)
        {
            return String.Join(separator, aryStr);
        }


        public static int[] AllIndexesOf(this string str, string value)
        {
            if (String.IsNullOrEmpty(value))
                return new int[] { };
            List<int> indexes = new List<int>();
            for (int index = 0; ; index += value.Length)
            {
                index = str.IndexOf(value, index);
                if (index == -1)
                    return indexes.ToArray();
                indexes.Add(index);
            }
        }

        /// <summary>
        /// 將逗號分隔的字串轉換成 json 陣列
        /// ex:"a01,a02,a03"  ==> ["a01","a02","a03]
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ToJsonArray(this string sList)
        {
            string r = "[";
            foreach (string s in sList.Split(new char[] { ',' }))
            {
                r += s.ToSqlStr() + ",";
            }
            r = r.RemoveRight(1) + "]";
            return r;
        }


        static public string ToSqlStr(this string s)
        {

            if (!String.IsNullOrEmpty(s))
            {
                s = s.Replace("'", "''");
                return "'" + s + "'";
            }
            else
            {
                return "NULL";
            }
        }


        static public string ToSqlStr(this double? o)
        {

            if (o == null)
            {
                return "NULL";
            }
            else
            {
                return o.ToString();
            }
        }



        static public string ToSqlIn(this string sList)
        {
            string[] lst = sList.Split(new char[] { ',' });
            StringBuilder r = new StringBuilder();
            foreach (string item in lst)
            {
                r.Append("'" + item.Trim() + "',");
            }
            if (r.Length > 0) r.Remove(r.Length - 1, 1);
            return r.ToString();

        }






        public static string ToSqlValue(this object v)
        {
            string r = "";
            if (v == null)
            {
                return "null";
            }

            switch (v.GetType().ToString())
            {
                case "System.Boolean":
                    r = Convert.ToBoolean(v) ? "1" : "0";
                    break;
                case "System.Byte":
                case "System.SByte":
                case "System.Decimal":
                case "System.Double":
                case "System.Single":
                case "System.Int32":
                case "System.UInt32":
                case "System.Int64":
                case "System.UInt64":
                case "System.Int16":
                case "System.UInt16":
                    r = v.ToString();
                    break;
                case "System.Char":
                case "System.String":
                    r = ClsUty.sqlstr(v as string);
                    break;
                case "System.DateTime":
                    r = ClsUty.sqldate(Convert.ToDateTime(v));
                    break;
                default:
                    throw new Exception("In SQLValue() 未識別的型別 !Type =" + v.GetType().ToString());
            }

            return r;

        }




        static public string RemoveQuote(this string s)
        {
            char l = s[0];
            char r = s[s.Length - 1];
            if ((l == '\"' && r == '\"') || (l == '\'' && r == '\''))
            {
                return s.Substring(1, s.Length - 2);
            }
            else
            {
                return s;
            }
        }


        static public string ToDblQuote(this string s)
        {
            return "\"" + s + "\"";
        }

        static public string ToSingleQuote(this string s)
        {
            return "'" + s + "'";
        }



        static public string AddQueryString(this string s, string key, string value)
        {
            if (s.Contains("?"))
            {
                return s + "&" + key + "=" + value;
            }
            else
            {
                return s + "?" + key + "=" + value;
            }
        }

        public static string Segment(this string line, char Separted, int nSeg)
        {
            if (nSeg == 0) return "";

            string[] a = line.Split(new char[] { Separted });
            if (Math.Abs(nSeg) > a.Length) return "";
            if (nSeg >= 1)
            {
                return a[nSeg - 1];
            }
            else
            {
                return a[a.Length + nSeg];
            }
        }

        static public string GetPropVal(this object myObject)
        {
            if (myObject == null) return "";
            string r = "";
            Type myType = myObject.GetType();
            foreach (PropertyInfo prop in myType.GetProperties())
            {
                object propValue = prop.GetValue(myObject, null);
                r += prop.Name + "=" + (propValue == null ? "null" : propValue.ToString()) + "\n";

                // Do something with propValue
            }
            return r;
        }


        static public string Left(this string s, int leng)
        {
            if (s.Length <= leng)
                return s;
            else
                return s.Substring(0, leng);
        }


        static public string Right(this string s, int leng)
        {
            if (s.Length <= leng)
                return s;
            else
                return s.Substring(s.Length - leng, leng);
        }



        static public string LeftBytes(this string s, int leng)
        {
            byte[] byteS = System.Text.Encoding.UTF8.GetBytes(s);
            if (byteS.Length <= leng) return s;

            leng -= 2;  //避免字尾遇到半字中文字，故-2 。當字尾遇到半個中文字時，UTF8.GetBytes()會取完整的中文字，所以最多會多出2個 byte，
            string r = System.Text.Encoding.UTF8.GetString(byteS, 0, leng);
            return r;
        }


        static public int LengthBytes(this string s)
        {
            byte[] byteS = System.Text.Encoding.UTF8.GetBytes(s);
            return byteS.Length;
        }


        public const string MarsStdDateTimeFormat = "yyyy/M/d H:m:s";        //定義標準日期格式
        //public static bool In<T>(this T val, params T[] values) where T : struct {
        //    return values.Contains(val);
        //}


        public static bool In<T>(this T val, params T[] values)
        {
            return values.Contains(val);
        }

        public static bool InList(this string s, string sList)
        {
            return s.In(sList.Split(new char[] { ',' }));
        }

        /// <summary>
        /// 時間取整數
        /// which is used as follows:
        /// dateTime = dateTime.Truncate(TimeSpan.FromMilliseconds(1)); // 取到千分秒
        /// dateTime = dateTime.Truncate(TimeSpan.FromSeconds(1)); // 取到秒
        /// dateTime = dateTime.Truncate(TimeSpan.FromMinutes(1)); // 取到分
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero) return dateTime; // Or could throw an ArgumentException
            return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }

        public static string ToString_MarsStdFormat(this DateTime dt)
        {
            return dt.ToString(MarsStdDateTimeFormat);
        }

        public static DateTime ToDateTime_MarsStdFormat(this string sDT)
        {
            return DateTime.ParseExact(sDT, MarsStdDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);
        }

        public static DateTime ToDateTime_MarsFormat(this string sDT, string format)
        {
            return DateTime.ParseExact(sDT, format, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);
        }

        /// <summary>
        /// 轉換字串形態的民國日期為日期形態
        /// 來源格式要求：yy/mm/dd    yy=民國年
        /// </summary>
        /// <param name="sDT"></param>
        /// <returns></returns>
        public static DateTime ToDateTime_MarsChinese(this string sDT)
        {
            string[] sDatePart = sDT.Trim().Split(new char[] { '/' });
            string yy, mm, dd;
            if (sDatePart.Length != 3)
            {
                throw new Exception("民國日期字串格式不符 !");
            }
            yy = sDatePart[0];
            mm = sDatePart[1];
            dd = sDatePart[2];
            string chiDate = (yy.ToInt_Mars() + 1911).ToString() + "/" + mm + "/" + dd;
            return DateTime.ParseExact(chiDate, "yyyy/M/d", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);
        }



        public static string ToString_Mars(this object o, bool ConvertZeroToNull = false)
        {
            string r;
            if (o == null || o == System.DBNull.Value)
            {
                return "";
            }
            else if (o.GetType().ToString() == "System.Boolean")
            {
                return o.ToString().ToLower();
            }
            else
            {
                r = o.ToString();
                if (ConvertZeroToNull)
                {
                    if (r == "0") r = "NULL";
                }
            }
            return r;

        }

        public static int ToInt_Mars(this object o)
        {
            if (o == null) return 0;
            if (o is Enum) return (int)o;
            string s = o.ToString();
            string type = o.GetType().ToString();
            switch (type)
            {
                case "System.DBNull":
                    return 0;
                case "System.Byte":
                case "System.SByte":
                case "System.Decimal":
                case "System.Double":
                case "System.Single":
                case "System.Int32":
                case "System.UInt32":
                case "System.Int64":
                case "System.UInt64":
                case "System.Int16":
                case "System.UInt16":
                case "System.Web.UI.WebControls.Unit":
                case "System.Boolean":
                case "Newtonsoft.Json.Linq.JValue":
                    if (s == "")
                        return 0;
                    else
                        return Convert.ToInt32(o);
                case "System.Char":
                case "System.String":
                    if (o.ToString() == "")
                    {
                        return 0;
                    }
                    else
                    {
                        return Convert.ToInt32(o);
                    }

                default:
                    throw new Exception("In ToInt_Mars() 未識別的型別 !Type =" + o.GetType().ToString());

            }
        }

        public static long ToInt64_Mars(this object o)
        {
            if (o == null) return 0;
            if (o is Enum) return (int)o;
            string type = o.GetType().ToString();
            switch (type)
            {
                case "System.DBNull":
                    return 0;
                case "System.Byte":
                case "System.SByte":
                case "System.Decimal":
                case "System.Double":
                case "System.Single":
                case "System.Int32":
                case "System.UInt32":
                case "System.Int64":
                case "System.UInt64":
                case "System.Int16":
                case "System.UInt16":
                    return Convert.ToInt64(o);
                case "System.Char":
                case "System.String":
                    return Int64.Parse(o.ToString().Trim(), NumberStyles.AllowThousands);
                default:
                    throw new Exception("In ToInt_Mars() 未識別的型別 !Type =" + o.GetType().ToString());
            }

        }

        public static DateTime ToDateTime_Mars(this object o)
        {

            if (o == System.DBNull.Value || o == null)
            {
                throw new Exception("In ToDateTime() 無法轉換 " + o.GetType().ToString());
            }
            else
            {
                return Convert.ToDateTime(o);
            }
        }



        public static DateTime ToDateTimeLocal_Mars(this object o)
        {

            if (o == System.DBNull.Value || o == null)
            {
                throw new Exception("In ToDateTime() 無法轉換 " + o.GetType().ToString());
            }
            else
            {
                DateTime r = DateTime.SpecifyKind(Convert.ToDateTime(o), DateTimeKind.Local);
                return r;
            }
        }


        public static DateTime? ToDateTimeNullLocal_Mars(this object o)
        {

            if (o == System.DBNull.Value || o == null)
            {
                return null;
            }
            else
            {
                DateTime r = DateTime.SpecifyKind(Convert.ToDateTime(o), DateTimeKind.Local);
                return r;
            }
        }

        public static DateTime? ToDateTimeNull_Mars(this object o)
        {
            if (o == System.DBNull.Value || o == null)
            {
                return null;
            }
            else
            {
                return Convert.ToDateTime(o);
            }
        }

        public static double ToDouble_Mars(this object o)
        {
            double r;
            if (o == null)
            {
                r = 0.0;
            }
            else
            {
                try
                {
                    r = double.Parse(o.ToString());
                }
                catch
                {
                    r = 0.0;
                }
            }
            return r;
        }
        public static float ToFloat_Mars(this object o)
        {
            float r;
            if (o == null)
            {
                r = 0.0f;
            }
            else
            {
                try
                {
                    r = float.Parse(o.ToString());
                }
                catch
                {
                    r = 0.0f;
                }
            }
            return r;
        }

        public static decimal ToDecimal_Mars(this object o)
        {
            string s = o.ToString();
            decimal r;
            if (s == "")
            {
                r = 0;
            }
            else
            {

                try
                {
                    //Double d = double.Parse(s);
                    r = decimal.Parse(s, NumberStyles.AllowThousands | NumberStyles.Float);
                }
                catch
                {
                    r = 0;
                }
            }
            return r;
        }

        public static bool ToBool_Mars(this object p)
        {
            if (p == null || p == DBNull.Value) return false;
            string s = p.ToString().Trim().ToLower();
            if (s == "") return false;
            if (s.In(new string[] { "true", "y", "1", "-1" })) return true;

            try
            {
                return Convert.ToBoolean(p);
            }
            catch
            {
                return false;
            }
        }



        static public string ToDblQuote(this object v)
        {
            if (v == null)
            {
                return "null";
            }
            switch (v.GetType().ToString())
            {
                case "System.String":
                    return "\"" + v + "\"";
                default:
                    return v.ToString();
            }

        }



        static public string sqlIn(this string sList)
        {
            string[] lst = sList.Split(new char[] { ',' });
            StringBuilder r = new StringBuilder();
            foreach (string item in lst)
            {
                r.Append("'" + item.Trim() + "',");
            }
            if (r.Length > 0) r.Remove(r.Length - 1, 1);
            return r.ToString();

        }

        static public string ToSqlStr_DateTime(this DateTime? d)
        {
            if (d == null)
            {
                return "";
            }
            else
            {
                return ((DateTime)d).ToSqlStr_DateTime();
            }
        }


        static public int ToSql_Bool(this bool b)
        {
            return !b ? 0 : 1;

        }

        static public int? ToSql_Bool(this bool? b)
        {
            if (b == true)
            {
                return 1;
            }
            else if (b == false)
            {
                return 0;
            }
            else
            {
                return null;
            }

        }



        static public string ToSqlStr_DateTime(this DateTime d)
        {
            return "TO_DATE('" + d.ToString("yyyy-MM-dd HH:mm:ss") + "','yyyy-mm-dd HH24:MI:SS')";
        }

        static public string ToSqlStr_TimeStamp(this DateTime tm)
        {
            return "TO_TIMESTAMP('" + tm.ToString("yyyy-MM-dd HH:mm:ss.fff") + "','yyyy-mm-dd HH24:MI:SS.FF')";
        }

        static public string ToSqlStr_TimeStampTZ(this DateTime tm)
        {
            string s = tm.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff zzz");
            return "TO_TIMESTAMP_TZ('" + tm.ToString("yyyy-MM-dd HH:mm:ss.fff zzz") + "','yyyy-mm-dd HH24:MI:SS.FF TZH:TZM')";
        }

        public static string ToTypeString(this string s, Type type)
        {
            string r = "";
            switch (type.ToString())
            {
                case "System.String":
                    r = s.ToSqlStr();
                    break;
                default:
                    return s;
            }

            return r;
        }

        /// <summary>
        /// 依傳入的字串及欲轉換的 dbColumnType 轉成對應的字串輸出
        /// </summary>
        /// <param name="o"></param>
        /// <param name="dbColumnType"></param>
        /// <returns></returns>
        public static string toSqlValue(this JToken o, string dbColumnType)
        {
            string s = o.ToString();
            string sLower = s.ToLower();


            string r = "";
            switch (dbColumnType)
            {
                case "System.String":
                    r = s.ToSqlStr();
                    break;
                case "System.Int16":
                    if (sLower == "false")
                        r = "0";
                    else if (sLower == "true")
                        r = "1";
                    else
                        r = s;
                    break;
                case "System.DateTime":
                    DateTime d = o.ToString().ToDateTime_MarsStdFormat();
                    r = d.ToSqlStr_DateTime();

                    break;

                default:
                    r = (s == "" ? "null" : s);
                    break;
            }

            return r;
        }


        /// <summary>
        /// 將 v 轉成 object 型態，
        /// 若 v= 指定的 nullValue 則傳回 null
        /// </summary>
        /// <param name="v"></param>
        /// <param name="nullValue"></param>
        /// <returns></returns>
        public static object ToObject(this int v, int nullValue)
        {
            object r = null;
            if (v != nullValue)
            {
                r = v;
            }
            return r;
        }

        /// <summary>
        /// 將 v 轉成 object 型態，
        /// 若 v= 指定的 nullValue 則傳回 null
        /// </summary>
        /// <param name="v"></param>
        /// <param name="nullValue"></param>
        /// <returns></returns>
        public static object ToObject(this string v, string nullValue)
        {
            object r = null;
            if (v != nullValue)
            {
                r = v;
            }
            return r;
        }

        public static object RandomItem(this object[] o)
        {
            Random ran = new Random(Guid.NewGuid().GetHashCode());
            int idx = ran.Next(0, o.Length);
            return o[idx];
        }


        /// <summary>
        /// 將傳入的 List 依指定的包裝數量打包成數個 Array
        /// </summary>
        /// <param name="lstOrg"></param>
        /// <param name="cntPerPack">一個 pack 的數量</param>
        /// <returns></returns>
        static public List<T>[] Pack<T>(this List<T> lstOrg, int cntPerPack)
        {
            List<List<T>> r = new List<List<T>>();
            int PackCnt = (int)Math.Ceiling((double)lstOrg.Count / cntPerPack);
            for (int i = 0; i < PackCnt; i++)
            {
                int idxFrom = i * cntPerPack;
                int cnt = cntPerPack;
                if (idxFrom + cnt > lstOrg.Count) cnt = lstOrg.Count - idxFrom;
                List<T> lst = lstOrg.GetRange(idxFrom, cnt);
                r.Add(lst);
            }
            return r.ToArray();
        }

        static public string ToJsonStr(this string sData)
        {
            string[] aryData = sData.Split(new char[] { ',' });
            StringBuilder r = new StringBuilder("[");
            foreach (string data in aryData)
            {
                r.Append(ClsUty.sqlstr(data) + ",");
            }
            r.Remove(r.Length - 1, 1);
            r.Append("]");
            return r.ToString();
        }


        public static T ToEnum<T>(this string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }


        public static T ToEnumDefault<T>(this string value, string defaultValue)
        {
            if (value == "") value = defaultValue;
            return (T)Enum.Parse(typeof(T), value, true);
        }

        public static string ToJson(this NameValueCollection nv)
        {
            var json = JsonConvert.SerializeObject(nv.AllKeys.ToDictionary(x => x, y => nv[y]));
            return json;

        }

        /// <summary>
        /// 將 string array 轉為 javascript array object 字串格式
        /// 
        /// </summary>
        /// <param name="AryValue">物件的 Value</param>
        /// <param name="name">物件的名稱，所有物件都是相同名稱</param>
        /// <returns></returns>
        public static string asNameValueJson(this string[] AryValue, string name)
        {
            string r = "[";
            foreach (var v in AryValue)
            {
                string o = "{" + name + ":" + v.ToSingleQuote() + "}";
                r += o + ",";
            }
            r = r.RemoveRight(1) + "]";
            return r;
        }

        public static string segment(this string line, char Separted, int nSeg)
        {
            if (nSeg == 0) return "";

            string[] a = line.Split(new char[] { Separted });
            if (Math.Abs(nSeg) > a.Length) return "";
            if (nSeg >= 1)
            {
                return a[nSeg - 1];
            }
            else
            {
                return a[a.Length + nSeg];
            }
        }



        /// <summary>
        /// 去除字串中的換行字元
        /// </summary>
        /// <param name="s"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string RemoveNewLine(this string s)
        {
            string r = s.Replace("\r\n", "");
            return r.Replace("\n", "");
        }


        /// <summary>
        /// 置換 kendo Template 特殊字元 # 成 \\#
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ToKDTemplate(this string s)
        {
            return s.Replace("#", @"\\#");
        }


        /// <summary>
        /// 將指定欄位的值如果是 0 轉成 null 
        /// 針對 not null 的數值欄位，在過程中 null 會被轉成 0，所以在寫入 DB 之前，必須將其還原為 null
        /// </summary>
        /// <param name="json"></param>
        /// <param name="fieldList">逗號分隔的欄位名稱</param>
        /// <returns></returns>
        public static void cvrtZeroToNull(this JObject json, string fieldList)
        {
            string[] aryField = fieldList.Split(new char[] { ',' });
            foreach (string fld in aryField)
            {
                if (json[fld].ToInt_Mars() == 0)
                {
                    json[fld] = null;
                }
            }

        }

        public static List<T> ToList<T>(this DataTable dt) where T : new()
        {
            // 定義集合    
            List<T> ts = new List<T>();
            // 獲得此模型的類型   
            Type type = typeof(T);
            string tempName = "";
            foreach (DataRow dr in dt.Rows)
            {
                T o = new T();
                // 獲得此模型的公共屬性      
                PropertyInfo[] propertys = o.GetType().GetProperties();
                foreach (PropertyInfo pi in propertys)
                {
                    tempName = pi.Name;  // 檢查DataTable是否包含此列   
                    if (dt.Columns.Contains(tempName))
                    {
                        // 判斷此屬性是否有Setter      
                        if (!pi.CanWrite) continue;
                        object value = dr[tempName];
                        string dbDataType = value.GetType().ToString();
                        string propType = pi.PropertyType.FullName;
                        if (value != DBNull.Value)
                        {
                            if (dbDataType == "System.Decimal")
                            {
                                if (propType == "System.Int64")
                                {
                                    value = Convert.ToInt64(value);

                                }
                                else if (propType == "System.Int32")
                                {
                                    value = Convert.ToInt32(value);
                                }
                            }
                            pi.SetValue(o, value, null);
                        }
                    }
                }
                ts.Add(o);
            }
            return ts;
        }

        public static List<Dictionary<string, object>> ToList(this DataTable dt)
        {
            return dt.AsEnumerable().Select(
                row => dt.Columns.Cast<DataColumn>().ToDictionary(
                    key => key.ColumnName,
                    value => row[value])).ToList();
        }



    }
}
