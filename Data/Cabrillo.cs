using Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Data {
    public enum CabrilloDataType {
        String, Date, Integer, QSOMode
    }
    public enum CabrilloPadding {
        Left, Right, Center, None
    }

    public enum CbrColEnum {
        Call,
        Mode,
        FREQ_RX_MHZ,
        QSO_Begin,
        MODE,
        STATION_CALLSIGN,
        RST_Sent,
        RST_Rcvd,
        SRX,
        STX
        
    }
    public enum CbrCodeEnum {
        freq,
        mo, //mode
        date,
        time,
        STATION_CALLSIGN,
        TX_rst,
        STX_STRING,
        call,
        RX_rst,
        SRX_STRING

    }

    public class CF_FREQ_RX:CabrilloField {
        public CF_FREQ_RX(double multiplier_=1000.0) {
            SourceCol=CbrColEnum.FREQ_RX_MHZ;
            multiplier=multiplier_;
            Code=CbrCodeEnum.freq;
            DataType=CabrilloDataType.Integer;

            StringFmt=new StringFMT { Fmt="#####", FmtLength=5, FMTPadding=CabrilloPadding.Left };

        }
    }
    public class CF_CALL:CabrilloField {
        public CF_CALL() {
            StringFmt.FMTPadding=CabrilloPadding.Right; //Pod on the righ side, left aligned
        }

        public override void SetValues() {

            switch (ContestCode) {
                case ContestEnum.NAQP_CW:
                    StringFmt.FmtLength=15;
                    break;
                default:
                    StringFmt.FmtLength=13;
                    break;
            }
            SetFmtValue();
        }
    }
    public class CF_RST:CabrilloField {
        public CF_RST() {
        }
        public override void SetSourceField() {
            switch (ContestCode) {
                    default: //Keep the original field
                    break;
            }
        }

        public override void SetValues() {

            DataType=CabrilloDataType.Integer;
            StringFmt=new StringFMT { Fmt="###", FmtLength=3, FMTPadding=CabrilloPadding.Left };
            switch (ContestCode) {
                 case ContestEnum.NAQP_CW:
                case ContestEnum.CWOPS_OPEN:
                case ContestEnum.RAEM:
                    Export=false;
                    break;
                default:
                    break;
            }
            SetFmtValue();
        }

    }
    public class CF_SERIAL:CabrilloField {
        public CF_SERIAL() {
            DataType=CabrilloDataType.String;
            StringFmt=new StringFMT { FMTPadding=CabrilloPadding.Left, Fmt="######", FmtLength=6 };
            }

        public override void SetValues() {
            string[] items = new string[0];
            string STX1 = "";
            string STX2 = "";
            string SRX1 = "";
            string SRX2 = "";
            FmtValue=ValueStr;      //FmtValue;
            items=FmtValue.Split(new char[] { ' ' });

            switch (ContestCode) {
                case ContestEnum.CWOPS_OPEN:

                    break;
                default:
                    break;
            }


            switch(Code) {
            case CbrCodeEnum.STX_STRING:
                switch(ContestCode) {
                case ContestEnum.RSGB_IOTA:

                    STX1=string.Format("{0:0000}",int.Parse(FmtValue));
                    STX2="------";

                    FmtValue=$"{STX1} {STX2}";
                    break;
                case ContestEnum.NAQP_CW:
                    STX1=items[0].PadRight(10);
                    STX2=items[1].PadRight(3); ;
                    FmtValue=$"{STX1} {STX2}";
                    break;
                case ContestEnum.RAEM:
                    STX1=items[0].PadLeft(4);
                    STX2=items[1].PadRight(8); ;
                    FmtValue=$"{STX1} {STX2}";
                    break;
                case ContestEnum.CWOPS_OPEN:
                    STX1=items[0].PadRight(6);
                    STX2=items[1].PadRight(13); ;
                    FmtValue=$"{STX1} {STX2}";
                    break;
                case ContestEnum.TESLA_HF:
                    /* http://www.radiosport.yu1srs.org.rs/HFTeslaMemorial/Razno/TeslaMemorialHF.txt    # DXLog.net contest definition file 
                     CABRILLO_CONTEST_NAME=TESLA-MEMORIAL-HF-CW
                    CABRILLO_LINE=FREQ{F=R,6, };MODE{F=L,3, };DATE;TIME;MYCALL{F=L,13, };SENT{F=L,3, };NR{F=R,4,0};EXCHANGE;CALL{F=L,13, };RCVD1{F=L,4, };RCVD2{F=R,4,0};RECINFO


                     */
                    STX1=items[0].PadRight(4);
                    STX2=items[1].PadRight(4); ;
                    FmtValue=$"{STX1}{STX2}";
                    break;
                default:
                    SetFmtValue();
                    break;
                }

                break;
            case CbrCodeEnum.SRX_STRING:
                switch(ContestCode) {
                case ContestEnum.IARU_HF:
                    SRX1=FmtValue.PadRight(6);
                    break;
                case ContestEnum.RSGB_IOTA:
                    FmtValue=GetSRX_RSGB_IOTA(FmtValue);
                    break;
                case ContestEnum.NAQP_CW:
                    items=FmtValue.Split(new char[] { ' ' });
                    SRX1=items[0].PadRight(10);

                    SRX2=items.Length>1 ? items[1].PadRight(3) : "";
                    FmtValue=$"{SRX1} {SRX2}".Trim();
                    break;
                case ContestEnum.RAEM:
                    SRX1=items[0].PadLeft(4);
                    SRX2=items[1].PadRight(8); ;
                    FmtValue=$"{SRX1} {SRX2}";
                    break;
                case ContestEnum.TRC_DX:
                    items=FmtValue.Split(new char[] { ' ' });
                    SRX1=items[0].PadLeft(10);

                    SRX2=items.Length >1 ? items[1] : "";
                    FmtValue=$"{SRX1}{SRX2}".Trim();
                    break;
                case ContestEnum.CWOPS_OPEN:
                    SRX1=items[0].PadRight(6);
                    SRX2=items[1].PadRight(13); ;
                    FmtValue=$"{SRX1} {SRX2}";
                    break;
                case ContestEnum.TESLA_HF:
                    /* http://www.radiosport.yu1srs.org.rs/HFTeslaMemorial/Razno/TeslaMemorialHF.txt    # DXLog.net contest definition file 
                     CABRILLO_CONTEST_NAME=TESLA-MEMORIAL-HF-CW
                    CABRILLO_LINE=FREQ{F=R,6, };MODE{F=L,3, };DATE;TIME;MYCALL{F=L,13, };SENT{F=L,3, };NR{F=R,4,0};EXCHANGE;CALL{F=L,13, };RCVD1{F=L,4, };RCVD2{F=R,4,0};RECINFO


                     */
                    SRX1=items[0].PadRight(4);
                    SRX2=items[1].PadRight(4); ;
                    FmtValue=$"{SRX1}{SRX2}";
                    break;
                default:
                    SetFmtValue();
                    break;
                }

                break;

            default:
                SetFmtValue();
                break;
            }

        }
        public static string GetSRX_RSGB_IOTA(string fmtValue) {
            string[] items = fmtValue.Split(new char[] { ' ' });
            string SRX1 = string.Format("{0:0000}", int.Parse(items[0]));
            string SRX2 = "";
            if (items.Length==2) {
                SRX2=$"{Util.GetLetters(items[1])}-{string.Format("{0:000}", Util.GetNumbersInt(items[1]))}";
            } else {
                SRX2="------";
            }
            fmtValue=$"{SRX1} {SRX2}";
            return fmtValue;
        }

    }
    public class StringFMT {
        public string Fmt { get; set; }                //="#####";
        public int FmtLength { get; set; }   //=5;
        public string Header { get; set; }
        public CabrilloPadding FMTPadding { get; set; }

    }

    public enum CallItemType {
        BASE_CALL, // Actual call
        IGNORE, // Irrelevant for matching
        US_STATE, // Province
        CANADA_PROV, // Province
        PORTABLE_REGION_NUMBER, // 1,2...0  CALL/4
        DXCC_CODE, // DXCC Modifier /F ( France)
        PFX_CODE, // Prefix modifier VE3/ IT9/ TI5
        UNRECOGNISED_TOKEN, // Cannot be recognised, not on any list
        UNKNOWN // Not parsed yet
    }

    public class CallItem {
        public int Seq { get; set; }  // Sequence of the item in compound call sign
        public string Item { get; set; }  // Item Value
        public CallItemType CallItemType { get; set; }// Type
        public string Comment { get; set; }
    }
    public class CallSignBasic {
        public string Call { get; set; }
        public string DXCC { get; set; }
        public string StateProvince { get; set; }
        public string Continent { get; set; }

    }
    //   public class CallSign:OQPCabrilloFieldString {
    public class CallSign:CabrilloField {
        //      public string CallSignFull { get; set; } // Slash/extension /P, /REGION  Original Call sign from the log
        public string Entity { get; set; }
        public string Continent { get; set; }
        public string StateProvince { get; set; }

        public string CallSignBase { get; set; }  // Derived, lookup from CallItems
        public string CallSignBaseForMatch { get; set; }  // Derived,  base call with valid prefixes
        public List<CallItem> CallItems = new List<CallItem>();
        public List<string> Parse(string rawStringVal) { // Ignore
            ValueStr=rawStringVal;
            return new List<string>(); // No conversion
        }
        public void GetCallSignBaseForMatch() {
            string call = ValueStr; // Start with full call


            foreach (CallItem item in CallItems) {
                switch (item.CallItemType) {
                    case CallItemType.BASE_CALL:
                    case CallItemType.PFX_CODE:
                    case CallItemType.PORTABLE_REGION_NUMBER:

                        break;
                    default:
                        call=call.Replace(item.Item, "");
                        break;
                }
            }// for each item

            call=call.TrimStart('/');
            call=call.TrimEnd('/');
            CallSignBaseForMatch=call;

            if (CallItems.Count>1) {

            }
        }

    }
    public  class CabrilloField {  //Consolidate CabrilloField and OQPCabrilloField
        public int Seq { get; set; }
        public ContestEnum ContestCode = ContestEnum.IGNORE;
        public bool Export = true;// Whether to export or not - depends on contest.
        public CbrCodeEnum Code { get; set; }
        public CbrColEnum SourceCol { get; set; }
        public DataRow DataRow { get; set; }
     // public abstract List<string> Parse(string rawStringVal);

        public string ValueStr {
            get {
                switch (SourceCol) {
                     default:
                        return DataRow[SourceCol.ToString()].ToString();
                }
            }
            set { }
        }
        public CabrilloDataType DataType { get; set; }  // String, Date, Integer
                                                        // public int FmtLength { get; set; }
        public StringFMT StringFmt = new StringFMT();
        public CabrilloField() {
        }
        public virtual void SetSourceField() {

        }
        public virtual void SetValues() {
            SetFmtValue();
        }


        public void SetFmtValue() {

            string fmtVal = "";

            switch (DataType) {
                case CabrilloDataType.String:
                    fmtVal=FmtString(ValueStr);
                    break;
                case CabrilloDataType.Integer:
                    fmtVal=FmtInteger(ValueStr.ToString());
                    break;
                case CabrilloDataType.Date:
                    fmtVal=FmtDate(ValueStr);
                    break;
                case CabrilloDataType.QSOMode:
                    fmtVal=QSOModeConvert(ValueStr).ToString();
                    break;

            }
            FmtValue=fmtVal;
        }


        //  public string Fmt { get; set; }
        public double multiplier = 1; // Multiply value by the multiplier for frequency it is 1000
        public string FmtValue { get; set; }

        private QSOMode QSOModeConvert(string val) {
            QSOMode outMode = QSOMode.UNK;
            QSOMode inMode = Util.ParseEnum<QSOMode>(val, true);
            switch (inMode) {
                case QSOMode.SSB:
                case QSOMode.LSB:
                case QSOMode.USB:
                    outMode=QSOMode.PH;
                    break;
                default: // CW,FM
                    outMode=inMode;
                    break;
            }
            return outMode;
        }
        private string FmtInteger(string val) {
            double dblVal = Double.Parse(val); // *   1000.0;
            dblVal*=multiplier;
            string intVal = Math.Round(dblVal, 0).ToString();
            return FmtString(intVal);
        }
        private string FmtString(string val) {

            string fmtVal = "";
            switch (StringFmt.FMTPadding) {
                case CabrilloPadding.Left:
                    fmtVal=val.PadLeft(GetPadLen(val), ' ');
                    break;
                case CabrilloPadding.Right:
                    fmtVal=val.PadRight(GetPadLen(val), ' ');
                    break;
            }

            return fmtVal;
        }
        private string FmtDate(string val) {
            DateTime valDate = DateTime.Parse(val);
            string fmtVal = valDate.ToString(StringFmt.Fmt);

            return fmtVal;
        }
        private int GetPadLen(string val) {
            return Math.Max(StringFmt.FmtLength, Math.Abs(StringFmt.FmtLength-val.Trim().Length));
        }

    }  // Output value

    public class Cabrillo:DataETLBase {
        string CabrilloVersion = "3.0";
        protected string CreatedBy = $"{AppSettings.CallSign} CabrilloCreator";
        protected string CreatedByVersion = "1.0";
        SettingsLog settings = null;
        public Cabrillo(SettingsLog settings_) : base(settings_.SQLServerConnectionString) {
            settings=settings_;
          }


        public RetCode CreateCabrilloLog( ) {
            //lowercase
            //https://cqwpx.com/api/get/<request>/<mode>/callsign/<callsign>/   raw scores for the latest contest CW or SSB
            //https://cqwpx.com/api/get/raw/ph/callsign/va3t nm
            // CABRILLO FORMATS https://members.storm.ca/~ferch/qsoform.txt

            RetCode ret = new RetCode();
          DataTable dt = GetQSODataContestForExport(settings.ContInfo.CabrilloCode, settings.ContestYear);
            List<string> logLines = new List<string>();
             List<string> qsoLines = GetQSOLines(dt,                  settings.ContInfo.Code);

            // Analyse qso data
            IEnumerable<object> modeList = dt.AsEnumerable().Select(row => row["Mode"]).Distinct().ToList();

            string CabrilloLogMode = "MIXED";
            if (modeList.Count()==1) {
                CabrilloLogMode=modeList.First().ToString();    // Single mode in the log CW/SSB
                //  KEEP this. May be needed to convert SSB to PH???  CabrilloField mode = new CabrilloField { DataType = CabrilloDataType.QSOMode, Value = CabrilloLogMode };
            }

            IEnumerable<object> bandList = dt.AsEnumerable().Select(row => new BandInfo().GetBandCodeM(double.Parse(row["FREQ_RX_MHZ"].ToString()))).Distinct().ToList();

            string CabrilloLogBand = "ALL";
            if (bandList.Count()==1) {
                CabrilloLogBand=bandList.First().ToString();    // Single mode in the log CW/SSB
            }

            string location = "DX";
            switch (settings.ContInfo.Code) {
                case ContestEnum.ARRL_10:
                case ContestEnum.ARRL_160:
                case ContestEnum.ARRL_DX_CW:
                case ContestEnum.ARRL_DX_SSB:
                    location="GH"; // Golden Horseshoe. See CCO site. (Regions of Niagara, Hamilton-Wentworth, Halton, York and Durham, as well as the Cities/Towns of Caledon, Brampton, Mississauga and Toronto)
                    break;

                case ContestEnum.CQ_WPX_CW:
                case ContestEnum.CQ_WPX_SSB:
                case ContestEnum.CANADA_WINTER:
                case ContestEnum.CANADA_DAY:
                    location="ON";
                    break;
                case ContestEnum.CQ_WW_CW:
                case ContestEnum.CQ_WW_SSB:
                    location="GTA";
                    break;
            }
            // SORTED FOR CQ-WW
            logLines.Add($"START-OF-LOG: {CabrilloVersion}");
            logLines.Add($"CALLSIGN: {AppSettings.CallSign}");
            logLines.Add($"CONTEST: {settings.ContInfo.CabrilloCode}");
            logLines.Add($"CREATED-BY: {CreatedBy}");
            logLines.Add($"EMAIL: SINISA.RUZIN@GMAIL.COM");
            logLines.Add($"GRID-LOCATOR: FN03EM");   
            logLines.Add($"LOCATION: {location}");  //

            logLines.Add($"CATEGORY-OPERATOR: SINGLE-OP");
            logLines.Add($"CATEGORY-ASSISTED: ASSISTED");
            logLines.Add($"CATEGORY-BAND: {CabrilloLogBand}");
            logLines.Add($"CATEGORY-MODE: {CabrilloLogMode}");

            string power = "";

            switch (settings.ContInfo.Code) {
                case ContestEnum.NAQP_CW:
                    power="LOW";
                    break;
                default:
                    power="HIGH";
                    break;
            }

            logLines.Add($"CATEGORY-POWER: {power}");
            logLines.Add($"CATEGORY-STATION: FIXED");

            string catTransmitter = "";
            switch (settings.ContInfo.Code) {
                case ContestEnum.CQ_WPX_SSB:
                case ContestEnum.CQ_WPX_CW:
                    catTransmitter="ONE";
                    break;
                default:
                    catTransmitter = "ONE"; 
                    break;
            }
            logLines.Add($"CATEGORY-TRANSMITTER: {catTransmitter}");
        //    logLines.Add($"CATEGORY-OVERLAY: OVER-50");
            logLines.Add($"CLUB: CONTEST CLUB ONTARIO");

            logLines.Add($"NAME: Sinisa Ruzin");
            logLines.Add($"ADDRESS: 2642 Constable Rd.");
            logLines.Add($"ADDRESS-CITY: Mississauga");
            logLines.Add($"ADDRESS-STATE-PROVINCE: ON");
            logLines.Add($"ADDRESS-POSTALCODE: L5J1W2");
            logLines.Add($"ADDRESS-COUNTRY: Canada");
            logLines.Add($"OPERATORS: {AppSettings.CallSign}");
            logLines.Add($"SOAPBOX: QSO Count={qsoLines.Count}");

            logLines.AddRange(qsoLines);
            logLines.Add($"END-OF-LOG:{Environment.NewLine} ");

            DateTimeInfo dtInfo = new DateTimeInfo(DateTime.Now);
            string LogFileName = $"{AppSettings.CallSign}_{settings.ContInfo.CabrilloCode}_{dtInfo.GetDateTimeYYYYMMDD_HHMMSS()}.log";

            string outDir = $@"{AppSettings.ROOT_DIR}\{AppSettings.CallSign}\OUTPUT\Cabrillo\{settings.ContestYear}\{settings.ContInfo.CabrilloCode}";
            if (!Directory.Exists(outDir)) {
                Directory.CreateDirectory(outDir);
            }

            string logFilePath = Path.Combine(outDir, LogFileName);
            Util.FileWriteLines(logFilePath, logLines);

            return ret;
        }

        protected override List<string> GetQSOLines(DataTable dt, ContestEnum contestCode) {
            List<List<CabrilloField>> cbrFieldListForRow = new List<List<CabrilloField>>();
            for (int nRow = 0; nRow<dt.Rows.Count; nRow++) {
                List<CabrilloField> cabFields = GetCabrilloFieldsForRow(dt.Rows[nRow], contestCode);
                cbrFieldListForRow.Add(cabFields);
            }

            List<string> qsoLines = new List<string>();
            foreach (List<CabrilloField> cbrFields in cbrFieldListForRow) {

                string line = "QSO:";
                foreach (CabrilloField field in cbrFields) {

                    string fmtValue = field.FmtValue;
                    string[] items = new string[1];

                    if (field.Export) {
                        line+=" "+fmtValue;
                    }
                }
                qsoLines.Add(line);
            }
            return qsoLines;
        }

        private static List<CabrilloField> GetCabrilloFieldsForRow(DataRow row, ContestEnum contestCode) {

            int seq = 0;

            List<CabrilloField> cabFields = new List<CabrilloField>();
            switch (contestCode) {
                case ContestEnum.ARRL_VHF_JUN: // Change the frequency to just 50 as band designator. Remove the signal reports from each of lines as well.  This can be done using your comyter's text editor program.
                                        //    For example:
                        //QSO: 50098 CW 2024-06-09 1525 VA3 TNM        599   FN03 KU8E          599   EM72
                        //should be:
                        //QSO: 50 CW 2024-06-09 1525 VA3 TNM FN03  KU8E EM72
                        //73, Paul Bourque, N1SFEContest Program Manager
                    cabFields.Add(new CF_FREQ_RX(multiplier_:1.0) { Seq=seq++ });  // Display 50 instead of 50098
                    break;
                default:
                    cabFields.Add(new CF_FREQ_RX { Seq=seq++ });
                    break;
            }


            cabFields.Add(new CabrilloField { Seq=seq++, Code=CbrCodeEnum.mo, SourceCol=CbrColEnum.Mode, DataType=CabrilloDataType.QSOMode });
            cabFields.Add(new CabrilloField { Seq=seq++, SourceCol=CbrColEnum.QSO_Begin, Code=CbrCodeEnum.date, DataType=CabrilloDataType.Date, StringFmt=new StringFMT { Fmt="yyyy-MM-dd" } });
            cabFields.Add(new CabrilloField { Seq=seq++, SourceCol=CbrColEnum.QSO_Begin, Code=CbrCodeEnum.time, DataType=CabrilloDataType.Date, StringFmt=new StringFMT { Fmt="HHmm" } });
            cabFields.Add(new CF_CALL { Seq=seq++, SourceCol=CbrColEnum.STATION_CALLSIGN, Code=CbrCodeEnum.STATION_CALLSIGN }); //
            switch (contestCode) {
                case ContestEnum.ARRL_VHF_JUN:
                    break;
                default:
                    cabFields.Add(new CF_RST { Seq=seq++, Code=CbrCodeEnum.TX_rst, SourceCol=CbrColEnum.RST_Sent });
                    break;
            }//   


          //   cabFields.Add(new CF_RST { Seq=seq++, Code=CbrCodeEnum.TX_rst, SourceCol=CbrColEnum.RST_Sent });
            cabFields.Add(new CF_SERIAL { Seq=seq++, Code=CbrCodeEnum.STX_STRING, SourceCol= CbrColEnum.STX});
            cabFields.Add(new CF_CALL { Seq=seq++, SourceCol=CbrColEnum.Call, Code=CbrCodeEnum.call }); //
            switch (contestCode) {
                case ContestEnum.ARRL_VHF_JUN:
                    break;
                default:
                    cabFields.Add(new CF_RST { Seq=seq++, Code=CbrCodeEnum.RX_rst, SourceCol=CbrColEnum.RST_Rcvd });
                    break;
            }//   

        //    cabFields.Add(new CF_RST { Seq=seq++, Code=CbrCodeEnum.RX_rst });
            cabFields.Add(new CF_SERIAL { Seq=seq++, Code=CbrCodeEnum.SRX_STRING, SourceCol=CbrColEnum.SRX });

            try {
                foreach (CabrilloField field in cabFields) {
                    field.ContestCode=contestCode;
                    field.SetSourceField(); // May modify the source field depending on contest or other variables
                    field.DataRow=row;
                    field.SetValues();
                }
            }catch (Exception ex) {

            }
            return cabFields;
        }
        [Obsolete("Use SRX1/SRX2")]
        public static string GetSRX_RSGB_IOTA(string fmtValue) {
            string[] items = fmtValue.Split(new char[] { ' ' });
            string SRX1 = string.Format("{0:0000}", int.Parse(items[0]));
            string SRX2 = "";
            if (items.Length==2) {
                SRX2=$"{Util.GetLetters(items[1])}-{string.Format("{0:000}", Util.GetNumbersInt(items[1]))}";
            } else {
                SRX2="------";
            }
            fmtValue=$"{SRX1} {SRX2}";
            return fmtValue;
        }
    }
}


