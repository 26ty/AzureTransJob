using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Transactions;

namespace uty60
{
    public class ClsDBLib : IDisposable
    {
        const string cr = "\n";
        TransactionScope? TranScope = null;
        public OracleConnection Connection;
        private readonly string connID="";
        public bool useTransaction { get; set; }

        public ClsDBLib(OracleConnection pConn)
        {
            this.Connection = pConn;
            if (this.Connection.State == ConnectionState.Closed) this.Connection.Open();

        }

        public ClsDBLib(string connID, bool useTransaction, IConfiguration config)
        {
            if (useTransaction)
            {
                TranScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            }
            this.connID = connID;
            string dbConnStr = config.GetValue<string>(connID);

            Connection = new OracleConnection(dbConnStr);


            Connection.Open();

            OracleGlobalization info = this.Connection.GetSessionInfo();
            info.TimeZone = "Asia/Taipei";
            var tz = info.TimeZone;

        }

        public async Task<DataTable> getDataTable(string sSql)
        {

            OracleCommand cmd = getCommand(sSql);
            DbDataReader reader = await cmd.ExecuteReaderAsync(CommandBehavior.Default);
            DataTable dt = new DataTable();
            dt.Load(reader);
            return dt;

        }

        public async Task<object> TblLookup(string TblName, string ResultField, string keyList, object[] aryKeyValue)
        {
            string sSql = GetTblLookupSQL(TblName, ResultField, keyList, aryKeyValue);
            DataTable tbl = await getDataTable(sSql);

            if (tbl.Rows.Count > 0)
            {
                return tbl.Rows[0][0];
            }
            else
            {
                return null;
            }

        }


        public async Task<bool> DataExists(string TblName, string keyList, object[] aryKeyValue)
        {

            string[] aryKeyFieldName = keyList.Split(',');
            string sWhere = "WHERE ";
            for (int i = 0; i < aryKeyFieldName.Length; i++)
            {
                string fld = aryKeyFieldName[i];
                sWhere += fld + "=" + SQLValue(aryKeyValue[i]) + cr + " AND ";

            }
            sWhere = sWhere.Substring(0, sWhere.Length - 5);

            string sSql;
            sSql = "  SELECT count(*)" + cr
                + "  FROM " + TblName + cr
                + sWhere;
            DataTable dt = await getDataTable(sSql);
            int cnt = int.Parse(dt.Rows[0][0].ToString());
            return cnt > 0;

        }


        public async Task<int> ExecNonQuery(string sSql)
        {
            OracleCommand cmd = getCommand(sSql);
            int r = await cmd.ExecuteNonQueryAsync();
            return r;
        }

        public OracleCommand getCommand(string sSql)
        {

            //OracleCommand cmd = new OracleCommand(sSql, conn);
            OracleCommand cmd = Connection.CreateCommand();
            cmd.Connection = Connection;
            cmd.CommandText = sSql;
            return cmd;

        }


        public string GetTblLookupSQL(string TblName, string ResultField, string keyList, object[] aryKeyValue)
        {
            string[] aryKeyFieldName = keyList.Split(new char[] { ',' });
            string sWhere = "WHERE ";
            for (int i = 0; i < aryKeyFieldName.Length; i++)
            {
                string fld = aryKeyFieldName[i];
                object v = aryKeyValue[i];
                string op = v == null ? " IS " : "=";

                sWhere += fld + op + SQLValue(aryKeyValue[i]) + cr + " AND ";

            }
            sWhere = sWhere.Substring(0, sWhere.Length - 5);

            string sSql;
            sSql = "  SELECT " + ResultField + cr
                + "  FROM " + TblName + cr
                + sWhere;
            return sSql;
        }


        public static string SQLValue(object v)
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


        public void Dispose()
        {
            Close();
        }

        private void Close()
        {
            Connection.Close();
            TranScope = null;
        }

        public void Commit()
        {
            if (useTransaction)
            {
                if (TranScope != null)
                {
                    TranScope.Complete();                    
                }
            }

        }

        public async Task<DbDataReader> getDataReader(string sql)
        {
            OracleCommand cmd = getCommand(sql);
            DbDataReader r = await cmd.ExecuteReaderAsync(CommandBehavior.Default);
            //OracleDataReader r1 = await cmd.ExecuteReaderAsync(CommandBehavior.Default);
            return r;
        }
    }
}
