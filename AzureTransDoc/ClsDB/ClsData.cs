using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureTransDoc.ClsDB
{
    internal class ClsData
    {
        public static ClsDBLib getDBLib(bool useTransaction, IConfiguration config)
        {
            ClsDBLib dbLib = new ClsDBLib("ConnectionStrings:DBMain", useTransaction, config);
            return dbLib;

        }
    }
}
