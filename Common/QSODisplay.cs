using System;
using System.Collections.Generic;
using System.Data;

namespace Common {
 
    public class QSODisplay :QSOBase{  // struct for perfomance. problem testing missing in LINQ.
        public double MatchPct { get; set; }
        //   public string Item { get; set; }
        //public string Value { get; set; }
        public string CellFormat { get; set; }

        //public string Call { get; set; }
        //public string Band { get; set; } // Easier to dispaly string than int
        //public string Mode { get; set; }
        public string LotW { get; set; }
        public Nullable<System.DateTime> LotwLastUpload { get; set; }
        public string DXCCPrefix { get; set; }
        public string DXCCName { get; set; }

        public bool Highlight { get; set; }
        // Utility members - optional
        //   public DateTime QSO_Begin { get; set; }
        //     public int QSOYear { get { return ((DateTime)QSO_Begin.Value).Year; } }
        //public string STX { get; set; }
        //public string SRX { get; set; }
        //public string SRX1 { get; set; }
        //public string SRX2 { get; set; }

        public double FREQ_RX_MHZ { get; set; }
        public double FREQ_RX_KHZ { get; set; }

        public string FREQ_RX_MHZ8_5 { get; set; }
        public string FREQ_RX_KHZ8_2 { get; set; }

        public string PFX { get; set; }
        //public string CQZ { get; set; }
        //public string ITUZ { get; set; }
        //public string CONTEST_ID { get; set; }

        public int BandNumeric { get; set; }
        public string QSO_YYYMMDD {
            get { 
                return (QSO_Begin).ToString("yyyy-MM-dd");
            }
        }
        public string QSOHHMM_YYYMMDD { get; set; }
        public string QSOHHMM { get; set; }

        public static QSODisplay GetDividerLine(char divider = '=') {
        //    return new QSODisplay { CellFormat="BACKGROUND:Red,FONTBOLD:YES", Band=new string(divider, 4), Call=new string(divider, 12), QSOHHMM_YYYMMDD=new string(divider, 10), CONTEST_ID=new string(divider, 12), LotW=new string(divider, 3), SRX=new string(divider, 4), STX=new string(divider, 4), Mode=new string(divider, 3), FREQ_RX_MHZ8_5=new string(divider, 10) };
            return new QSODisplay { CellFormat="BACKGROUND:Red,FONTBOLD:YES",  Call=new string(divider, 12), QSOHHMM=new string(divider, 10), CONTEST_ID=new string(divider, 12), LotW=new string(divider, 3), SRX=new string(divider, 4), STX=new string(divider, 4), Mode=new string(divider, 3), FREQ_RX_MHZ8_5=new string(divider, 10) };
        }
        public static QSODisplay ConvertDTRowToQSODisplayObject(DataRow row) {

            QSODisplay disp = new QSODisplay {
                Call=row.Field<string>("Call"),
                Band=row.Field<int>("Band"),
                Mode=row.Field<string>("Mode"),
                LotW=row.Field<string>("LotW"),
                DXCCPrefix=row.Field<string>("DXCCPrefix"),
                DXCCName=row.Field<string>("DXCCName"),
                QSO_Begin=(DateTime)row.Field<DateTime?>("QSO_Begin"),
                QSOHHMM_YYYMMDD=row.Field<string>("QSOHHMM_YYYMMDD"),
                QSOHHMM=row.Field<string>("QSOHHMM_YYYMMDD").Substring(0,5),

                STX=row.Field<string>("STX"),
                SRX=row.Field<string>("SRX"),
                SRX1=QSOInfo.GetSRX1(row.Field<string>("SRX")),
                SRX2=QSOInfo.GetSRX2(row.Field<string>("SRX")),
                PFX=row.Field<string>("PFX"),
                CQZ=row.Field<int>("CQZ"),
                ITUZ=row.Field<int>("ITUZ"),
                FREQ_RX_MHZ8_5=row.Field<string>("FREQ_RX_MHZ8_5"),
                FREQ_RX_MHZ=row.Field<double>("FREQ_RX_MHZ"),
                FREQ_RX_KHZ8_2=row.Field<string>("FREQ_RX_KHZ8_2"),
                FREQ_RX_KHZ=row.Field<double>("FREQ_RX_KHZ"),

                CONTEST_ID=row.Field<string>("contest_id")

            };
            return disp;
        }// ConvertDTRowToQSODisplayObject
        public static string GetDisplayString(QSODisplay disp, bool contestMode, bool header, int padLength) {

          //  string matchPct = Math.Round(100.0*disp.MatchPct).ToString("0");

            string fmtLine = "";
            
            //if (contestMode) {
            //    if (header) {
            //        fmtLine= $"{"_UTC_ __Date__",-14} {"_______STX",10} {"_______SRX",10} {"Mod",3} {"_RX_Freq",8} {"Call______",-10}";
            //    } else {
            //        fmtLine= $"{disp.QSOHHMM_YYYMMDD,-14} {disp.STX,10} {disp.SRX,10} {disp.Mode,3} {disp.FREQ_RX_MHZ8_5,8} {disp.Band,3} {disp.Call,-10}";
            //    }
            //} else {
                if (header) {
                    fmtLine= $"{"CONTEST_ID",-10} {"_UTC_ __Date__",-14} L {"_______STX",10} {"_______SRX",10} {"Mod",3} {"_RX_Freq",8}  {"Call______",-10}";
                } else {
                    fmtLine=$"{disp.CONTEST_ID.Left(10),-10} {disp.QSOHHMM_YYYMMDD,-14} {disp.LotW} {disp.STX,10} {disp.SRX,10} {disp.Mode,3} {disp.FREQ_RX_MHZ8_5,8} {disp.Band,3} {disp.Call,-10} ";

                }
            //}

            fmtLine= fmtLine.PadRight(padLength);
            return fmtLine;
        }
        public static QSODisplay GetDelimiterItems() {
            string s = "-";
            QSODisplay disp = new QSODisplay {
                Call=Util.RepeatStringLinq(s, 13), Mode=Util.RepeatStringLinq(s, 3), LotW="-", QSOHHMM_YYYMMDD=Util.RepeatStringLinq(s, 17),
                STX=Util.RepeatStringLinq(s, 8), SRX=Util.RepeatStringLinq(s, 8), SRX1="", SRX2="", PFX="", CQZ=0, ITUZ=0, FREQ_RX_MHZ8_5=Util.RepeatStringLinq(s, 8), FREQ_RX_MHZ=0.0, FREQ_RX_KHZ8_2="FREQ_RX_KHZ8_2", FREQ_RX_KHZ=0.0, CONTEST_ID=Util.RepeatStringLinq(s, 8)
            };
            return disp;
        }
    }// QSODisplay
    public class Distinct_QSODisplay_ComparerBandCallModeLotw:IEqualityComparer<QSODisplay> { //https://abundantcode.com/how-to-remove-duplicates-in-list-using-linq/
        public bool Equals(QSODisplay x, QSODisplay y) {
            bool BandEq = x.Band==y.Band;
            bool CallEq = x.Call==y.Call;
            bool ModeEq = x.Mode==y.Mode;
            bool LotWEq = x.LotW==y.LotW;
            return BandEq&&CallEq&&ModeEq&&LotWEq;
        }
        public int GetHashCode(QSODisplay obj) {
            return obj.Call.GetHashCode();
        }
    }
    public class Distinct_QSODisplay_ComparerCall:IEqualityComparer<QSODisplay> { //https://abundantcode.com/how-to-remove-duplicates-in-list-using-linq/
        public bool Equals(QSODisplay x, QSODisplay y) {
            bool CallEq = x.Call==y.Call;
            return CallEq;
        }
        public int GetHashCode(QSODisplay obj) {
            return obj.Call.GetHashCode();
        }
    }
}
