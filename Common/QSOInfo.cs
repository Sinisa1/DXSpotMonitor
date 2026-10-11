using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Common {

  

    public class StationInfo {

        public bool QRZ_Member = false;// Check QRZ before logging the QSO

        public string APP_DXKeeper_LotW_MEMBER = "";  //  Y/N Got from LotW DB
        public string APP_DXKeeper_EQSL_MEMBER = ""; //  Y/N Got from LotW DB

        public string CALL = "";
        public string Name = "";
        public string Region = "";
        public string PFX = "";   
        public string QTH = "";

        public string DXCCPrefix = "";
        public int DXCCID = 0;

        public string Country = "";
        public int CQZ = 0;
        public int ITUZ = 0;
        public string CONT = "";
        public double LAT = 0.0;
        public double LON = 0.0;
        public double LON_UTC_Shift {
            get {
                if (UTC_SHIFT>0) { // West of Greenwich
                    return -LON;
                } else if (UTC_SHIFT==0) {  // Greenwich 

                    return -LON;  // All countries are West of meridian 0
                } else if (UTC_SHIFT<0) {  // East of Greenwich 
                    return -LON;
                }
                return LON;
            }

        }
        public double UTC_OFFSET { get { return -UTC_SHIFT; } } // UTC offset in hours. Positive for East of Greenwich
        public string UTC_OFFSET_FMT { get { if(UTC_OFFSET>0) { return $"+{UTC_OFFSET}"; } else { return $"{UTC_OFFSET}"; } } }
        public DateTime LocalTimeTime { get { return Util.GetLocalTime(-UTC_SHIFT); } }
        public double UTC_SHIFT = 0.0;// Not in QSO entity

        //Derived values


        public string CountryInfoSource { get; set; } //AD1C_BIGCTY_PERL
        public bool QRZChecked = false;// If the QRZ has been checked since last reset
        public bool LotwChecked = false;// If the LotW has been checked since last reset
        public bool EQSLChecked = false;// If the eQSL has been checked since last reset
        public string EQSLMemberYesNo { get; set; } //If the callsign is a member of eQSL Yes/No

        public string LastUpload { get; set; } // From LotW table
        public string State { get; set; }// Updated from USAPCallGrid
        public string Grid { get; set; }// Updated from USAPCallGrid
        public double Bearing { get; set; }// Great Circle Bearing.  Calculated from coordinates
        public double Distance { get; set; }// Great Circle Distance. Calculated from coordinates

        public int BigCTYSeq { get; set; }    // Index of matching item in BigCTY file
        public string OverrideType { get; set; }    // Prefix or attibute override from BigCTY
        public string CQWW_WAE_Flag { get; set; }    //  BigCTY => If the caountry is WAE or CQWW mullti. * sign on BIgCTY file

        public StationInfo() {
            CALL="";
        }

        public static string GetPFX(string call) { // Gets the prefix of the callsign
            string pfx = "";

            int rightmostNumeric = Util.FindRightMostNumericIndex(call);
            if (rightmostNumeric>=0) {
                pfx=call.Substring(0, rightmostNumeric+1);
            }
            return pfx;          
        }
    }




  
    public class QSOInfo:QSOInfoBase {
        public string STATION_CALLSIGN = "";
        public string RST_SENT = "";
        public string RST_RCVD = "";

        public QSOInfo() : base() {
            stnInfo= new StationInfo();
        }
    }

        public class QSOInfoBase {
        public string TextSearchPrev { get; set; }  // callsign<space>SerialRx combo


        public StationInfo stnInfo = null;
           public double FREQ_kHz = 0;      


        public int BAND = 0;
        public string BANDM{ get { return $"{BAND}M"; } }
        public string MODE = "";
        public DateTimeInfo QSO_DT = new DateTimeInfo(DateTime.MinValue);
        public string QSO_DATE {
            get { return QSO_DT.GetDateYYYYMMDD(); }
        }
        public string TIME_ON {
            get { return QSO_DT.GetTimeHHMMSS(); }
        }
        public string FREQ_kHz_FMT {
            get {
                return GetFreqKHzFMTFromKHz();
            }
        }
        public double FREQ_MHz {
            get {
                return FREQ_kHz/1000.0;
            }
        }
        public string FREQ_MHz_FMT {
            get {
                return GetFreqMHZFMTFromKHz();
            }
        }

    
        public string TX_PWR = "";

        public string CabrilloCode = "";
        public string STX = "";  // Serial sent
        public string SRX {
            get {
                if(SerialTx2Enabled==true&&SRX2.Length>0) {
                    return $"{SRX1} {SRX2}";
                } else {
                    return SRX1;
                }
            }
        }
        public string SRX12 { //CONSOLIDATE WITH SRX
            get {
                if(SRX2.Length>0) {
                    return $"{SRX1} {SRX2}";
                } else {
                    return SRX1;
                }
            }
        }
        public string SRX1 = ""; //Serial1 received
        public string SRX2 = ""; //Serial2 received
        public bool SerialTx2Enabled = false;
        public string Name = "";  //OP Name
        public string Comment = "";

        public bool AlreadyWorkedPartialCallMatch { get; set; } // Is this the right place? 
        public bool AlreadyWorkedExactCallMatch { get; set; } // Is this the right place? 

        public QSOInfoBase() {
            stnInfo=new StationInfo();         
        }
        public string GetFreqMHZFMTFromKHz() { // use for new QSOs (not from DB)

            return (FREQ_kHz/1000.0).ToString("#0.00000");

        }
        public string GetFreqKHzFMTFromKHz() {      // use for new QSOs (not from DB)     
            return (FREQ_kHz).ToString("####0.00");
        }

        public static QSODisplay QSOInfoToQSODisplay(QSOInfo qsoInfo,string mode,bool contestMode) {
            string ContestCabrilloFmt = contestMode==true ? qsoInfo.CabrilloCode : "";

            QSODisplay qsoDisp = new QSODisplay {
                Call=qsoInfo.stnInfo.CALL.Trim(),Band=qsoInfo.BAND,Mode=qsoInfo.MODE,STX=qsoInfo.STX.Trim(),
                SRX=qsoInfo.SRX.Trim(),SRX1=qsoInfo.SRX1.Trim(),SRX2=qsoInfo.SRX2.Trim(),
                FREQ_RX_MHZ8_5=qsoInfo.FREQ_MHz_FMT,FREQ_RX_MHZ=qsoInfo.FREQ_kHz/1000.0,FREQ_RX_KHZ=qsoInfo.FREQ_kHz,
                FREQ_RX_KHZ8_2=qsoInfo.FREQ_kHz_FMT,CONTEST_ID=ContestCabrilloFmt,DXCCName=qsoInfo.stnInfo.Country,
                DXCCPrefix=qsoInfo.stnInfo.DXCCPrefix,CQZ=qsoInfo.stnInfo.CQZ,ITUZ=qsoInfo.stnInfo.ITUZ,PFX=qsoInfo.stnInfo.PFX,
                QSO_Begin=qsoInfo.QSO_DT.dt,
                QSOHHMM_YYYMMDD=qsoInfo.QSO_DT.GetDateTimeHHMM_YYYYMMDD(),
                QSOHHMM=qsoInfo.QSO_DT.dt.ToString("HH:mm")   // ← fix: populate HH:MM for the grid UTC column
            };
            switch(mode) {
            case "NEW_QSO":
                qsoDisp.LotW="N";
                break;
            default:
                break;
            }
            return qsoDisp;
        }

        public static QSOInfo ConvertDTRowToQSOInfoObject(DataRow row) {
            QSOInfo qsoInfo = null;
            try {

                StationInfo stnInfo = new StationInfo {

                    CALL=row.Field<string>("Call"),
                    APP_DXKeeper_LotW_MEMBER=row.Field<string>("LotW"),
                    DXCCPrefix=row.Field<string>("DXCCPrefix"),
                    Country=row.Field<string>("DXCCName"),

                    PFX=row.Field<string>("PFX"),
                    CONT=row.Field<string>("CONT"),

                };

                int.TryParse(row.Field<string>("DXCCID"), out stnInfo.DXCCID);
                stnInfo.CQZ=row.Field<int>("CQZ");
                stnInfo.ITUZ=row.Field<int>("ITUZ");
         
                DateTimeInfo dtInfo = new DateTimeInfo(row.Field<DateTime>("QSO_Begin"));

                qsoInfo=new QSOInfo {
                    stnInfo=stnInfo,
                    BAND=row.Field<int>("Band"),
                    MODE=row.Field<string>("Mode"),
                    QSO_DT=new DateTimeInfo(row.Field<DateTime>("QSO_Begin")),

                    RST_SENT=row.Field<string>("RST_SENT"),
                    STX=row.Field<string>("STX"),
                    RST_RCVD=row.Field<string>("RST_RCVD"),
  //                  SRX=row.Field<string>("SRX"),
                    SRX1=GetSRX1(row.Field<string>("SRX")),
                    SRX2=GetSRX2(row.Field<string>("SRX")),
                    FREQ_kHz=row.Field<double>("FREQ_RX_KHZ"),
                    CabrilloCode=row.Field<string>("contest_id"),
                    STATION_CALLSIGN=row.Field<string>("STATION_CALLSIGN"),
                };
            } catch (Exception e) {

            }
            return qsoInfo;
        }
        public static string GetSRX1(string srx) {
            string[] items = srx.Split(new char[] { ' ' });
            return items[0];
        }
        public static string GetSRX2(string srx) {
            string[] items = srx.Split(new char[] { ' ' });
            if (items.Length==2) {
                return items[1];
            } else {
                return "";
            }
        }
    }
    public class Band {
        public int Code { get; set; } // 20, 15
        public double MinFreqMHz { get; set; }
        public double MaxFreqMHz { get; set; }
        public int MaxFreqChangekHz { get; set; }  // Skip range  for DXSpider spots. If PA is ON, do not allow skipping bands or big changes
        public List<ModeRange> ModeRanges = new List<ModeRange>();
        public Band() {
            MaxFreqChangekHz=10; // Default.
        }
    }
    public class ModeRange {
        public QSOMode Mode { get; set; }
        public double Bottom { get; set; }
        public double Top { get; set; }

    }
    public class BandInfo {
        public List<Band> Bands { get; set; }
        public bool IsOutOfBand { get; set; }

        public BandInfo() { //https://www.dxzone.com/ft8-frequencies/
            Bands = new List<Band>();
            List<ModeRange> ranges;

            // 160 Meters
            ranges = new List<ModeRange>();
            ranges.Add(new ModeRange { Mode = QSOMode.CW,Bottom = 1.80,Top = 1.83499 });
            ranges.Add(new ModeRange { Mode = QSOMode.DG,Bottom = 1.835,Top = 1.8499 });
            ranges.Add(new ModeRange { Mode = QSOMode.SSB,Bottom = 1.850001,Top = 2.0 });
            Bands.Add(new Band { Code = 160,MinFreqMHz = 1.8,MaxFreqMHz = 2.0,ModeRanges = ranges,MaxFreqChangekHz = 20 });

            // 80 Meters
            ranges = new List<ModeRange>();
            ranges.Add(new ModeRange { Mode = QSOMode.CW,Bottom = 3.500,Top = 3.56999 });
            ranges.Add(new ModeRange { Mode = QSOMode.DG,Bottom = 3.570,Top = 3.59999 });
            ranges.Add(new ModeRange { Mode = QSOMode.SSB,Bottom = 3.600,Top = 4.0 });
            Bands.Add(new Band { Code = 80,MinFreqMHz = 3.5,MaxFreqMHz = 4.0,ModeRanges = ranges,MaxFreqChangekHz = 40 });

            // 60 Meters (Left empty per instructions)
            Bands.Add(new Band { Code = 60,MinFreqMHz = 5.3305,MaxFreqMHz = 5.405 });

            // 40 Meters (Fixed SSB bottom from 7.8 to 7.100)
            ranges = new List<ModeRange>();
            ranges.Add(new ModeRange { Mode = QSOMode.CW,Bottom = 7.0,Top = 7.03999 });
            ranges.Add(new ModeRange { Mode = QSOMode.DG,Bottom = 7.040,Top = 7.07999 });
            ranges.Add(new ModeRange { Mode = QSOMode.SSB,Bottom = 7.100,Top = 7.300 });
            Bands.Add(new Band { Code = 40,MinFreqMHz = 7.0,MaxFreqMHz = 7.3,ModeRanges = ranges,MaxFreqChangekHz = 50 });

            // 30 Meters
            ranges = new List<ModeRange>();
            ranges.Add(new ModeRange { Mode = QSOMode.CW,Bottom = 10.1,Top = 10.129999 });
            ranges.Add(new ModeRange { Mode = QSOMode.DG,Bottom = 10.13,Top = 10.15 });
            Bands.Add(new Band { Code = 30,MinFreqMHz = 10.1,MaxFreqMHz = 10.15,ModeRanges = ranges,MaxFreqChangekHz = 100 });

            // 20 Meters
            ranges = new List<ModeRange>();
            ranges.Add(new ModeRange { Mode = QSOMode.CW,Bottom = 14.0,Top = 14.0699 });
            ranges.Add(new ModeRange { Mode = QSOMode.DG,Bottom = 14.07,Top = 14.09499 });
            ranges.Add(new ModeRange { Mode = QSOMode.SSB,Bottom = 14.112,Top = 14.35 });
            Bands.Add(new Band { Code = 20,MinFreqMHz = 14.0,MaxFreqMHz = 14.35,ModeRanges = ranges,MaxFreqChangekHz = 200 });

            // 17 Meters (Fixed CW bottom from 18 to 18.068)
            ranges = new List<ModeRange>();
            ranges.Add(new ModeRange { Mode = QSOMode.CW,Bottom = 18.068,Top = 18.086999 });
            ranges.Add(new ModeRange { Mode = QSOMode.DG,Bottom = 18.087,Top = 18.1099 });
            ranges.Add(new ModeRange { Mode = QSOMode.SSB,Bottom = 18.11,Top = 18.168 });
            Bands.Add(new Band { Code = 17,MinFreqMHz = 18.068,MaxFreqMHz = 18.168,ModeRanges = ranges,MaxFreqChangekHz = 200 });

            // 15 Meters (Fixed DG bottom from 21.7 to 21.07)
            ranges = new List<ModeRange>();
            ranges.Add(new ModeRange { Mode = QSOMode.CW,Bottom = 21.0,Top = 21.06999 });
            ranges.Add(new ModeRange { Mode = QSOMode.DG,Bottom = 21.07,Top = 21.12499 });
            ranges.Add(new ModeRange { Mode = QSOMode.DG,Bottom = 21.125,Top = 21.149999 }); // DG+CW
            ranges.Add(new ModeRange { Mode = QSOMode.SSB,Bottom = 21.15,Top = 21.45 });
            Bands.Add(new Band { Code = 15,MinFreqMHz = 21.0,MaxFreqMHz = 21.45,ModeRanges = ranges,MaxFreqChangekHz = 250 });

            // 12 Meters
            ranges = new List<ModeRange>();
            ranges.Add(new ModeRange { Mode = QSOMode.CW,Bottom = 24.890,Top = 24.8999999 });
            ranges.Add(new ModeRange { Mode = QSOMode.DG,Bottom = 24.90,Top = 24.93999 });
            ranges.Add(new ModeRange { Mode = QSOMode.SSB,Bottom = 24.94,Top = 24.99 });
            Bands.Add(new Band { Code = 12,MinFreqMHz = 24.89,MaxFreqMHz = 24.99,ModeRanges = ranges,MaxFreqChangekHz = 120 });

            // 10 Meters
            ranges = new List<ModeRange>();
            ranges.Add(new ModeRange { Mode = QSOMode.CW,Bottom = 28.0,Top = 28.06999 });
            ranges.Add(new ModeRange { Mode = QSOMode.DG,Bottom = 28.07,Top = 28.1984999 });
            ranges.Add(new ModeRange { Mode = QSOMode.CW,Bottom = 28.2,Top = 28.2999 });
            ranges.Add(new ModeRange { Mode = QSOMode.SSB,Bottom = 28.3,Top = 29.29999 });
            ranges.Add(new ModeRange { Mode = QSOMode.SAT,Bottom = 29.3,Top = 29.51999 });
            ranges.Add(new ModeRange { Mode = QSOMode.FM,Bottom = 29.52,Top = 29.7 });
            Bands.Add(new Band { Code = 10,MinFreqMHz = 28.0,MaxFreqMHz = 29.7,ModeRanges = ranges,MaxFreqChangekHz = 400 });

            // 6 Meters and 2 Meters (Left untouched)
            Bands.Add(new Band { Code = 6,MinFreqMHz = 50.0,MaxFreqMHz = 54.0,MaxFreqChangekHz = 100 });
            Bands.Add(new Band { Code = 2,MinFreqMHz = 144.0,MaxFreqMHz = 148.0,MaxFreqChangekHz = 300 });
        }

        public static QSOMode GetMode(double freq) {
            QSOMode mode = QSOMode.UNK;

            BandInfo bandInfo = new BandInfo();
            string band = bandInfo.GetBandCodeString(freq);
            List<ModeRange> ranges = bandInfo.Bands.SelectMany(x => x.ModeRanges).ToList<ModeRange>();   // Get all ranges from all modes

            ModeRange range = ranges.Where(x => x.Bottom<=freq&&freq<=x.Top).FirstOrDefault();
            if (range!=null) {
                return range.Mode;
            }
            return mode;
        }


        public  string GetBandCodeM(double freq) {
            string band = GetBandCodeString(freq);

            return band+"M";
        }
        public static double ConvertFreqMHzkHz(string freq, ref bool success) {
            double outFreq = 0;
            success=  double.TryParse(freq, out outFreq);
            if (success) {
                outFreq=ConvertFreqMHzkHz(outFreq); // MHz to kHz
            }
            return outFreq;
        }

            public static double ConvertFreqMHzkHz(double freq) {
            if (freq<100) { // If in MHz
                freq*=1000.0;
            }
            return freq;
        }

        public   Band GetBandFromFreq(double freq) {
            freq=ConvertFreqMHzkHz(freq);
           
            Band band = Bands.Where(x => x.MinFreqMHz<=freq/1000.0&&freq/1000.0<=x.MaxFreqMHz).FirstOrDefault();
            return band;
        }
        public string GetBandCodeString(double freq) {
            freq=ConvertFreqMHzkHz(freq);

            Band band = GetBandFromFreq(freq);
            if (band==null) {

                IsOutOfBand=true;
                return "OUT";// $"WRONG BAND FOR Frequency: {freq}";             

            } else {
                IsOutOfBand=false;
                return band.Code.ToString();
            }
        }
        public int GetBandCodeInt(double freq) {
            freq=ConvertFreqMHzkHz(freq);

            Band band = GetBandFromFreq(freq);
            if (band==null) {

                IsOutOfBand=true;
                return 0;// $"WRONG BAND FOR Frequency: {freq}";             

            } else {
                IsOutOfBand=false;
                return band.Code;
            }
        }
    }
  
    public enum QSOMode {
        CW,
        PH,
        FM,
        RY,
        DG,
        SSB, USB, LSB,
        SAT,
        UNK,
        BCN, // RBN
        DXF, // RBN
    }
    public enum QSOItemCodes {
        SRX, PFX, DXCC, CQZ, ITUZ,
        SRX1, SRX2,
        REGION,   // State/Province fpr ARRL contests, some Russian/LZ/OK contests
                  //   SPECIAL_CALLS, // A list of calls which send SRX as code
        DEFAULT  // Assign default QSOItemCodes for multiplier type
    }
   
    public class vQSO {
        public string Call { get; set; }
        public string Band { get; set; }
        public string Mode { get; set; }
        public string DXCCPrefix { get; set; }
        public string DXCCActive { get; set; }
        public string DXCCName { get; set; }
        public string LotW { get; set; }
        public Nullable<System.DateTime> LotwLastUpload { get; set; }
        public string contest_id { get; set; }
        public Nullable<int> QSO_YEAR { get; set; }
        public Nullable<System.DateTime> QSODate { get; set; }
        public Nullable<System.DateTime> QSO_Begin { get; set; }
        public string Name { get; set; }
        public string DXCCID { get; set; }
        public string State { get; set; }
        public string QTH { get; set; }
        public string CQZ { get; set; }
        public string ITUZ { get; set; }
        public string PFX { get; set; }
        public int QSONumber { get; set; }
        public string CNTY { get; set; }
        public string STX { get; set; }
        public string SRX { get; set; }
        public string FREQ_RX_MHZ8_5 { get; set; }
    }

    public class LotWCallsign {
        public string CallSign { get; set; }
        public DateTime LastUpload { get; set; }
        public string LastUploadFMT { get { return LastUpload.ToString("yyyy-MM-dd"); }  }
    }
    public class USAPCallGrid {
        public string Call { get; set; }
        public string Grid { get; set; }
        public string State { get; set; }
    }

    public class MAP_CTYDAT_DXCC {
        public string CTYDAT_NAME { get; set; }
        public string DXCCName { get; set; }
        public string DXCCPrefix { get; set; }
        public string DXCCSearch { get; set; }
        public short CountryCode { get; set; }
    }
    public class eQSLAGCallsign {
        public string CallSign { get; set; }
    }
}