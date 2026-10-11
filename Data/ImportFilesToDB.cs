using ClubLogXmlParser;
using Common;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Xml.Linq;
using static System.Windows.Forms.AxHost;

namespace Data {
    public class ImportFilesToDB:DataETLBase {  // Import various files/formats into DB

        public ImportFilesToDB(string connString) : base(connString) {
        }

        public void LoadDatamasterSCP() {
            dBManager.ExecuteNonQuery(CommandType.Text, "truncate table QSOLOG.dbo.SUPERCHECK_MASTER");

            string InputFilePath = $@"{InputFileLocation}\MASTER.SCP";
            DataTable dt = Util.ConvertCSVtoDataTableHeaderDefined(InputFilePath, new List<string> { "CALL" }, ",", new List<string> { "#" });
            dBManager.BulkInsert(dt, "QSOLOG.dbo.SUPERCHECK_MASTER");

            // RUN SP FROM DATABASE. PROBLEM WITH CURSOR      dBManager.ExecuteNonQuery(System.Data.CommandType.StoredProcedure, "QSOLOG..UpdSuperCheckMasterFromCTY"); // Runs for 15 minutes

        }
        public void LoadlkpNAQP_CALLS() {
            dBManager.ExecuteNonQuery(CommandType.Text, "truncate table QSOLOG.dbo.lkpNAQP_CALLS");

            string InputFilePath = $@"{InputFileLocation}\NAQPCW-007.txt";
            DataTable dt = Util.ConvertCSVtoDataTableHeaderDefinedOQP(InputFilePath, new List<string> { "Call", "Name", "State", "UserText" }, "", new List<string> { "#","!" });
            dBManager.BulkInsert(dt, "QSOLOG.dbo.lkpNAQP_CALLS");

            // RUN SP FROM DATABASE. PROBLEM WITH CURSOR      dBManager.ExecuteNonQuery(System.Data.CommandType.StoredProcedure, "QSOLOG..UpdSuperCheckMasterFromCTY"); // Runs for 15 minutes

        }
        

        public void LoadClubLogXML() {
          //  dBManager.ExecuteNonQuery(CommandType.Text,"truncate table QSOLOG.dbo.SUPERCHECK_MASTER");

            string InputFilePath = $@"{InputFileLocation}\ClubLog\cty.xml";
            string xmlContent = File.ReadAllText(InputFilePath);
            ClubLogXmlParser.ClubLogData clubLogData= ClubLogXmlParser.ClubLogXmlParser.Parse(xmlContent);

            dBManager.ExecuteNonQuery(CommandType.Text,"truncate table QSOLOG.rbn.clEntity");
            dBManager.ExecuteNonQuery(CommandType.Text,"truncate table QSOLOG.rbn.clException");
            dBManager.ExecuteNonQuery(CommandType.Text,"truncate table QSOLOG.rbn.clPrefix");
            dBManager.ExecuteNonQuery(CommandType.Text,"truncate table QSOLOG.rbn.clInvalidOperation");
            dBManager.ExecuteNonQuery(CommandType.Text,"truncate table QSOLOG.rbn.clSatelite");
            dBManager.ExecuteNonQuery(CommandType.Text,"truncate table QSOLOG.rbn.clZoneException");
            
            dBManager.BulkInsert(ClubLogDataTableExtensions.ToDataTable(clubLogData.Entities.Items),"rbn.clEntity");
            dBManager.BulkInsert(ClubLogDataTableExtensions.ToDataTable(clubLogData.Exceptions.Items),"rbn.clException");
            dBManager.BulkInsert(ClubLogDataTableExtensions.ToDataTable(clubLogData.Prefixes.Items),"rbn.clPrefix");
            dBManager.BulkInsert(ClubLogDataTableExtensions.ToDataTable(clubLogData.InvalidOperations.Items),"rbn.clInvalidOperation");
            dBManager.BulkInsert(ClubLogDataTableExtensions.ToDataTable(clubLogData.Satellites.Items),"rbn.clSatelite");
            dBManager.BulkInsert(ClubLogDataTableExtensions.ToDataTable(clubLogData.ZoneExceptions.Items),"rbn.clZoneException");

        }
    }
}