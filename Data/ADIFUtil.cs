using Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using WinControlLibrary;

namespace Data {
    public class ADIFUtil:DataETLBase {
        string CreatedBy = $"{AppSettings.CallSign} ADIFCreator";
        string CreatedByVersion = "1.0";

        string ProgramID = $"{AppSettings.CallSign}_ADIFCreator";
        public static string EOR = "<EOR>";

        private TcpClient client = null;
        NetworkStream stream = null;
        SettingsLog settings = null;

        public ADIFUtil (    SettingsLog settings_) : base(settings_.SQLServerConnectionString) {
            settings=settings_;
        }

        ~ADIFUtil() {
            // Close everything.
            try {
                if (stream!=null) {
                    stream.Close();
                    stream=null;
                }

                if (client!=null) {
                    client.Close();
                    client=null;
                }
            } catch {
            }
        }

        public RetCode CreateADIFLog() {
            //lowercase
            //https://cqwpx.com/api/get/<request>/<mode>/callsign/<callsign>/   raw scores for the latest contest CW or SSB
            //https://cqwpx.com/api/get/raw/ph/callsign/va3 tnm

            RetCode ret = new RetCode();
            DataTable dt = GetQSODataContestForExport(settings.ContInfo .CabrilloCode, settings.ContestYear);
            List<string> logLines = new List<string>();
            List<string> qsoLines = GetQSOLines(dt, settings.ContInfo.Code);

            DateTimeInfo dtInfo = new DateTimeInfo(DateTime.Now);
            DateTimeInfo dtInfoUTC = new DateTimeInfo(DateTime.UtcNow);
            string adifVersion = "3.1.4";
            logLines.Add("ADIF file");
            logLines.Add($"created by {CreatedBy} on {dtInfoUTC.GetDateTimeDisplayLongFMTSS()} UTC");
            logLines.Add($"<ProgramID:{ProgramID.Length}>{ProgramID}");
            logLines.Add($"<ProgramVersion:{CreatedByVersion.Length}>{CreatedByVersion}");
            logLines.Add($"<Created_Timestamp:{dtInfoUTC.GetDateTimeYYYYMMDspaceHHMMSS().Length}>{dtInfoUTC.GetDateTimeYYYYMMDspaceHHMMSS()}");
            logLines.Add($"<ADIF_Ver:{adifVersion.Length}>{adifVersion}");
            logLines.Add("<EOH>");

            logLines.AddRange(qsoLines);


            string LogFileName = $"{AppSettings.CallSign}_{settings.ContInfo.CabrilloCode}_{dtInfo.GetDateTimeYYYYMMDD_HHMMSS()}.adi";

            string outDir = $@"{AppSettings.ROOT_DIR}\{AppSettings.CallSign}\OUTPUT\Cabrillo\{settings.ContestYear}\{settings.ContInfo.CabrilloCode}";
            if (!Directory.Exists(outDir)) {
                Directory.CreateDirectory(outDir);
            }

            string logFilePath = Path.Combine(outDir, LogFileName);
            Util.FileWriteLines(logFilePath, logLines);

            return ret;
        }
        protected override List<string> GetQSOLines(DataTable dt, ContestEnum contestCode) {

            List<QSOInfo> qsos = dt.AsEnumerable().Select(row => QSOInfo.ConvertDTRowToQSOInfoObject(row)).ToList<QSOInfo>();

            string overrideContestCode = "";
            if (settings.UseN1MMContestCodeForADIF) {
                overrideContestCode=settings.ContInfo.N1MMContestCode;

            }
            List<AdifRecord> adifs = new List<AdifRecord>();
            foreach( QSOInfo qso in qsos) {
                try {
                    AdifRecord adif = CreateADIFRecord(qso, ContestMode: settings.ContestMode, overrideContestCode: overrideContestCode, mode: "FULL");
                    adifs.Add(adif);
                } catch (Exception ex) {

                }
            }                
                qsos.Select(qso => CreateADIFRecord(qso, ContestMode: settings.ContestMode, overrideContestCode: overrideContestCode, mode: "FULL")).ToList<AdifRecord>();

            List<string> qsoLines = adifs.Select(x => $"{x.FmtRecordString} {ADIFUtil.EOR}").ToList<string>();
            return qsoLines;
        }


        public RetCode CreateQSOandInsertADIFRecord(QSOInfo qso) {
            RetCode ret = new RetCode();
            AdifRecord adifRecord = CreateADIFRecord(qso, ContestMode: settings.ContestMode, overrideContestCode: "", mode: "DXKEEPER");
            ret=InsertDxKeeperLog(adifRecord);
            return ret;
        }

        private AdifRecord CreateADIFRecord(QSOInfo qso, bool ContestMode, string overrideContestCode, string mode = "FULL") {
            string contestId = overrideContestCode.Length>0 ? overrideContestCode : qso.CabrilloCode;
            AdifRecord adifRecord = new AdifRecord { Name="DxKeeperInsertLog" };
            int seq = 0;
            //N1MM
            adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="CALL", Value=qso.stnInfo.CALL });
            adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="QSO_DATE", Value=qso.QSO_DATE });
            adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="TIME_ON", Value=qso.TIME_ON });
            adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="TIME_OFF", Value=qso.TIME_ON });
            adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="BAND", Value=qso.BANDM });  //TX
            adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="STATION_CALLSIGN", Value=qso.STATION_CALLSIGN });
            adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="FREQ", Value=qso.FREQ_MHz_FMT });
            adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="MODE", Value=qso.MODE });
            adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="RST_SENT", Value=qso.RST_SENT });
            adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="RST_RCVD", Value=qso.RST_RCVD });
            adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="OPERATOR", Value=qso.STATION_CALLSIGN });
            adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="CQZ", Value=qso.stnInfo.CQZ.ToString() });



            adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="Comment", Value=qso.Comment });
            if (ContestMode) {// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!  when exporting for contest from button make always contestmode true
                adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="CONTEST_ID", Value=contestId });
                adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="STX", Value=qso.STX });
                adifRecord .Fields.AddRange (GetContestSRX(contestId, ref seq,qso ));
            }

            adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="FREQ_RX", Value=qso.FREQ_MHz_FMT }); //RX  !!!!!!!!
            adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="BAND_RX", Value=qso.BANDM });  //RX
            adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="QTH", Value=qso.stnInfo.QTH });

 //           adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="TX_PWR", Value="100" });
            adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="APP_DXKeeper_LotW_MEMBER", Value=qso.stnInfo.APP_DXKeeper_LotW_MEMBER });
            adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="APP_DXKeeper_EQSL_MEMBER", Value=qso.stnInfo.APP_DXKeeper_EQSL_MEMBER });
            string home = qso.STATION_CALLSIGN=="VA3TNM" ? "Home" : "Home VE3ZR";
            adifRecord.Fields.Add(new AdifField { Seq=seq++,AdifCode="APP_DXKEEPER_MY_QTHID",Value=home });
            adifRecord.Fields.Add(new AdifField { Seq=seq++,AdifCode="OWNER_CALLSIGN",Value=qso.STATION_CALLSIGN });

            if(mode=="FULL") { // Some of these (DXCC? prevent DXKeeper to look up the DXCC)

           //     adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="APP_N1MM_EXCHANGE1", Value=qso.SRX });

                adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="DXCC", Value=qso.stnInfo.DXCCID.ToString() });
                adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="CONT", Value=qso.stnInfo.CONT });
                adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="PFX", Value=qso.stnInfo.PFX });      
            }

            //adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="STATE", Value=qso.stnInfo.State });
            adifRecord.Fields.Add(new AdifField { Seq=seq++, AdifCode="NAME", Value=qso.Name });  // This was commented out check why. 2024-12-13

            return adifRecord;
        }
        /// <summary>
        /// Send edited QSO fields to DXKeeper via ADIF TCP.
        /// DXKeeper matches on CALL+QSO_DATE+TIME_ON+BAND+MODE (all from QSO_Begin UTC):
        ///   - keys found  → updates the existing record
        ///   - keys absent → inserts a new record (expected behaviour on call change)
        /// Replaces UpdateQSOInAccessDB for DXKeeper synchronisation.
        /// </summary>
        public RetCode QSOUpdateADIF(QSOEditItem qso) { //NOT USED. DXKEEPER CANNOT UPDATE THE RECORD. IT INSERTS A NEW ONE.
            RetCode ret = new RetCode();
            AdifRecord adifRecord = CreateADIFUpdateRecord(qso);
            ret = InsertDxKeeperLog(adifRecord);
            return ret;
        }

        /// <summary>
        /// Builds an ADIF record from a GUI QSOEditItem.
        /// Contains the 5 DXKeeper key fields (CALL, QSO_DATE, TIME_ON, BAND, MODE)
        /// plus all editable fields (STX, SRX, FREQ, FREQ_RX).
        /// QSO_Begin is stored as UTC — used directly for QSO_DATE and TIME_ON.
        /// </summary>
        private AdifRecord CreateADIFUpdateRecord(QSOEditItem item) { //NOT USED. DXKEEPER CANNOT UPDATE THE RECORD. IT INSERTS A NEW ONE.
            AdifRecord adifRecord = new AdifRecord { Name = "DxKeeperInsertLog" };
            int seq = 0;

            // ── 5 DXKeeper identifying keys ────────────────────────────────────────
            adifRecord.Fields.Add(new AdifField { Seq = seq++,AdifCode = "CALL",Value = item.Call });
            adifRecord.Fields.Add(new AdifField { Seq = seq++,AdifCode = "QSO_DATE",Value = item.QSO_Begin.ToString("yyyyMMdd") });
            adifRecord.Fields.Add(new AdifField { Seq = seq++,AdifCode = "TIME_ON",Value = item.QSO_Begin.ToString("HHmmss") });
            adifRecord.Fields.Add(new AdifField { Seq = seq++,AdifCode = "BAND",Value = $"{item.Band}M" });
            adifRecord.Fields.Add(new AdifField { Seq = seq++,AdifCode = "MODE",Value = item.Mode });

            // ── editable fields ────────────────────────────────────────────────────
            adifRecord.Fields.Add(new AdifField { Seq = seq++,AdifCode = "STX",Value = item.STX.Trim() });
            adifRecord.Fields.Add(new AdifField { Seq = seq++,AdifCode = "SRX",Value = item.SRX.Trim() });
            adifRecord.Fields.Add(new AdifField { Seq = seq++,AdifCode = "SRX_STRING",Value = item.SRX });// USED FOR SOME CONTESTS
            adifRecord.Fields.Add(new AdifField { Seq = seq++,AdifCode = "FREQ",Value = item.FreqMHz.ToString("#0.00000") });
            adifRecord.Fields.Add(new AdifField { Seq = seq++,AdifCode = "FREQ_RX",Value = item.FreqMHz.ToString("#0.00000") });

            // ── Other not modified fields from source QSODisplay-──────────────────
            adifRecord.Fields.Add(new AdifField { Seq = seq++,AdifCode = "CONTEST_ID",Value = item.Source.CONTEST_ID });
            string RST = item.Mode=="CW" ? "599" : "59";
            adifRecord.Fields.Add(new AdifField { Seq = seq++,AdifCode = "RST_Rcvd",Value = RST });
            adifRecord.Fields.Add(new AdifField { Seq = seq++,AdifCode = "RST_Sent",Value = RST });

           
            string home = item.Source.STATION_CALLSIGN=="VA3TNM" ? "Home" : "Home VE3ZR";
            adifRecord.Fields.Add(new AdifField { Seq=seq++,AdifCode="APP_DXKEEPER_MY_QTHID",Value=home });




            return adifRecord;
        }


        private List<AdifField> GetContestSRX(string contestId, ref int seq, QSOInfo qso) {
            List<AdifField> list = new List<AdifField>();
            switch(contestId) {
                case "NAQP-CW":
                case "NAQPCW":
                    list.Add(new AdifField { Seq=seq++, AdifCode="NAME", Value=qso.SRX1 });
                    list.Add(new AdifField { Seq=seq++, AdifCode="STATE", Value=qso.SRX2 });


                    list.Add(new AdifField { Seq=seq++, AdifCode="SRX", Value=qso.SRX });  // FIX 2025-08-02  See if name/state is required for N1MM???
                    list.Add(new AdifField { Seq=seq++, AdifCode="NAME", Value=qso.Name });

                list.Add(new AdifField { Seq=seq++,AdifCode="SRX_STRING",Value=qso.SRX12 });  //  NAME STATE
                list.Add(new AdifField { Seq=seq++,AdifCode="APP_DXLOG_RCVD",Value=qso.SRX1 });
                list.Add(new AdifField { Seq=seq++,AdifCode="APP_DXLOG_RECINFO",Value=qso.SRX2 });




                break;
                default:
                
                    list.Add(new AdifField { Seq=seq++, AdifCode="SRX", Value=   $"{qso.SRX1} {qso.SRX2}".Trim()        }); // fix this WITH mandatory optional SRX2 RSGB_COMMONWEALTH
                    list.Add(new AdifField { Seq=seq++, AdifCode="NAME", Value=qso.Name });
                    break;
            }
            list.Add(new AdifField { Seq=seq++, AdifCode="APP_N1MM_EXCHANGE1", Value=qso.SRX });  // ARRL 10
            if (!Util.IsNumeric(qso.SRX)) {
                list.Add(new AdifField { Seq=seq++, AdifCode="STATE", Value=qso.SRX });  // ARRL 10
            }
            return list;
        }

        public RetCode InsertDxKeeperLog(AdifRecord adifRecord) {

            RetCode ret = new RetCode();
            string message = "";

            AdifCommand command = new AdifCommand { command="log", adif=adifRecord };

            message=command.commandText;
            try {

                client=new TcpClient();
                DxKeeperHost host = new DxKeeperHost();
                client.Connect(host.endPoint);

                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);    // Translate the passed message into ASCII and store it as a Byte array.

                stream=client.GetStream();
                stream.Write(data, 0, data.Length); // Send the message to the connected TcpServer.
                stream.Flush();

                // Buffer to store the response bytes.
                data=new Byte[256];

                // String to store the response ASCII representation.
                String responseData = String.Empty;

                // Read the first batch of the TcpServer response bytes.
                Int32 bytes = stream.Read(data, 0, data.Length);   // FAILS IN N1MM IS RUNNING
                responseData=System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                ret.Msg=$"Received: {responseData}";

            } catch (ArgumentNullException ex) {
                return new RetCode { Code=-1, Msg=$"ArgumentNullException: {ex.Message}" };
            } catch (SocketException ex1) {
                return new RetCode { Code=-1, Msg=$"SocketException: {ex1.Message}" };
            }
            return ret;
        }
    }

    public class AdifCommand {
        public AdifRecord adif = new AdifRecord();
        public string command { get; set; }

        public string commandText {
            get {
                string parameters = $"{adif.FmtRecordString} {ADIFUtil.EOR}";
                String message = $"<command:{command.Trim().Length}>{command.Trim()}<parameters:{parameters.Length}>{parameters}";
                return message;
            }
        }
    }
    public class AdifRecord {

        public string Name { get; set; }
        public List<AdifField> Fields = new List<AdifField>();
        public string FmtRecordString {
            get {
                string retVal = "";

                List<string> items = Fields.Where(x => x.Value!=null&&x.Value.Trim().Length>0).OrderBy(x => x.Seq).Select(x => x.ValueFmt).ToList<string>();
                retVal=string.Join(" ", items).Trim();
                return retVal;
            }
        }
    }
    public class AdifField {
        public int Seq { get; set; }
        public string Name { get; set; }
        public string AdifCode { get; set; } //RST_SENT
        public string Value { get; set; }
        public int LengthRawValue {
            get {
                return Value.Trim().Length;
            }
        }
        public string ValueFmt { //<RST_SENT:3>599
            get {
                return $"<{AdifCode}:{LengthRawValue}>{Value.Trim()}";
            }
        }

        public int LengthValueFmt {
            get {
                return ValueFmt.Trim().Length;
            }
        }
    }
}
