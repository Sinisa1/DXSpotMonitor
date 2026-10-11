using Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
namespace Data {

    public class SettingsLog:AppSettingsBase {
        public bool UseOptimizedSearch = true;
        public bool ContestMode = false;
        public int ContestYear = DateTime.Now.Year;
        public bool SortByMatchScore { get; set; } = false;
        public ContestInfo ContInfo = null;
        public List<ContestInfo> ContestInfoList { get; set; }

        public BandInfo BandInfo = null;
        public int StatusWriteLevel = 0; //0 nothing, 1 basic(contest), 2 detail
        public int GridDisplayLevel = 2; //0 nothing, 1 basic(contest), 2 detail


        //User settings and Contest settings
        public bool UseN1MMContestCodeForADIF { get; set; }

        public int contestCallLenMin = 1;
        public int wkdDXCCCCallLenMin = 2;

        // ════════════════════════════════════════════════════════════════
        // Contest Match Search Settings (tbExactCallContestMatch)
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Minimum characters before tbExactCallContestMatch search starts.
        /// Default: 2 (prevents search on single character)
        /// </summary>
        public int ContestMatchMinSearchLength { get; set; } = 2;

        /// <summary>
        /// Minimum characters before Contains matching activates (in addition to StartsWith).
        /// Default: 3 (Contains on 2 characters matches too many irrelevant calls)
        /// </summary>
        public int ContestMatchMinContainsLength { get; set; } = 3;

        /// <summary>
        /// Maximum StartsWith results to display in tbExactCallContestMatch.
        /// Default: 10
        /// </summary>
        public int ContestMatchMaxStartsWith { get; set; } = 10;

        /// <summary>
        /// Maximum Contains results to display in tbExactCallContestMatch.
        /// Default: 5
        /// </summary>
        public int ContestMatchMaxContains { get; set; } = 5;
        // ════════════════════════════════════════════════════════════════

        public string AccessConnectionString { get; set; }

        //public DateTimeInfo dtUTC = new DateTimeInfo();
        //public DateTimeInfo dtLocal = new DateTimeInfo();
        public double RcvFreqkHz { get; set; }
        public double RcvFreqMHz { get { return RcvFreqkHz/1000.0; } }
        public string Mode { get; set; }
        public string Band { get; set; }

        public string BandM { get { return Band+"M"; } }
        public bool TimerCopyNewQSOsEnabled { get; set; }

        public string FormDisplayScreenIndex { get; set; } //Screen:  0 or 1 . See if Primary/Secondary can be used
        public string FormDisplayScreenAlignmentX { get; set; } //Screen: LEFT,CENTER,RIGHT, nnn absolute position of top left corner of the form
        public string FormDisplayScreenAlignmentY { get; set; }  //Screen: LEFT,CENTER,RIGHT, nnn absolute position of top left corner of the form

        public bool DebugIgnoreContestDateDifference { get; set; }
        public SettingsLog() : base() {

        }
        public override void InitializeAAAA() {

            ContInfo=new ContestInfo();
            ContestInfoList=ContestInfo.PopulateContestInfoList();

            BandInfo=new BandInfo();

            FormDisplayScreenIndex=ConfigurationManager.AppSettings.Get("FormDisplayScreenIndex");
            FormDisplayScreenAlignmentX=ConfigurationManager.AppSettings.Get("FormDisplayScreenAlignmentX");
            FormDisplayScreenAlignmentY=ConfigurationManager.AppSettings.Get("FormDisplayScreenAlignmentY");

            DebugIgnoreContestDateDifference=Util.ReadAppSettingsBool("DebugIgnoreContestDateDifference");

        }
        public List<string> GetContestCabrilloCodeList() {
            return ContestInfoList.Where(x => x.CabrilloCode!="IGNORE").Select(x => x.CabrilloCode).ToList();
        }

        public void SetActiveContestInfo(ContestInfo contInfo) {
            ContInfo=contInfo;
        }
        /// <summary>
        /// Get contest expiry hours with debug override support.
        /// Returns 336 (14 days) if debug mode is on, otherwise uses contest's configured expiry.
        /// </summary>
        public int GetEffectiveContestExpiryHours() {
            return DebugIgnoreContestDateDifference
                ? 336  // 14 days for debugging
                : ContInfo.GetContestexpiryHours();
        }

        /// <summary>
        /// Check if a QSO belongs to the current contest period.
        /// </summary>
        public bool IsCurrentContestPeriod(DateTime qsoDateTime) {
            return Util.DateDiffHours(qsoDateTime,DateTime.Now) < GetEffectiveContestExpiryHours();
        }

        /// <summary>
        /// Check if a QSO belongs to a previous contest period (same contest, older years).
        /// </summary>
        public bool IsPreviousContestPeriod(DateTime qsoDateTime) {
            return Util.DateDiffHours(qsoDateTime,DateTime.Now) >= GetEffectiveContestExpiryHours();
        }
        public RetCode SetActiveContestInfo(string cabrilloCode) {
            RetCode ret = new RetCode();
            ContestInfo contInfo_ = ContestInfoList.Where(x => x.CabrilloCode.ToString()==cabrilloCode.ToString()).FirstOrDefault();
            SetActiveContestInfo(contInfo_);
            return ret;
        }

        public RetCode SetActiveContestInfo(ContestEnum contestCode) {
            RetCode ret = new RetCode();
            ContestInfo contInfo = ContestInfoList.Where(x => x.Code==contestCode).FirstOrDefault();
            SetActiveContestInfo(contInfo);
            return ret;
        }
        public void IncrementSerialNumbers() {
            IncrementSerialNumbers(ContestMode); // ( LogContestHeader.ContestMode);
        }
        public void IncrementSerialNumbers(bool ContestMode) {
            ContInfo.IncrementSerialNumbers(ContestMode);
        }


        public string GetDefaultRST() {
            switch(Mode) {
            case "CW":
                return "599";
            case "USB":
            case "LSB":
            case "SSB":
            case "FM":
            case "AM":
                return "59";
            }
            return "ERR";
        }
        public string FormatSerialNumber(string inputVal,string format1,string format2,string formatCombo) {
            if(format1.Length>0) { //if the format is not empty, this is numerical SN
                if(Util.IsNumeric(inputVal)) {
                    inputVal=int.Parse(inputVal).ToString(format1);
                }
            }

            string txSerial2 = ContInfo.SerialTX2Value.Trim();
            if(format2.Length>0) { //if the format is not empty, this is numerical SN
                if(Util.IsNumeric(txSerial2)) {
                    txSerial2=int.Parse(txSerial2).ToString(format2);
                }
            }
            string finalVal = string.Format(formatCombo,inputVal,txSerial2);
            return finalVal;
        }
        public RetCode LoadLogContestSettingsHeader(DataTable dt) {
            RetCode ret = new RetCode();
            if(dt.Rows.Count>0) {
                DataRow row = dt.Rows[0];
                try {
                    ContestMode=row.Field<int>("ContestMode")==1;
                    ContestYear=row.Field<int>("ActiveContestYear");
                    string cabrilloCode = row.Field<string>("ActiveContestCabrilloCode").ToString();
                    SetActiveContestInfo(cabrilloCode);
                } catch(Exception ex) {

                }
            }

            return ret;
        }
    }
}