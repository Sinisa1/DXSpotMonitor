using Common;
using Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Linq;

namespace DataAccessLayer {
    /// <summary>
    ///                https://stackoverflow.com/questions/19117106/sqlbulkcopy-unexpected-remaining-transaction
    /// </summary>
    public class CopyAccessToMSSQL:DataETLBase {
        private string strConnectAccess { get; set; }
        private string strConnectSQL { get; set; }

        public CopyAccessToMSSQL(string strConnectAccess_, string strConnectSQL_):base(strConnectSQL_) {
            strConnectAccess=strConnectAccess_;
            strConnectSQL=strConnectSQL_;
        }

        private string GetAccessDBConnectionString(string sourceDir, string sourceDBName) {
            return strConnectAccess.Replace("{sourceDir}", sourceDir).Replace("{sourceDBName}", sourceDBName);
        }
        public RetCode CopyFromAccessWithTruncate(string sourceDir, string sourceDBName, string sourceTableName, string targetTableName) {
            RetCode ret = new RetCode();
            int rowCount = 0;
            string strConnectAccessUpdated = GetAccessDBConnectionString(sourceDir, sourceDBName);

               try {      
                dBManager.ExecuteNonQuery(CommandType.Text,$"TRUNCATE TABLE QSOLOG.dbo.{targetTableName}");
            
                string sourceSQL = $"SELECT * FROM {sourceTableName}";
                DataTable dtSource = new DataTable();
                using (OleDbConnection conAccess = new OleDbConnection(strConnectAccessUpdated)) {
                    string accessCmd = sourceSQL;
                    OleDbCommand cmd = new OleDbCommand(accessCmd, conAccess);
                    conAccess.Open();
                    OleDbDataAdapter da = new OleDbDataAdapter(cmd);
                    da.Fill(dtSource);
                }
                rowCount=dtSource.Rows.Count;
                if (rowCount>0) {
                    bool success = dBManager.  BulkInsert(dtSource,  $"QSOLOG.dbo.{targetTableName}");
                    if (success==false) {
                        ret.Code=-1;
                        ret.Msg=$" BULK copy rollback. sourceDir: {sourceDir}, sourceDBName: {sourceDBName}, sourceTableName: {sourceTableName} , targetTableName: {targetTableName}";
                    }                
                }

            } catch (Exception ex) {
                ret.Code=-1;
                ret.Msg=$"sourceDir: {sourceDir}, sourceDBName: {sourceDBName}, sourceTableName: {sourceTableName} , targetTableName: {targetTableName}, error: {ex.Message}";
            }
            ret.IntValue=rowCount;
            return ret;
        }
        public RetCode CopyFromAccessDxKeeperQSOs(string mode, string sourceDir, string sourceDBName) {
            RetCode ret = new RetCode();
            int rowCount = 0;
            string strConnectAccessUpdated = GetAccessDBConnectionString(sourceDir, sourceDBName);
            //SELECT [Band] as BandM , REPLACE(   [Band] , "M","")  as  BandNumeric ,Call,DXCCPrefix,DXCCID,Freq,Name,Comment,QTH,RST_Rcvd,RST_Sent,State,QSO_Begin,CONT,ITUZ,CQZ,CNTY,CONTEST_ID,SRX,STX,PFX,BAND_RX,FREQ_RX,APP_DXKeeper_LotW_QSLRDATE,APP_DXKeeper_QSO_NUMBER,APP_DXKeeper_USER_DEFINED_0,APP_DXKeeper_LotWConfirmation,Mode FROM QSOs
            string columns = "  CInt(REPLACE([Band], \"M\",\"\"))  as  BandNumeric,[Band] as BandM ,Call,DXCCPrefix,DXCCID,Freq,Name,Comment,QTH,RST_Rcvd,RST_Sent,State,QSO_Begin,CONT,ITUZ,CQZ,CNTY,CONTEST_ID,SRX,STX,PFX,BAND_RX,FREQ_RX,APP_DXKeeper_LotW_QSLRDATE,APP_DXKeeper_QSO_NUMBER,APP_DXKeeper_USER_DEFINED_0,APP_DXKeeper_LotWConfirmation,Mode,STATION_CALLSIGN";
            string sourceAccessSQL = "";
            try {
                System.Data.SqlClient.SqlConnection conSQL = new SqlConnection(strConnectSQL);
                conSQL.Open();

                SqlCommand cmdSQL = new SqlCommand();
                cmdSQL.Connection=conSQL;
                int maxId = 0;
                object obj = null;
                switch (mode) {
                    case "ALL":
                        cmdSQL.CommandText="TRUNCATE TABLE QSOLOG.dbo.QSOs";
                        obj=cmdSQL.ExecuteScalar();
                        break;
                    case "NEW_ONLY":
                        cmdSQL.CommandText="Select max(APP_DXKeeper_QSO_NUMBER) APP_DXKeeper_QSO_NUMBER from QSOLOG.dbo.QSOs";
                        obj=cmdSQL.ExecuteScalar();
                        bool res = int.TryParse(obj.ToString(), out maxId);
                        break;
                }

                switch (mode) {
                    case "ALL":
                        sourceAccessSQL=$"SELECT {columns} FROM QSOs";
                        break;
                    case "NEW_ONLY":
                        sourceAccessSQL=$"SELECT {columns} FROM QSOs where APP_DXKeeper_QSO_NUMBER>{maxId}";
                        break;
                }
                DataTable dtSource = new DataTable();
                using (OleDbConnection conAccess = new OleDbConnection(strConnectAccessUpdated)) {
                    string accessCmd = sourceAccessSQL;
                    OleDbCommand cmd = new OleDbCommand(accessCmd, conAccess);
                    conAccess.Open();
                    OleDbDataAdapter da = new OleDbDataAdapter(cmd);
                    da.Fill(dtSource);

                    DataColumn[] arr = new DataColumn[dtSource.Columns.Count];
                    dtSource.Columns.CopyTo(arr, 0);

                }
                rowCount=dtSource.Rows.Count;
                //https://stackoverflow.com/questions/19117106/sqlbulkcopy-unexpected-existing-transaction
                // Download 64 bit Access driver  https://www.microsoft.com/en-us/download/details.aspx?id=13255
                // 64 bit. AccessDatabaseEngine_X64.exe  Microsoft Access Database Engine 2010 Redistributable
                //Visual studio project target 64bit instead of any CPU

                if (rowCount>0) {
                    using (SqlTransaction transaction = conSQL.BeginTransaction(IsolationLevel.ReadCommitted)) {

                        using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(conSQL, SqlBulkCopyOptions.Default, transaction)) {

                            sqlBulkCopy.NotifyAfter=5000;
                            sqlBulkCopy.BatchSize=100;
                            sqlBulkCopy.BulkCopyTimeout=60;
                            sqlBulkCopy.DestinationTableName="QSOLOG.dbo.QSOs";

                            try {
                                sqlBulkCopy.WriteToServer(dtSource);
                                transaction.Commit();
                            } catch (Exception ex) {
                                transaction.Rollback();
                            }
                        }
                    }
                }//row count>0

            } catch (Exception ex) {
                ret.Code=-1;
                ret.Msg=$"Mode: {mode}, sourceDir:  {sourceDir}, sourceDBName: {sourceDBName} error: {ex.Message}";
            }
            ret.IntValue=rowCount;
            return ret;
        }
    }
}
