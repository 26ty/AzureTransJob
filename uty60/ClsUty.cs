using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uty60
{
    public class ClsUty
    {
        static public string sqlstr(string s)
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
        static public string sqldate_ora(DateTime d)
        {
            return "TO_DATE('" + d.ToString("yyyy-MM-dd") + "','yyyy-mm-dd')";
        }


        static public string sqldatetime_ora(DateTime d)
        {
            return "TO_DATE('" + d.ToString("yyyy-MM-dd HH:mm:ss") + "','yyyy-mm-dd HH24:MI:SS')";
        }

        static public string sqldate(DateTime d)
        {
            return sqldate_ora(d);
        }

        static public string sqldateTime(DateTime d)
        {
            return sqldatetime_ora(d);
        }

        public static string sqlTimeStamp(DateTime tm)
        {
            return "TO_TIMESTAMP('" + tm.ToString("yyyy-MM-dd HH:mm:ss.fff") + "','yyyy-mm-dd HH24:MI:SS.FF')";
        }

    }
}
