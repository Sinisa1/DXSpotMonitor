using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace Common {

   


    public class MatchedCalls {
        public string Call { get; set; }
        public string CallMorse { get; set; }
        public double MatchPct { get; set; }
        public double MatchScore { get; set; }
        public bool InSuperCheck { get; set; }
        public int WkdBeforeCount { get; set; }
        public bool StartsWith { get; set; }
        public bool Contains { get; set; }

        // ✅ OPTIMIZATION: Remove StationInfo (only create when needed)
        // public StationInfo StationInfo = new StationInfo();

        // ✅ Display methods unchanged
        public string GetSearchLabel() {
            string label = InSuperCheck ? "s" : " ";
            if (WkdBeforeCount > 0) {
                label += $"W_{WkdBeforeCount,-3}";
            }
            else {
                label += "     ";
            }
            label += $" {Math.Round(MatchScore, 0),4}({(Math.Round(MatchPct, 0))})";
            return label;
        }

        public string GetSC_Wkd() {
            string label = InSuperCheck ? "s" : " ";
            if (WkdBeforeCount > 0) {
                label += $"W_{WkdBeforeCount,-3}";
            }
            else {
                label += "     ";
            }
            return label;
        }

        public string GetScore() {
            return $" {Math.Round(MatchScore, 0),4}({(Math.Round(MatchPct, 0)),3})";
        }
    }



    public class DynamicTBGridContainer {
        public string Name { get; set; }
        public string Type { get; set; } // Type of the object
        public string DisplayName { get; set; } // Display name for the object
        public string Description { get; set; } // Description of the object
        public bool IsVisible { get; set; } = true;
        public bool IsEnabled { get; set; } = true;
        public bool IsReadOnly { get; set; } = false;
    }

  
    public class USAPInfo { // maps to USAPInfo table
                            //      STATE DXCC    LOCATION CONT    CQ ITU IOTA TIME    SEARCH MAP
                            //      NH United States New Hampshire NA	5	8		5	K K
        public string STATE { get; set; }
        public string DXCC { get; set; }
        public string LOCATION { get; set; }
        public string CONT { get; set; }
        public int CQ { get; set; }
        public int ITU { get; set; }
        public int IOTA { get; set; }
        public int TIME { get; set; }
        public string SEARCH { get; set; } //DXCC K
        public string MAP { get; set; }   //DXCC Prefix K, KL7
    }
    public class CanadaPrefix { // maps to CanadaPrefix table
     //   MainPFX ProvCode    SpecialPFX PFXReason   PFXComment
     //VA1 NS CF1 NULL NULL
        public string MainPFX { get; set; }
        public string ProvCode { get; set; }
        public string SpecialPFX { get; set; }  // VEx, VAx mapped to themselves for easier mapping and search
        public string PFXReason { get; set; }
        public string PFXComment { get; set; }     
    }

    public class QSOBase {  // Ancestor of all QSO classes
        public string STATION_CALLSIGN = ""; // From Access
        public string MYQTHID = ""; // From Access
        public string Call { get; set; }
        public string Mode { get; set; }
        public int Band { get; set; } // Easier to dispaly string than int
        public double FreqMHz { get; set; }
        public double FreqKHz { get { return FreqMHz * 1000.0; } }
        public string FreqMHzString { get { return FreqMHz.ToString(); } }
        public DateTime QSO_Begin { get; set; }
        public int QSOYear { get { return ((DateTime)QSO_Begin).Year; } }
        public string STX { get; set; }
        public string SRX { get; set; }
        public string SRX1 { get; set; }
        public string SRX2 { get; set; }
        public int CQZ { get; set; }
        public int ITUZ { get; set; }
        public string CONT { get; set; } //Continent
        public string CONTEST_ID { get; set; }

        public void SplitSRX() {
            string[] items = SRX.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (items.Length>0) {
                SRX1=items[0];
            }
            if (items.Length>1) {
                SRX2=items[1];
            }
        }

    }


    public class GenericDTRowsColumns {
        // ── Label columns (always string) ──────────────────────────────────────
        public string RowLabel0 { get; set; }
        public string RowLabel1 { get; set; }

        // ── Value columns stored as int? for correct numeric sorting ───────────
        // The string properties (ColValue00..09) are kept for binding/display.
        // The int properties (ColInt00..09) are what the DataGrid sorts on.
        public string ColValue00 { get; set; }
        public string ColValue01 { get; set; }
        public string ColValue02 { get; set; }
        public string ColValue03 { get; set; }
        public string ColValue04 { get; set; }
        public string ColValue05 { get; set; }
        public string ColValue06 { get; set; }
        public string ColValue07 { get; set; }
        public string ColValue08 { get; set; }
        public string ColValue09 { get; set; }

        // ── Typed shadow properties for correct numeric sorting ────────────────
        public int? ColInt00 { get; set; }
        public int? ColInt01 { get; set; }
        public int? ColInt02 { get; set; }
        public int? ColInt03 { get; set; }
        public int? ColInt04 { get; set; }
        public int? ColInt05 { get; set; }
        public int? ColInt06 { get; set; }

        // RowLabel0 as int for CQZ zone numeric sort
        public int? RowLabelInt0 { get; set; }

        // ── Converters ────────────────────────────────────────────────────────

        public static GenericDTRowsColumns Convert_DXSpiderContestNotWorkedDXCC_DTRowToGenericDTRowsColumns(DataRow row) {
            try {
                var v00 = row.Field<int?>("All");
                var v01 = row.Field<int?>("160");
                var v02 = row.Field<int?>("80");
                var v03 = row.Field<int?>("40");
                var v04 = row.Field<int?>("20");
                var v05 = row.Field<int?>("15");
                var v06 = row.Field<int?>("10");
                return new GenericDTRowsColumns {
                    RowLabel0  = row.Field<string>("CONT"),
                    RowLabel1  = row.Field<string>("DXCCPrefix"),
                    ColValue00 = v00?.ToString() ?? "",
                    ColValue01 = v01?.ToString() ?? "",
                    ColValue02 = v02?.ToString() ?? "",
                    ColValue03 = v03?.ToString() ?? "",
                    ColValue04 = v04?.ToString() ?? "",
                    ColValue05 = v05?.ToString() ?? "",
                    ColValue06 = v06?.ToString() ?? "",
                    ColInt00 = v00,
                    ColInt01 = v01,
                    ColInt02 = v02,
                    ColInt03 = v03,
                    ColInt04 = v04,
                    ColInt05 = v05,
                    ColInt06 = v06,
                };
            } catch (Exception ex) {
                return null;
            }
        }

        public static GenericDTRowsColumns Convert_DXSpiderContestNotWorkedCONT_DTRowToGenericDTRowsColumns(DataRow row) {
            try {
                var v00 = row.Field<int?>("All");
                var v01 = row.Field<int?>("160");
                var v02 = row.Field<int?>("80");
                var v03 = row.Field<int?>("40");
                var v04 = row.Field<int?>("20");
                var v05 = row.Field<int?>("15");
                var v06 = row.Field<int?>("10");
                return new GenericDTRowsColumns {
                    RowLabel0  = row.Field<string>("CONT"),
                    ColValue00 = v00?.ToString() ?? "",
                    ColValue01 = v01?.ToString() ?? "",
                    ColValue02 = v02?.ToString() ?? "",
                    ColValue03 = v03?.ToString() ?? "",
                    ColValue04 = v04?.ToString() ?? "",
                    ColValue05 = v05?.ToString() ?? "",
                    ColValue06 = v06?.ToString() ?? "",
                    ColInt00 = v00,
                    ColInt01 = v01,
                    ColInt02 = v02,
                    ColInt03 = v03,
                    ColInt04 = v04,
                    ColInt05 = v05,
                    ColInt06 = v06,
                };
            } catch (Exception ex) {
                return null;
            }
        }

        public static GenericDTRowsColumns Convert_DXSpiderContestNotWorkedCQZ_DTRowToGenericDTRowsColumns(DataRow row) {
            try {
                var cqz = row.Field<int?>("CQZ");
                var v00 = row.Field<int?>("All");
                var v01 = row.Field<int?>("160");
                var v02 = row.Field<int?>("80");
                var v03 = row.Field<int?>("40");
                var v04 = row.Field<int?>("20");
                var v05 = row.Field<int?>("15");
                var v06 = row.Field<int?>("10");
                return new GenericDTRowsColumns {
                    RowLabel0     = cqz?.ToString() ?? "",
                    RowLabelInt0  = cqz,              // typed for numeric sort
                    ColValue00    = v00?.ToString() ?? "",
                    ColValue01    = v01?.ToString() ?? "",
                    ColValue02    = v02?.ToString() ?? "",
                    ColValue03    = v03?.ToString() ?? "",
                    ColValue04    = v04?.ToString() ?? "",
                    ColValue05    = v05?.ToString() ?? "",
                    ColValue06    = v06?.ToString() ?? "",
                    ColValue07    = row.Field<string>("CQZName"),
                    ColValue08    = row.Field<string>("CQZPrefixes"),
                    ColInt00 = v00,
                    ColInt01 = v01,
                    ColInt02 = v02,
                    ColInt03 = v03,
                    ColInt04 = v04,
                    ColInt05 = v05,
                    ColInt06 = v06,
                };
            } catch (Exception ex) {
                return null;
            }
        }

        public static GenericDTRowsColumns Convert_DXSpiderContestNotWorkedDXCCMultiCSV_DTRowToGenericDTRowsColumns(DataRow row) {
            try {
                return new GenericDTRowsColumns {
                    RowLabel0  = row["Band"]?.ToString() ?? "",
                    ColValue00 = row["DXCC"]?.ToString() ?? "",
                };
            } catch(Exception ex) {
                return null;
            }
        }

        // ── CQZ_MULTI_CSV: Band + comma-separated CQZ list (DSType intentionally skipped) ──
        public static GenericDTRowsColumns Convert_DXSpiderContestNotWorkedCQZMultiCSV_DTRowToGenericDTRowsColumns(DataRow row) {
            try {
                return new GenericDTRowsColumns {
                    RowLabel0  = row["Band"]?.ToString() ?? "",
                    ColValue00 = row["CQZ"]?.ToString() ?? "",
                };
            } catch(Exception ex) {
                return null;
            }
        }
    }

    public class DXSpiderQSOBase:QSOBase { // D not put any derived fields here. Slow to cast datatable to list of objects
        public static List<MultiType> MtTypeList = new List<MultiType> { MultiType.IGNORE,MultiType.IGNORE };
        //
        public string CallPrefix { get; set; }
        public string CountryName { get; set; }
        public int SpotCount { get; set; }



        public string RptByCall { get; set; }
        public int RptCQZ { get; set; }
        public string RptByType { get; set; }
        public int GainDb { get; set; }
        public int Q { get; set; }
        public byte IsStar { get; set; }
        public byte IsPlus { get; set; }
        public string CQZones { get; set; }

        //public int? CQZ { get; set; }
        //public int? ITUZ { get; set; }
        public int? CQZWkd { get; set; }
        public int? ITUZWkd { get; set; }

        // M!, M2 are from RBN, WM1, WM2  are from QSOs. Used for multipliers in contests
        public string M1 { get; set; }
        public string M2 { get; set; }
        public int WM1 { get; set; }
        public int WM2 { get; set; }
        public string WM1String { get { return WM1 == 0 ? "" : WM1.ToString(); } }
        public string WM2String { get { return WM2 == 0 ? "" : WM2.ToString(); } }

        //Binding
        public string DXCCPrefix { get; set; }
        //     public int? Zone { get; set; }// Worked in contest  CQZ/ITUZ depends on contest
        public string DXCCPrefixWkd { get; set; }// Worked in contest
        public int? ZoneWkd { get; set; }// Worked in contest  CQZ/ITUZ depends on contest
        public int MinAgo { get; set; }
        public long RowNum { get; set; } // Use for filteering to avoid duplicate rows when filter conditions overlap :CQZones and CQZ of the skimmer 
        //Skimmer information from rbn.Skimmer
        public int? SKCQZ { get; set; }
        public int? SKITUZ { get; set; }
        public int? SKQuality { get; set; }
        public int? SKDistanceToFN03em { get; set; }
        public string SKGridSquare { get; set; }
        public string SKStateProv { get; set; }
        public string SKLocation { get; set; }
        public string SKDescription { get; set; }
        public int? WkdContestCurrYear { get; set; } // Worked in contest on band and mode
        public int? WkdContestPrevYear { get; set; }// Worked in contest on ANY band and mode

        public string WkdContestCurrYearString { get { return WkdContestCurrYear>0 ? WkdContestCurrYear.ToString() : " "; } }
        public string WkdContestPrevYearString { get { return WkdContestPrevYear>0 ? WkdContestPrevYear.ToString() : " "; } }


    }

    public class DXSpiderContestNotWorked:DXSpiderQSOBase { // Host for SP GetD XSpiderContestNotWorked
        // Add this property to fix the binding error
        public bool Highlight { get; set; }
        public static DXSpiderContestNotWorked ConvertDTRowToDXSpiderContestNotWorkedObject(DataRow row) {
            try {
                DXSpiderContestNotWorked spotNotWkd = new DXSpiderContestNotWorked {
                    MinAgo=row.Field<int>("MinAgo"),
                    RowNum=row.Field<long>("RowNum"),
                    
                    Call=row.Field<string>("DXcall"),
                    FreqMHz=(double)row.Field<decimal>("FreqMHz"),

                    Mode=row.Field<string>("Mode"),
                    Band=row.Field<int>("Band"),
                    CountryName=row.Field<string>("CountryName"),
                    DXCCPrefix=row.Field<string>("DXCCPrefix"),
                    CQZ=row.Field<int>("CQZ"),
                    ITUZ=row.Field<int>("ITUZ"),
                    CONT=row.Field<string>("CONT"),
                    CQZWkd=row.Field<int?>("CQZWkd"),
                    ITUZWkd=row.Field<int?>("ITUZWkd"),
                    DXCCPrefixWkd=row.Field<string>("DXCCPrefixWkd"),
                    CallPrefix=row.Field<string>("Pfx"),
                    RptByCall=row.Field<string>("RptByCall"),
                    RptCQZ=row.Field<int>("RptCQZ"),
                    RptByType=row.Field<string>("RptByType"),
                    GainDb=row.Field<int>("GainDb"),
                    Q=row.Field<int>("Q"),
                    IsStar=row.Field<byte>("IsStar"),
                    IsPlus=row.Field<byte>("IsPlus"),
                    CQZones=row.Field<string>("CQZones"),
                    //Skimmer information from rbn.Skimmer
                    SKCQZ=row.Field<int?>("SKCQZ"),
                    SKITUZ=row.Field<int?>("SKITUZ"),
                    SKQuality=row.Field<int?>("SKQuality"),
                    SKDistanceToFN03em=row.Field<int?>("SKDistanceToFN03em"),
                    SKGridSquare=row.Field<string>("SKGridSquare"),
                    SKStateProv=row.Field<string>("SKStateProv"),
                    SKLocation=row.Field<string>("SKLocation"),
                    SKDescription=row.Field<string>("SKDescription"),
                    WkdContestCurrYear=row.Field<int?>("WkdContestCurrYear"), // Worked in contest on band and mode
                    WkdContestPrevYear=row.Field<int?>("WkdContestPrevYear") // Worked in contest on ANY band and mode

                    


                };
 // SP Pfx is better               spotNotWkd.CallPrefix= StationInfo.GetPFX(  spotNotWkd.Call);
                return spotNotWkd;
            } catch (Exception ex) {
                return null;
            }
        }
    }
    public class DXSpiderDXCCNotWorked:DXSpiderQSOBase { // Host for SP GetDXSpiderContestNotWorkedBase
        public string LotWBandMode { get; set; }
        public string LotWBand { get; set; }
        public string LotWDXCC { get; set; }
        public DateTime LotWDT { get; set; }
        public string b160m { get; set; }
        public string b80m { get; set; }
        public string b40m { get; set; }
        public string b30m { get; set; }
        public string b20m { get; set; }
        public string b17m { get; set; }
        public string b15m { get; set; }
        public string b12m { get; set; }
        public string b10m { get; set; }
        public string b6m { get; set; }

        //public string RptByType { get; set; }
        //public int GainDb { get; set; }
        //public int Q { get; set; }
        //public byte IsStar { get; set; }
        //public byte IsPlus { get; set; }
        //public string CQZones { get; set; }

      public List<String> CQZList { get { return CQZones.Split(',').ToList<string>(); } } // Split the CSV list of reporting zones into a list of CQZ


        // Add this property to fix the binding error
        public bool Highlight { get; set; }
        public static DXSpiderDXCCNotWorked ConvertDTRowToDXSpiderDXCCNotWorkedObject(DataRow row) {
            try {
                DXSpiderDXCCNotWorked obj = new DXSpiderDXCCNotWorked {
                    MinAgo=row.Field<int>("MinAgo"),
                    Call=row.Field<string>("DXcall"),
                    FreqMHz=(double)row.Field<decimal>("Freq"),
                    Mode=row.Field<string>("Mode"),
                    Band=row.Field<int>("Band"),
                    DXCCPrefix=row.Field<string>("DXCCPrefix"),
                    CountryName=row.Field<string>("CountryName"),
                    SpotCount=row.Field<int>("SpotCount"),


                    LotWBand=row.Field<string>("LotWBand"),
                    LotWBandMode=row.Field<string>("LotWBandMode"),
                    LotWDXCC=row.Field<string>("LotWDXCC"),
                    LotWDT=row.Field<DateTime>("LotWDT"),
                    b160m=row.Field<string>("b160m"),
                    b80m=row.Field<string>("b80m"),
                    b40m=row.Field<string>("b40m"),
                    b30m=row.Field<string>("b30m"),
                    b20m=row.Field<string>("b20m"),
                    b17m=row.Field<string>("b17m"),
                    b15m=row.Field<string>("b15m"),
                    b12m=row.Field<string>("b12m"),
                    b10m=row.Field<string>("b10m"),
                    b6m=row.Field<string>("b6m"),

                    RptByType=row.Field<string>("RptByType"),
                    GainDb=row.Field<int>("GainDb"),
                    Q=row.Field<int>("Q"),
                    IsStar=row.Field<byte>("IsStar"),
                    IsPlus=row.Field<byte>("IsPlus"),
                    CQZones=row.Field<string>("CQZones")        




                };
                return obj;
            } catch (Exception ex) {
                return null;
            }
        }
    }

    public class DXCCOverride { // Host for SQL table DXCCOverride
        public string Call { get; set; }
        public string DXCCPrefix { get; set; }
        public string PrimeLocation { get; set; }  // Probably not needed
        public string DXCCName { get; set; }
        public int CountryCode { get; set; }
    }

    //public class MatchedCalls { // Host for SQL table SUPERCHECK_MASTER and used for matching worked
    //    public string Call;
    //    public string CallMorse;
    //    public double MatchPct;
    //    public double MatchScore;
    //    public bool InSuperCheck;
    //    public int WkdBeforeCount;
    //    public bool StartsWith; // the call starts with the search text
    //    public bool Contains; // the call contains the search text
    //    public StationInfo StationInfo= new StationInfo (); // Used to get the PFX, DXCC Continent for the call


    //    public string GetSearchLabel() { //complete label
    //        string label = InSuperCheck ? "s" : " ";  // ← "s" or space
    //        if (WkdBeforeCount > 0) {
    //            label += $"W_{WkdBeforeCount,-3}";  // ← Add worked count
    //        }
    //        else {
    //            label += "     ";  // ← 5 spaces
    //        }
    //        label += $" {Math.Round(MatchScore, 0),4}({(Math.Round(MatchPct, 0))})";
    //        return label;
    //    }

    //    public string GetSC_Wkd() {
    //        string label = InSuperCheck ? "s" : " ";  // ← "s" or space
    //        if (WkdBeforeCount > 0) {
    //            label += $"W_{WkdBeforeCount,-3}";  // ← Add worked count
    //        }
    //        else {
    //            label += "     ";  // ← 5 spaces
    //        }           
    //        return label;
    //    }


    //    public string GetScore() { //complete label
    //        string label = $" {Math.Round(MatchScore, 0),4}({(Math.Round(MatchPct, 0)),3})";
    //        return label;
    //    }
    


    //}

    public class DGVRowDisplayBase { //Displays information about current QSO or the callsign which is in the search box.
        public int RowNum { get; set; } // If the dataset has ordereed row numbers
        public string Item { get; set; }
        public string Value { get; set; }
        public string CellFormat { get; set; }
        public string BACKGROUND { get; set; }
        public string FONTBOLD { get; set; }

    }

    public class DGVRowDisplayBaseOLD { //Displays information about current QSO or the callsign which is in the search box.
        public int RowNum { get; set; } // If the dataset has ordereed row numbers
        public string Item { get; set; }
        public string Value { get; set; }
        public string CellFormat { get; set; }
        public string BACKGROUND { get; set; }
        public string FONTBOLD { get; set; }

    }
    public class BandInfo1:DGVRowDisplayBaseOLD {  // Use with stored procedure
        public string b160m { get; set; }
        public string b80m { get; set; }
        public string b40m { get; set; }
        public string b30m { get; set; }
        public string b20m { get; set; }
        public string b17m { get; set; }
        public string b15m { get; set; }
        public string b12m { get; set; }
        public string b10m { get; set; }
        public string b6m { get; set; }
    }

    public class ContestQSOTotalsInfo:BandInfo1 {  // Use with stored procedure   
                                                   //        CQZone	160m	80m	40m	20m	15m	10m	Prefixes Mode    QSO_YEAR contest_id
        public int? QSO_YEAR { get; set; }
        public string contest_id { get; set; }
        public int? All { get; set; }  // Total QSOs
    }

    public class ContestCQZInfo:BandInfo1 {  // Use with stored procedure   
        //QSO_YEAR contest_id  All	160m	80m	40m	20m	15m	10m
        //2022	CQ-WW-CW	1058	3	90	276	247	263	179
        public int? CQZone { get; set; }
        public string Prefixes { get; set; }
        public string ZoneName { get; set; }
        public string Mode { get; set; }
        public int? QSO_YEAR { get; set; }
        public string contest_id { get; set; }
        public ContestCQZInfo() {
        }
    }

    public class ContestDXCCInfo:BandInfo1 {  // Use with stored procedure   
                                              // DXCCName	160m	80m	40m	20m	15m	10m	DXCCPrefix DXCCID  QSO_YEAR contest_id  Mode
                                              //American Samoa NULL    NULL NULL    NULL	1	NULL KH8	9	2022	CQ-WW-CW CW
        public string DXCCName { get; set; }
        public string DXCCPrefix { get; set; }
        public string DXCCID { get; set; }
        public int? QSO_YEAR { get; set; }
        public string contest_id { get; set; }
        public string Mode { get; set; }
        public static ContestDXCCInfo ConvertDTRowToContestDXCCInfoObject(DataRow row) {
            try {
                ContestDXCCInfo disp = new ContestDXCCInfo {
                    RowNum=row.Field<int>("RowNum"),
                    DXCCName=row.Field<string>("DXCCName"),

                    b160m=row.Field<int?>("160").ToString(),
                    b80m=row.Field<int?>("80").ToString(),
                    b40m=row.Field<int?>("40").ToString(),
                    b20m=row.Field<int?>("20").ToString(),
                    b15m=row.Field<int?>("15").ToString(),
                    b10m=row.Field<int?>("10").ToString(),
                    DXCCPrefix=row.Field<string>("DXCCPrefix"),
                    DXCCID=row.Field<string>("DXCCID"),
                    QSO_YEAR=row.Field<int?>("QSO_YEAR"),
                    contest_id=row.Field<string>("contest_id"),
                    Mode=row.Field<string>("Mode")

                };
                return disp;
            } catch (Exception ex) {
                return null;
            }
        }
    }

    public class StationInfoFromQSOs:DGVRowDisplayBase {  // Use with stored procedure

        //Call QSONum  Name LotW    minQSODate maxQSODate
        //VK2GR	7	Allan Y	2020-09-11	2022-06-06
        public string Call { get; set; }
        public string QTH { get; set; }
        public int QSONum { get; set; }
        public string Name { get; set; }
        public string LotW { get; set; }
        public DateTime? MinQSODate { get; set; }
        public DateTime? MaxQSODate { get; set; }
    }

    public class OQPMulti {
        public string County { get; set; }
        public string Code { get; set; }
        public string Region { get; set; }

        public static List<OQPMulti> ParseOQPMultiConfigFile(string filePath) {
            List<OQPMulti> multis = new List<OQPMulti>();
            List<string> lines = File.ReadAllLines(filePath).ToList();
            lines=lines.Where(x => !x.Contains("#")).ToList<string>();
            foreach (string line in lines) {
                string[] parts = line.Split(new char[] { ',' });
                OQPMulti multi = new OQPMulti { County=parts[0].Trim(), Code=parts[1].Trim(), Region=parts[2].Trim() };
                multis.Add(multi);
            }
            return multis;
        }
    }

    public class CallSuffixSpecial {
        public string Suffix = "";
        public string DXCC = "";
        public string Comment = "";

        public static List<CallSuffixSpecial> GetList() {
            return new List<CallSuffixSpecial> {
            new  CallSuffixSpecial{ Suffix= "M",Comment="Mobile" },
            new  CallSuffixSpecial{ Suffix= "P",Comment="Portable" },
            new  CallSuffixSpecial{ Suffix= "MM",Comment="Maritime Mobile" },
            new  CallSuffixSpecial{ Suffix= "AM",Comment="Air Mobile" },
            new  CallSuffixSpecial{ Suffix= "QRP" },
            new  CallSuffixSpecial{ Suffix= "ROVER",Comment="Rover" },
            new  CallSuffixSpecial{ Suffix= "ROV",Comment="Rover" },
            new  CallSuffixSpecial{ Suffix= "R",Comment="Rover" },
            new  CallSuffixSpecial{ Suffix= "AG", DXCC="K",Comment="Awaiting General" },
            new  CallSuffixSpecial{ Suffix= "AE", DXCC="K",Comment="Awaiting Extra" },
            new  CallSuffixSpecial{ Suffix= "MM",Comment="Maritime Mobile" },
            new  CallSuffixSpecial{ Suffix= "DX",Comment="Outside US/CA" }
           };
        }
    }
    public class ContestScoreRecord {
        public string ContestID;

        public long QSO_ID { get; set; }
        public DateTime QSO_Begin { get; set; }
        public string Call { get; set; }
        public string DXCCPrefix { get; set; }
        public string CONT { get; set; }
        public int? Band { get; set; }
        public int? CQZ { get; set; }

        // Multiplier flags
        public byte? MultiDXCC { get; set; }
        public byte? MultiCQZ { get; set; }
        public byte? AnyMulti { get; set; }

        // Multiplier counts
        public int? MNumDXCC { get; set; }
        public int? MNumCQZ { get; set; }
        public int? TtlMulti { get; set; }

        // Points
        public int? QSOPoints { get; set; }
        public int? SumQSOPoints { get; set; }
        public int? TtlPoints { get; set; }
        public int? Diff { get; set; }

        // Next QSO predictions (2-point)
        public int NextQSO2P { get; set; }
        public int Next2PM1 { get; set; }
        public int Next2PM2 { get; set; }
        public decimal? Next2PM1Ratio { get; set; }
        public decimal? Next2PM2Ratio { get; set; }

        // Next QSO predictions (3-point)
        public int NextQSO3P { get; set; }
        public int Next3PM1 { get; set; }
        public int Next3PM2 { get; set; }
        public decimal? Next3PM1Ratio { get; set; }
        public decimal? Next3PM2Ratio { get; set; }
    }
}