using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Common;
using DataAccessLayer;
namespace Data {   
   

    public class LogContestSettings { //}:DataETLBase {

        SettingsLog settings_ = null;
        public double ContestExpiryMonthsOverride = 1.0;   // Stays here because it is in the DB table
        public string ContestPeriod { get; set; }

        public bool MatchBand = false;
        public bool MatchMode = false;


        public string SerialTX1Value { get; set; }
        public int SerialTX1AutoIncrement { get; set; }// 0 - no increment

        public string SerialTX2Value { get; set; }
        public bool SerialTx2Enabled { get; set; }
        public int SerialTX2AutoIncrement { get; set; } // 0 - no increment
        // DERIVED

        public int MatchBandInt { get { return MatchBand ? 1 : 0; } }
        public int MatchModeInt { get { return MatchMode ? 1 : 0; } }
        public int SerialTx2EnabledInt { get { return SerialTx2Enabled ? 1 : 0; } }


        public void ResetToNewContest(string contestCode, int contestYear) {
            ContestPeriod="1";

            MatchBand=false;
            MatchMode=false;


            SerialTX1Value="";
            SerialTX1AutoIncrement=0;

            SerialTX2Value="";
            SerialTx2Enabled=false;
            SerialTX2AutoIncrement=0;

        }

        public void ConvertDTRowToLogContestSettings(DataRow row) {
            try {
                ResetToNewContest("", 0);
                ContestPeriod =row.Field<string>("ContestPeriod").ToString();

                MatchBand=row.Field<int>("MatchBand")==1;
                MatchMode=row.Field<int>("MatchMode")==1;


                SerialTX1Value=row.Field<string>("SerialTX1Value").ToString();
                SerialTX1AutoIncrement=row.Field<int>("SerialTX1AutoIncrement");

                SerialTX2Value=row.Field<string>("SerialTX2Value").ToString();
                SerialTx2Enabled=row.Field<int>("SerialTx2Enabled")==1;
                SerialTX2AutoIncrement=row.Field<int>("SerialTX2AutoIncrement");


                ContestExpiryMonthsOverride=double.Parse(row.Field<decimal>("ContestExpiryMonthsOverride").ToString());

            } catch (Exception ex) {

            }
        }


        public void IncrementSerialNumbers(bool ContestMode) {
            int valInt = 0;
            if (!ContestMode) {
                return;
            }
            if (SerialTX1AutoIncrement>0) {

                if (int.TryParse(SerialTX1Value, out valInt)) {
                    SerialTX1Value=(++valInt).ToString();
                }
            }
            if (SerialTX2AutoIncrement>0) {
                if (int.TryParse(SerialTX2Value, out valInt)) {
                    SerialTX2Value=(++valInt).ToString();
                }
            }
        }

    }
}
