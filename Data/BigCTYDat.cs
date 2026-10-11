using Common;
using DataAccessLayer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Data {
    /*
     https://www.country-files.com/cty-dat-format/

    CTY.DAT is the format for CT Version 9. It includes more information than the .CTY files for previous versions of CT. This allows CT to calculate, in real time, beam heading and sun times. It also means that a single file can be used for all DX contests. The format is as follows. Note that the fields are aligned in columns and spaced out for readability only. It is the “:” at the end of each field that acts as a delimiter for that field:

COLUMN	LENGTH	DESCRIPTION
1	26	Country Name
27	5	CQ Zone
32	5	ITU Zone
37	5	2-letter continent abbreviation
42	9	Latitude in degrees, + for North
51	10	Longitude in degrees, + for West
61	9	Local time offset from GMT
70	6	Primary DXCC Prefix (A “*” preceding this prefix indicates that the country is on the DARC WAEDC list, and counts in CQ-sponsored contests, but not ARRL-sponsored contests).
Alias DXCC prefixes (including the primary one) follow on consecutive lines, separated by commas (,). Multiple lines are OK; a line to be continued should end with comma (,) though it’s not required. A semi-colon (;) terminates the last alias prefix in the list.

If an alias prefix is preceded by ‘=’, this indicates that the prefix is to be treated as a full callsign, i.e. must be an exact match.

The following special characters can be applied after an alias prefix:

(#)	Override CQ Zone
[#]	Override ITU Zone
<#/#>	Override latitude/longitude
{aa}	Override Continent
~#~	Override local time offset from GMT
See the examples on page 27 in the CT9 manual.

Important Information
A callsign can be associated with more than one entity in the file. 4U1VIC is one example. In the ARRL DX contests, for example, it counts for Austria, OE. In the CQWW contests, it counts as a unique multiplier, separate from Austria. See the information about “Primary DXCC Prefix” above. Software is expected to parse the file from top to bottom.  If a duplicate callsign is found, simply ignore it the second time.

Known Limitations
CT8 can override zones on a prefix basis, but can only override continents on a country basis. This doesn’t work in a few cases, notably IG9/IH9, which are in CQ Zone 33, and hence in Africa, but will be logged as Europe. Neither does it work for the part of Turkey that is in Europe – there is no way to just override the continent for the TA0 and TA1 prefixes. This is really only a problem for the IARU HF World Championship. In CQWW, the European part counts as a separate multiplier, hence CT gets the continent correct. In ARRL, contacts from W/VE are the same number of points whether Turkey is in Europe or Asia.This is not a problem in CT9, because you can now override the continent for a given prefix.
CT doesn’t handle prefixes such as FR5ZQ/J correctly. Listing FR/J as a prefix for Juan de Nova in the country file doesn’t work unless a station signs “FR/J” as his callsign. If you work one of these islands, you’ll have to add it to the country file manually. Otherwise “/J” will be logged as Japan, “/G” as England, “/T” as Reunion, and “/E” as an unknown prefix.








     * 
     * 
    */

    //   BigCtySeq CountryName PrimaryDXCCPfx OvrType Code CQZOvr  ITUZOvr ContinentOvr    LattitudeOvr LongitudeOvr    UTCOffsetOvr CQZ ITUZ Continent   Lattitude Longitude   UTCOffset
    //184	United States   K PFX KI9	4	8	NA	37.6	91.87	5	5	8	NA	37.6	91.87	5

    public class CTYDXCCPrefixOverride {  // Override CTY DAT if it is inaccurate IT9=>Italy, not Sicily
        public string CTYDXCCPrefix { get; set; }
        public string DXCCPrefix { get; set; }
        public string Comment { get; set; }

    }

        public class vCTYCallPfxOverride {  // Use  generate SQL Excel to make insert statements:INSERT INTO MAP_CTYDAT_DXCC_Prefix  ( DXCCPrefix, CountryCode, DXCCName, DXCCPrefixCTY, PrimaryDXCCPfxCTY, CountryNameCTY)  SELECT '3D2-F',176,'Fiji Islands','3D2','3D2','Fiji'


        public int BigCtySeq { get; set; }
        public string CountryName { get; set; }
        public string PrimaryDXCCPfx { get; set; } //Original E5/n
        public string DXCCPrefix { get; set; } //Converted to DXKeeper format E5-N
        public string OvrType { get; set; }
        public string Code { get; set; }
        public int CQZOvr { get; set; }
        public int ITUZOvr { get; set; }
        public string ContinentOvr { get; set; }
        public double LattitudeOvr { get; set; }
        public double LongitudeOvr { get; set; }
        public double UTCOffsetOvr { get; set; }
        public int CQZ { get; set; }
        public int ITUZ { get; set; }
        public string Continent { get; set; }
        public double Lattitude { get; set; }
        public double Longitude { get; set; }
        public double UTCOffset { get; set; }
        public string CQWW_WAE_Flag { get; set; } // * for WAE contests
        
        public vCTYCallPfxOverride() {
            DXCCPrefix="";

        }
    }


    public class CTYCallPfxInfoBase {   //public class CTYCallInfo:CTYCallPfxInfoBase 
        public string PrimaryDXCCPfx { get; set; } // reference to parent => FK
        public string Code { get; set; }
        public int CQZOverride = 0;   //(#)
        public int ITUZOverride = 0;  //[*]
        public double LattitudeOverride = 0.0;//<lat/Long>
        public double LongitudeOverride = 0.0;
        public string ContinentOverride = "";// {#}
        public double LocalTimeOffsetOverride = 0.0;// ~#~
    }

    public class CTYDxccInfo {
        public string CountryName { get; set; }
        public int CQZ { get; set; }
        public int ITUZ { get; set; }
        public string Continent { get; set; }
        public double Lattitude { get; set; }
        public double Longitude { get; set; }
        public double UTCOffset { get; set; }
        public string PrimaryDXCCPfx { get; set; }  // Original E5/n
        public string DXCCPrefix { get; set; }  // DXKeeper standard  E5-N   ; select dbo.ConvertCTYPrefixToDXCCPrefix( 'E5/n');

        public string CQWW_WAE_Flag { get; set; }  // * as the first character

    }
    public class CTYDataItems {
        public string[] rawItems { get; set; }
        public List<CTYCallPfxInfoBase> AliasPfxList = new List<CTYCallPfxInfoBase>();
        public List<CTYCallPfxInfoBase> CallSignList = new List<CTYCallPfxInfoBase>();
        public void ParseDataLine(string dataLine , CTYDxccInfo parentDXCC ) {

            rawItems=dataLine.Split(',');
    
            List<string> tempUnparsedPfxList = rawItems.Where(x => !x.StartsWith("=")).Select(x => x.Replace("=", "")).ToList<string>();
            List<string> tempUnparsedCallSignList = rawItems.Where(x => x.StartsWith("=")).Select(x => x.Replace("=", "")).ToList<string>();

            ParseOverrides(tempUnparsedPfxList, ref AliasPfxList, parentDXCC);
            ParseOverrides(tempUnparsedCallSignList, ref CallSignList, parentDXCC);
        }

        private void ParseOverrides(List<string> tempUnparsedList, ref List<CTYCallPfxInfoBase>outList,  CTYDxccInfo parentDXCC) {
            foreach (string itemRaw in tempUnparsedList) {
                CTYCallPfxInfoBase info = new CTYCallPfxInfoBase(); // Remove brackets later

                int indexOfAnyDelimiter = itemRaw.IndexOfAny("([<~".ToCharArray());

                if (indexOfAnyDelimiter!=-1) { // The raw call contains brackets/delimiters
                    info.PrimaryDXCCPfx=parentDXCC.PrimaryDXCCPfx;
                    info.Code=itemRaw.Substring(0, indexOfAnyDelimiter);

                    info.CQZOverride=Util.GetSubstringByStringInt(itemRaw, "(", ")");
                    info.ITUZOverride=Util.GetSubstringByStringInt(itemRaw, "[", "]");
                    info.ContinentOverride=Util.GetSubstringByString(itemRaw, "{", "}");
                    string latLongOverride = Util.GetSubstringByString(itemRaw, "<", ">");
                    string[] arrLatLong = latLongOverride.Split('/');
                    if (arrLatLong.Length==2) {
                        double.TryParse(arrLatLong[0], out info.LattitudeOverride);
                        double.TryParse(arrLatLong[1], out info.LattitudeOverride);
                    }
                    info.LocalTimeOffsetOverride=Util.GetSubstringByStringDouble(itemRaw, "~", "~");

                } else {
                    info.Code=itemRaw; // No overrides /delimiters
                }
                outList.Add(info);
            }
        }
    }

    public class CTYItem {
        public int ItemID { get; set; }
        public string mainLine { get; set; }
        public string dataLine { get; set; }
        public CTYDxccInfo DxccInfo { get; set; }
        public CTYDataItems DataItems { get; set; }

        public RetCode ParseDXCCInfo() {
            RetCode ret = new RetCode();
            string cqww_wae_flag = "";
            string[] dxccItems = mainLine.Split(':').Select(x => x.Trim()).ToArray<string>();
            string primaryPfx = dxccItems[7];
            if(primaryPfx .StartsWith("*")) {
                primaryPfx=primaryPfx.Replace("*", "");
                cqww_wae_flag="*";
            }
           
            DxccInfo=new CTYDxccInfo { CountryName=dxccItems[0], CQZ=int.Parse(dxccItems[1]), ITUZ=int.Parse(dxccItems[2]), Continent=dxccItems[3], Lattitude=double.Parse(dxccItems[4]), Longitude=double.Parse(dxccItems[5]), UTCOffset=double.Parse(dxccItems[6]), PrimaryDXCCPfx=primaryPfx, CQWW_WAE_Flag=cqww_wae_flag };

            DataItems=new CTYDataItems();
            DataItems.ParseDataLine(dataLine, DxccInfo);
            return ret;
        }
    }


    public class BigCTYDat:DataETLBase {
        List<CTYItem> CTYItems = new List<CTYItem>();
        public List<CTYDXCCPrefixOverride> CTYDXCCPrefixOverride = new List<CTYDXCCPrefixOverride>();
        public List<vCTYCallPfxOverride> vCTYCallPfxOverrideList = new List<vCTYCallPfxOverride>();
        private Dictionary<string,vCTYCallPfxOverride> _exactCallLookup;
        private Dictionary<string,CTYDXCCPrefixOverride> _prefixOverrideLookup;
        private static readonly HashSet<string> ExcludedCallParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)            { "P", "QRP", "M", "MM", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "B" };

        string[] ctyLines = new string[0];
        public BigCTYDat(string connString) : base(connString) {
            _exactCallLookup= new Dictionary<string,vCTYCallPfxOverride>();
            _prefixOverrideLookup= new Dictionary<string,CTYDXCCPrefixOverride>();

        }
     
        public RetCode ProcessFile(FileInfo fi) {
            RetCode ret = new RetCode();
            ret=ParseFile(fi);

            foreach (CTYItem item in CTYItems) {
                item.ParseDXCCInfo();
            }
            return ret;
        }
        public List<CTYItem> GetCTYItems() {
            return CTYItems;    
        }
        public void ParseCTY_DATandLoad() {
            FileInfo fi = new FileInfo($@"{InputFileLocation}\cty.dat");
            ProcessFile(fi);
            TestStatistics();
            List<CTYItem> CtyItems = GetCTYItems();
            CTYItemsInsertToDB(CtyItems);
            LoadvCTYCallPfxOverrideData(); // reload to list
            BuildLookupIndexes();
        }
      
        public RetCode CTYItemsInsertToDB(List<CTYItem> CtyItems) {
            RetCode ret = new RetCode();
            string sql = "";
            dBManager.ExecuteNonQuery(CommandType.Text, "truncate table QSOLOG..CTYCallPfxOverride;"); // Child
            dBManager.ExecuteNonQuery(CommandType.Text, "truncate table QSOLOG..CTYDxccInfo;"); // parent
            foreach (CTYItem item in CtyItems) {
                string country = item.DxccInfo.CountryName;
                if (country.Contains("'")) {
                    country=country.Replace("'", "''");
                }
                sql=$"INSERT INTO QSOLOG..CTYDxccInfo(BigCtySeq, CountryName, PrimaryDXCCPfx, CQZ, ITUZ, Continent, Lattitude, Longitude, UTCOffset, CQWW_WAE_Flag)";
                sql+=$" SELECT {item.ItemID},  '{country}','{item.DxccInfo.PrimaryDXCCPfx}',{item.DxccInfo.CQZ},{item.DxccInfo.ITUZ},'{item.DxccInfo.Continent}',{item.DxccInfo.Lattitude},{item.DxccInfo.Longitude},{item.DxccInfo.UTCOffset},'{item.DxccInfo.CQWW_WAE_Flag}' ";
                dBManager.ExecuteNonQuery(CommandType.Text, sql);

                foreach (CTYCallPfxInfoBase x in item.DataItems.AliasPfxList) {
                    sql=$"INSERT INTO QSOLOG..CTYCallPfxOverride(BigCtySeq, PrimaryDXCCPfx, OvrType, Code, CQZOverride, ITUZOverride, ContinentOverride, LattitudeOverride, LongitudeOverride, LocalTimeOffsetOverride)";
                    sql+=$" SELECT {item.ItemID},  '{item.DxccInfo.PrimaryDXCCPfx}','PFX','{x.Code}',{x.CQZOverride},{x.ITUZOverride},'{x.ContinentOverride}',{x.LattitudeOverride},{x.LongitudeOverride},{x.LocalTimeOffsetOverride}";
                    dBManager.ExecuteNonQuery(CommandType.Text, sql);
                }

                foreach (CTYCallPfxInfoBase x in item.DataItems.CallSignList) {
                    sql=$"INSERT INTO QSOLOG.. CTYCallPfxOverride(BigCtySeq, PrimaryDXCCPfx, OvrType, Code, CQZOverride, ITUZOverride, ContinentOverride, LattitudeOverride, LongitudeOverride, LocalTimeOffsetOverride)";
                    sql+=$" SELECT {item.ItemID},  '{item.DxccInfo.PrimaryDXCCPfx}','CALL','{x.Code}',{x.CQZOverride},{x.ITUZOverride},'{x.ContinentOverride}',{x.LattitudeOverride},{x.LongitudeOverride},{x.LocalTimeOffsetOverride}";
                    dBManager.ExecuteNonQuery(CommandType.Text, sql);
                }
            }
            return ret;
        }
        public bool IsDataLoaded() {
            bool loaded = vCTYCallPfxOverrideList != null && vCTYCallPfxOverrideList.Count > 0
                          && CTYDXCCPrefixOverride != null && CTYDXCCPrefixOverride.Count > 0
                          && _exactCallLookup != null && _exactCallLookup.Count >= 0
                          && _prefixOverrideLookup != null && _prefixOverrideLookup.Count >= 0;

            System.Diagnostics.Debug.WriteLine($"[BigCTYDat] Data loaded: {loaded} | vCTYCallPfxOverrideList: {vCTYCallPfxOverrideList?.Count ?? 0} | Overrides: {CTYDXCCPrefixOverride?.Count ?? 0}");
            return loaded;
        }


        public void BuildLookupIndexes() {
            _exactCallLookup = vCTYCallPfxOverrideList
                .Where(x => x.OvrType == "CALL")
                .ToDictionary(x => x.Code,x => x,StringComparer.OrdinalIgnoreCase);

            _prefixOverrideLookup = CTYDXCCPrefixOverride
                .ToDictionary(x => x.CTYDXCCPrefix,x => x,StringComparer.OrdinalIgnoreCase);
        }

        public RetCode LoadvCTYCallPfxOverrideData() {
            RetCode ret = new RetCode();
            string sql = "select * from QSOLOG..vCTYCallPfxOverride";
            vCTYCallPfxOverrideList=(List<vCTYCallPfxOverride>)LoadSQLToListOfObjects<vCTYCallPfxOverride>(sql, ref ret);
            return ret;
        }

        public RetCode LoadCTYDXCCPrefixOverride() {
            RetCode ret = new RetCode();
            string sql = "select * from QSOLOG..CTYDXCCPrefixOverride";
            CTYDXCCPrefixOverride=(List<CTYDXCCPrefixOverride>)LoadSQLToListOfObjects<CTYDXCCPrefixOverride>(sql, ref ret);
            return ret;
        }
        public RetCode LoadUpdCallFromCTY(string call, ref List<vCTYCallPfxOverride> att) {
            RetCode ret = new RetCode();
            DataTable dt = null;

            dBManager.CreateParameters(3); //   
            dBManager.AddParameters(0, "@call", $"{call}");
            dBManager.AddParameters(1, "@Origcall", $"{call}");
            dBManager.AddParameters(2, "@action", $"RETURN_SINGLE_ROW");
            dt=dBManager.ExecuteDataTable(System.Data.CommandType.StoredProcedure, "QSOLOG..UpdCallFromCTY");
            if ( dt==null ||dt.Rows.Count==0) {
                att=new List<vCTYCallPfxOverride>();//blank list
                return new RetCode { Code=-1, Msg=$"Invalid Call {call}" };
            }
            att=        ConvertDatatabletoListofT<vCTYCallPfxOverride>(dt);
            return ret;
        }

        public RetCode AddBIGCTYAttributes(ref StationInfo stnInfo) {
            string stnCall = stnInfo.CALL;

            // ✅ O(1) dictionary lookup instead of O(n) LINQ
            if(_exactCallLookup.TryGetValue(stnCall,out var exactMatch)) {
                UpdateStationInfo(exactMatch,ref stnInfo);
                return new RetCode { Code = 1,Msg = $"Exact call [{stnCall}] match in vCTYCallPfxOverride" };
            }

            // ✅ Sort once and cache
            string[] callParts = stnCall.Split('/');
            Array.Sort(callParts,(a,b) => a.Length.CompareTo(b.Length));

            foreach(string callPart in callParts) {
                // ✅ O(1) HashSet lookup
                if(ExcludedCallParts.Contains(callPart)) {
                    continue;
                }

                vCTYCallPfxOverride foundMatch = FindPFXvCTYCallPfxOverride(callPart);
                if(foundMatch.DXCCPrefix.Length > 0) {
                    // ✅ O(1) dictionary lookup
                    if(_prefixOverrideLookup.TryGetValue(foundMatch.DXCCPrefix,out var ctyOverride)) {
                        foundMatch = FindPFXvCTYCallPfxOverride(ctyOverride.DXCCPrefix);
                    }

                    UpdateStationInfo(foundMatch,ref stnInfo);
                    return new RetCode { Code = 1,Msg = $"Prefix match for [{stnCall}]" };
                }
            }

            return new RetCode { Code = 0,Msg = $"No match found for [{stnCall}]" };
        }




        private void UpdateStationInfo(vCTYCallPfxOverride matched, ref StationInfo stInfo) {  // copies BigCTY attributes to StationInfo
            stInfo.Country=matched.CountryName;
            stInfo.CONT=matched.ContinentOvr;
            stInfo.CQZ=matched.CQZOvr;
            stInfo.ITUZ=matched.ITUZOvr;
            stInfo.LAT=matched.LattitudeOvr;
            stInfo.LON=matched.LongitudeOvr;
            stInfo.UTC_SHIFT=matched.UTCOffsetOvr;
            // Special attributes
            stInfo.BigCTYSeq=matched.BigCtySeq;
            stInfo.DXCCPrefix=matched.DXCCPrefix;                 //matched.PrimaryDXCCPfx;
            stInfo.OverrideType=matched.OvrType;
            stInfo.CQWW_WAE_Flag=matched.CQWW_WAE_Flag;
            stInfo.CountryInfoSource="BIGCTY_VIEW";
        }

        private vCTYCallPfxOverride FindPFXvCTYCallPfxOverride(string callPart) { // Match part of a call to CTY dat.
            vCTYCallPfxOverride FoundMatch = new vCTYCallPfxOverride();
            List<vCTYCallPfxOverride> matchPFXLoop = new List<vCTYCallPfxOverride>();
            for (int index = callPart.Length; index>-1; index--) {
                string callSub = callPart.Substring(0, index);
                matchPFXLoop=vCTYCallPfxOverrideList.Where(x => x.OvrType=="PFX"&&callSub==x.Code).ToList<vCTYCallPfxOverride>();
                if (matchPFXLoop.Count>0) {
                    FoundMatch=matchPFXLoop[0];
                    break;
                }
            }
            return FoundMatch;
        }




        private RetCode ParseFile(FileInfo fi) {
            RetCode ret = new RetCode();
            ctyLines=File.ReadAllLines(fi.FullName);

            CTYItems=new List<CTYItem>();
            CTYItem item = null;
            string curMainLine = "";
            string dataLine = "";
            int entityCounter = 0;

            foreach (string ctyLine in ctyLines) {

                if (entityCounter==0) { //start
                    curMainLine=ctyLine;
                    entityCounter++;
                    continue;
                }
                if (ctyLine.Substring(0, 1).Trim()!=string.Empty) { //starting line with DXCCName
                    if (ctyLine!=curMainLine) {
                        item=new CTYItem { mainLine=curMainLine, dataLine=dataLine.Replace(";", ""), ItemID=entityCounter }; // remove trailing ;
                        CTYItems.Add(item);
                        curMainLine=ctyLine;
                        dataLine="";
                        entityCounter++;
                    }
                } else { // DATA line
                    dataLine+=ctyLine.Trim();
                }
            }
            item=new CTYItem { mainLine=curMainLine, dataLine=dataLine, ItemID=entityCounter }; // Add the last item
            CTYItems.Add(item);
            return ret;
        }
        public void TestStatistics() {

            List<CTYItem> CQZOverride = CTYItems.Where(x => x.DataItems.CallSignList.Where(c => c.CQZOverride>0).Count()>0).ToList<CTYItem>();
            List<CTYItem> ITUZOverride = CTYItems.Where(x => x.DataItems.CallSignList.Where(c => c.ITUZOverride>0).Count()>0).ToList<CTYItem>();
            List<CTYItem> LattitudeOverride = CTYItems.Where(x => x.DataItems.CallSignList.Where(c => c.LattitudeOverride>0.0).Count()>0).ToList<CTYItem>();
            List<CTYItem> LongitudeOverride = CTYItems.Where(x => x.DataItems.CallSignList.Where(c => c.LongitudeOverride>0.0).Count()>0).ToList<CTYItem>();
            List<CTYItem> ContinentOverride = CTYItems.Where(x => x.DataItems.CallSignList.Where(c => c.ContinentOverride.Length>  0).Count()>0).ToList<CTYItem>();
            List<CTYItem> LocalTimeOffsetOverride = CTYItems.Where(x => x.DataItems.CallSignList.Where(c => c.LocalTimeOffsetOverride>0.0).Count()>0).ToList<CTYItem>();

            CTYItem Chile = CTYItems.Where(x => x.DxccInfo.CountryName=="Chile").FirstOrDefault();
            CTYItem Antarctica = CTYItems.Where(x => x.DxccInfo.CountryName=="Antarctica").FirstOrDefault();
            CTYItem China = CTYItems.Where(x => x.DxccInfo.CountryName=="China").FirstOrDefault();

        }
    }
}
