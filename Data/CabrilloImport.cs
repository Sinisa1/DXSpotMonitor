using Common;
using DataAccessLayer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace Data {
    public enum LogType {
        CABRILLO,
        ADIF,
        CUSTOM
    }
    public class QSO { // Matches QSO table
        public int LogFileSeq { get; set; }
        public int QSOSeq { get; set; } // Unique within LogFileSeq
        public int ContestYear { get; set; }
        public int QSO_YEAR { get; set; }
        public DateTime QSO_BEGIN_UTC { get; set; }
        public int BAND { get; set; }   // USE BAND
        public Band Band { get; set; }
        public double FREQ_RX { get; set; }
        public string QSOMODE { get; set; }
        public string QSO_BEGIN_UTC_DATE_STRING { get; set; }
        public string QSO_BEGIN_UTC_TIME_STRING { get; set; }



        public CallSign STATION_CALLSIGN { get; set; } // base call sign  CALL/P   =>/P removed


        public int RST_SENT { get; set; }
        public string STX { get; set; }
        public CallSign CALLSIGN { get; set; }
        public string DXCCPrefix { get; set; }  //Derived from CallSign and Big CTY lookup

        public int RST_RCVD { get; set; }
        public string SRX { get; set; }
        public int Errors { get; set; }
        public string ErrorsText { get; set; }
        public string QSOLineString { get; set; }
        public int Match_CptyCount { get; set; } // How many times this QSO was matched to primary QSOs

        public int Match_LogFileSeq { get; set; } // Matched to this Log File as primary
        public int Match_QSOSeq { get; set; }// Matched to this QSO as primary
        public int Match_Quality { get; set; }
        public string Match_Type { get; set; }

        public DateTime GetUTCFromStrings() {
            string dtStr = $"{QSO_BEGIN_UTC_DATE_STRING} {QSO_BEGIN_UTC_TIME_STRING}";
            DateTime dt = DateTime.ParseExact(dtStr, "yyyy-MM-dd HHmm", CultureInfo.InvariantCulture);
            return dt;
        }

        public static QSO ConvertCabrilloLineToQSO(string qsoLineCabrillo) {
            string[] items = qsoLineCabrillo.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            BandInfo bandInfo = new BandInfo();

            QSO qso = new QSO {

                FREQ_RX=double.Parse(items[1]),
                QSOMODE=items[2],
                QSO_BEGIN_UTC_DATE_STRING=items[3],
                QSO_BEGIN_UTC_TIME_STRING=items[4],
                STATION_CALLSIGN=new CallSign { CallSignBase=items[5] },
                RST_SENT=int.Parse(items[6]),
                STX=items[7],
                CALLSIGN=new CallSign { CallSignBase=items[8] },
                RST_RCVD=int.Parse(items[9]),
                SRX=items[10]
            };
            qso.Band=bandInfo.GetBandFromFreq(qso.FREQ_RX);
            qso.QSO_BEGIN_UTC=qso.GetUTCFromStrings();
            return qso;
        }

        public static QSO ConvertDTRowToQSOInfoObject(DataRow row) {

            QSO qso = null;
            try {
                qso=new QSO {
                    LogFileSeq=row.Field<int>("LogFileSeq"),
                    QSOSeq=row.Field<int>("QSOSeq"),
                    ContestYear=row.Field<int>("ContestYear"),
                    QSO_YEAR=row.Field<int>("QSO_YEAR"),
                    QSO_BEGIN_UTC=row.Field<DateTime>("QSO_BEGIN_UTC"),
                    BAND=row.Field<int>("BAND"),
                    FREQ_RX=row.Field<int>("FREQ_RX"),
                    QSOMODE=row.Field<string>("QSOMODE"),
                    QSO_BEGIN_UTC_DATE_STRING=row.Field<string>("QSO_BEGIN_UTC_DATE_STRING"),
                    QSO_BEGIN_UTC_TIME_STRING=row.Field<string>("QSO_BEGIN_UTC_TIME_STRING"),
                    STATION_CALLSIGN=new CallSign { CallSignBase=row.Field<string>("STATION_CALLSIGN") },
                    RST_SENT=row.Field<int>("RST_SENT"),
                    STX=row.Field<string>("STX"),
                    CALLSIGN=new CallSign { CallSignBase=row.Field<string>("CALLSIGN") },
                    RST_RCVD=row.Field<int>("RST_RCVD"),
                    SRX=row.Field<string>("SRX"),
                    Errors=row.Field<int>("Errors"),
                    ErrorsText=row.Field<string>("ErrorsText"),
                    QSOLineString=row.Field<string>("QSOLineString"),

                    Match_CptyCount=row.Field<int>("Match_CptyCount"),
                    Match_QSOSeq=row.Field<int>("Match_QSOSeq"),
                    Match_Quality=row.Field<int>("Match_Quality"),
                    Match_Type=row.Field<string>("Match_Type")
                };
            } catch (Exception e) {

            }
            return qso;
        }
    }

    public class LogFile {
        public int LogFileSeq { get; set; } //sequence column Unique within ContestYear
        public bool ParserErrors = false;
        public LogType FileFileType { get; set; }  // Cabrillo, ADIF
                                                   //     public LoggerType FileType { get; set; }  // Cabrillo, ADIF

        public FileInfo FileInfo { get; set; }
        public List<string> AllLInes { get; set; } = new List<string>();
        public List<string> QSOLInes { get; set; } = new List<string>();
    }


    public class CabrilloImport {


        public List<LogFile> LogFileList = new List<LogFile>();
        public List<LogFile> LogFileWithErrorsList = new List<LogFile>();
        DBManager dBManager = null;
        string sqlServerConnectionString_ = "";
        public CabrilloImport(string sqlServerConnectionString) {
            sqlServerConnectionString_=sqlServerConnectionString;

            dBManager=new DBManager(DataProvider.SqlServer, sqlServerConnectionString_);  // See if DBManager is needed here
            dBManager.Open();
        }
        public RetCode ReadCBRLogFileIntoQsoList(string cbrLogFilePath, ref List<string> allLines, ref List<QSO> qsoList) {

            RetCode ret = new RetCode();
            allLines=File.ReadAllLines(cbrLogFilePath).ToList();
            //  logFile.QSOLInes=logFile.AllLInes.Where(x => x.StartsWith("QSO:")).ToList<string>();
            qsoList=new List<QSO>();
            ret=ParseQSOLinesCabrilloWithBigCTYToQSOList(ref qsoList, allLines);

            return ret;
        }


        public RetCode ParseQSOLinesCabrilloWithBigCTYToQSOList(ref List<QSO> qsoList, List<string> allLogLines) { // NOt OQP complex parsing
            RetCode ret = new RetCode();
          //  OQPCabrilloRecord rec = new OQPCabrilloRecord();
            qsoList=new List<QSO>();
            List<string> qsoLines = allLogLines.Where(x => x.StartsWith("QSO:")).ToList<string>();
            int qsoSeq = 1;

            BigCTYDat bigCTY = new BigCTYDat(sqlServerConnectionString_);
            foreach (string qsoLine in qsoLines) {
                QSO qso = new QSO();
                try {
                    qso=QSO.ConvertCabrilloLineToQSO(qsoLine);
                    qso.QSOSeq=qsoSeq++;
                    List<vCTYCallPfxOverride> att = new List<vCTYCallPfxOverride>();
                    bigCTY.LoadUpdCallFromCTY(qso.CALLSIGN.CallSignBase, ref att);
                    qso.DXCCPrefix=att[0].DXCCPrefix;
                } catch (Exception e) {
                } finally {
                    qsoList.Add(qso);
                }
            }

            return ret;

        }

        public RetCode ParseQSOLinesCabrilloBasicToQSOList(ref List<QSO> qsoList , List<string> allLogLines) { // NOt OQP complex parsing
            RetCode ret = new RetCode();
            //  OQPCabrilloRecord rec = new OQPCabrilloRecord();
            qsoList=new List<QSO>();
            List<string> qsoLines = allLogLines.Where(x => x.StartsWith("QSO:")).ToList<string>();
            int qsoSeq = 1;

            foreach (string qsoLine in qsoLines) {
                QSO qso = new QSO();
                try {
                    qso=QSO.ConvertCabrilloLineToQSO(qsoLine);
                    qso.QSOSeq=qsoSeq++;
                } catch (Exception e) {
                } finally {
                    qsoList.Add(qso);
                }
            }
            return ret;
        }

    }
}
