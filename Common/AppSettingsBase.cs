using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
namespace Common {
    public enum Hosts {
        PPS,
        P510,
        R500A,
        HP
    }
    public abstract class AppSettingsBase {
        private string _host = "";
        public Hosts Host { get { return Util.ParseEnum<Hosts>(_host); } }
        public bool AppEnabled = true;
        public string SQLServer = ""; //FROM HP use HP\MSSQL2019 from P510 use HP
         string SQLServerConnectionStringTemplate = "";
        public string SQLServerConnectionString = "";
        //{
        //    get {
        //        if (SQLServerConnectionStringTemplate==null||SQLServer==null) {
        //            return "";
        //        }
        //        string connTemplate = "";

        //        try {
        //            connTemplate=SQLServerConnectionStringTemplate.Replace("{SQLServer}", SQLServer);
        //        } catch (Exception ex) { }
        //        return connTemplate;
        //    }

        //}      
        public AppSettingsBase() {
            _host=Environment.MachineName;          







            AppSettings.ROOT_DIR=ConfigurationManager.AppSettings.Get($"ROOT_DIR_{Host}");
        //   SQLServerConnectionStringTemplate=ConfigurationManager.ConnectionStrings["SQLServerConnectionStringTemplate"].ConnectionString;
       //     ConnectionStringSettings connSettings = ConfigurationManager.ConnectionStrings.;// ["SQLServerConnectionStringTemplate"];
            //           SQLServer=ConfigurationManager.AppSettings.Get($"SQLServer_{Host}");

            InitializeAAAA();
        }

        public virtual void InitializeAAAA() {
           //do not put complex code in the constructor. The form gui fails to render
  //          SetSQLServer(); // See if this is the best place to set this
        }
       
    
        
        
        public void SetSQLServer() {
            SQLServerConnectionStringTemplate=ConfigurationManager.ConnectionStrings["SQLServerConnectionStringTemplate"].ConnectionString;
            SQLServer=ConfigurationManager.AppSettings.Get($"SQLServer_{Host}");
            string connTemplate = "";

            try {
                connTemplate=SQLServerConnectionStringTemplate.Replace("{SQLServer}", SQLServer);
            } catch (Exception ex) { }
            SQLServerConnectionString=connTemplate;
        }
    
    }
}
