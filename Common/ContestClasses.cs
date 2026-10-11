using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static Common.RegionMultiContainer;
using static System.Net.WebRequestMethods;

namespace Common {
    public class ContinentMultiplier {
        public string Continent { get; set; }
    }


    public enum ContestEnum {  // STORED IN C:\Users\Sinisa\AppData\Local\WinFormsLog\WinFormsLog.exe_Url_pz5lr05a0fi32gu3amdgs4njt0hxnbac\1.0.0.0
        IGNORE,

        CQ_WW_CW, ARRL_DX_CW, ARRL_DX_SSB, ARRL_SS_CW, ARRL_SS_SSB,ARRL_FD,
        CQ_WW_SSB,CQ_VHF_SSBCW, CQ_160_CW,
        CQ_WPX_CW,
        CQ_WPX_SSB,
        ON_QSO_PARTY,
        RSGB_COMMONWEALTH, // BERU contest no multis
        ALL_AFRICA,  // Complex multis, ignore
        GC,//Yuri Gagarin International DX Contest CW
        LZ_DX,
        OK_OM_DX_CW,
        PCC,  // YO - PRO CW Contest. 3 Dec.
        CANADA_WINTER,

        ARRL_10, // Complex multipliers DXCC and regions,
        ARRL_160,
        YOTA,   // Multi: Age once per band, Work: Once band/mode   1- Apr/May  2- July 3 - Dec
        ARI_DX, // Italian - CW/SSB provinces
        UKEI_DX_CW, UKEI_DX_SSB, // various dates

        MMC_HF_CW, //Marconi Memorial
        SMMC, //Samuel Morse Memorial Contest  
        YB_DX_CONTEST, // SSB YB prefixes
        STEW_PERRY,//160m CW
        HA_DX,
        WIA_AUSTRALIADAY,
        REF_CW,  //French/Corsica departments once per band.          French overseas prefixes once per band.  non-French DXCC countries once per band (available only to French stations)
        EUDXC,
        PACC,
        UBA_DX_CW,   // ON contest
        SA10M, // March 11 SA 10m only. Multis CQZ, SA Pfx
        TESLA_HF,    // March 11 Serial + square , No mUltis
        RDXC, //UA oblast + dxccc
        JIDX_CW,  // JIDX-CW  
        YUDX, // PFX/DXCC
        CQMMDX, //SA contest April
        HELVETIA, // HB CONTEST
        CQ_M,  //Russian contest May
        EA_MAJESTAD_CW,//His Maj. King of Spain Contest, CW
        UN_DX,// Kazakhstan CW/SSB
        RUS_WW_MM,  //Russian contest Multi Mode 
                    //        NOCONTEST,
        TISZA_CUP, // HG, June 1, CW/ CQZ & PFX of Tisza countries.
        
        IARU_FD_R1_DARC_CW,
        IARU_HF,
        AADX_CW,   //All Asian DX Contest, CW 
        AADX_SSB,
        CANADA_DAY,
        DARC_WAEDC_CW, //M7/W4
        RSGB_IOTA,
        NAQP_CW,
        CVA_DX_CW,
        YO_DX_HF,
        FOCBWQP, // FOC QSO Party activity  Bill Windle QSO Party
        CWOPS_OPEN,
        SAC_CW,  // Scandinavian Activity Contest
        ME_QSO_PARTY,
        TRC_DX,// LZ contest
        OCEANIA_DX_CW,
        WAG,
        RAEM,
        PMC,  //Each PMC once per mode per band. January
        QRS_10, // QRS CW Marathon, Brazil, Monthly. First Sunday 19-23 UTC
        WWPDX, //World Wide Patagonia DX Contest  
        SPDX,
        PORTUGAL_DAY,
        GACW,  // Argentina
        ARRL_VHF_JUN,
        LABRE_DX, // Brasil 48h 3rd week of July
        CRO_9A_DX,   // Dec 3rd week
        VENEZUELA_IND_DAY, //July
        // QSO PARTIES
        FCG_FQP, // Florida QSO Party 600Z, Apr 26 to 0159Z, Apr 27 and 1200Z-2159Z, Apr 27, 2025
        RDAC,  //Russian District Award Contest August 2W   Serial # -- non-RU Stations: Each RDA district once only
        HAM_SPIRIT //  CW/SSB SSB contest probably does not exist,

    }
    public class Schedule { // when a contest is scheduled
        public int Month { get; set; }
        public int Week { get; set; }
        public int MonthWeekNum { get { return 10*Month+Week; } } // M1, W3 = 13  M11, W 3 =113
        public int ExpiryHours = 7*24;
    }
    public enum MultiType {
        PFX,
        PFX_NUMBER,  // Number 0-9 within any DXCC regardless of prefix
        PFX_CONTINENT,  //Only PFX on the list of continents
        DXCC,
        CQZ,
        ITUZ,
        //        REGION_EXCLUSIVE
        SPECIAL_CALLS,
        SRX, // Simple SRX code YOTA, not DXCC specific HAM-SPIRIT  4FM   4FN  
        SRX2, // IOTA optional
        REGION, // DXCC list or SRX as source
        SRX_REGION, // SRX is region/county which is not on the excluded list (K/VE states/prov)
        REGION_LIST, // SRX is region/county which is not on the excluded list and which is on included list VE/K Prov/States

        WAG, // Special for Worked All Germany
        DEFAULT,
        IGNORE
    }
    public enum MatchingMode {
        LIST, // A list of allowed values  Region : ON, QC...
        SRX   // Any unique value from SRX is fine.
    }
    public class ContestMultiplier {
        public QSOItemCodes CodeSrc { get; set; } // SRX, PFX,DXCC - Other unique code
        public MultiType MtType { get; set; }
        public MatchingMode MatchingMode = MatchingMode.SRX; //Default
        public string CodeDescription { get; set; } // District/State/Region/
        public string Label {
            get {
                if(CodeDescription.Length==0) {
                    return MtType.ToString();
                } else {
                    return CodeDescription;
                }
            }
        }
        public bool Band { get; set; }  // Is it "Per Band?"
        public bool Mode { get; set; }  // Is it "Per Mode?"
        //consolidate these

        public List<DXCCPrefixMultiplier> DXCCPrefixMultipliers { get; set; }  //
        public List<DXCCPrefixMultiplier> ExcludedDXCCPrefixMultipliers { get; set; }  // Excluded DXCC for MtType DXCC/ITUZ
        public List<ContinentMultiplier> ContinentMultipliers { get; set; }
        public RegionMultiContainer RegMultiContainer { get; set; }
        public RegionMultiContainer ExcludedRegContainer { get; set; }


        //public List<RegionMultiContainer> ExcludedRegionMultipliers { get; set; }
        public bool matchedDXCC = false;
        public bool excludedDXCC = false;
        public bool matchedRegion = false; // IF SRX is on the list of regions
        public bool matchedDXCCRegion = false; //IF DXCC applies to list of regions
        public bool excludedRegion = false;
        public bool matched = false;
        public bool applicable = true;
        public bool multiAllowed = true;
        public bool valueApplicableForPFX = true; // For PFX matching. If the value is applicable for PFX (YODX counties are grouped by PFX YO2, YO3)
        public string displayValue = ""; // Appropriate value used for matching. SRX, DXCCPrefix, other values
        public List<SpecialCallMultiplier> SpecialCallMultipliers { get; set; }

        public ContestMultiplier() {
            MtType=MultiType.DEFAULT;
            CodeSrc=QSOItemCodes.SRX;
            CodeDescription="";
            Band=false;
            Mode=false;
            DXCCPrefixMultipliers=new List<DXCCPrefixMultiplier>();
            ExcludedDXCCPrefixMultipliers=new List<DXCCPrefixMultiplier>();

            RegMultiContainer=new RegionMultiContainer();
            ExcludedRegContainer=new RegionMultiContainer();
            ContinentMultipliers=new List<ContinentMultiplier>();
        }
        public ContestMultiplier(MultiType type_) {
            MtType=type_;
            if(type_==MultiType.DEFAULT) {
                CodeSrc=QSOItemCodes.SRX;
            } else {
                CodeSrc=QSOItemCodes.DEFAULT;// Set explicitly
            }
            CodeDescription="";
            Band=false;
            Mode=false;
            DXCCPrefixMultipliers=new List<DXCCPrefixMultiplier>();
        }
        public void SetMultiplierState(QSOInfo qsoInfo) {

            // Set multiplier state
            matched = false;
            valueApplicableForPFX = true;
            matchedDXCC = IsStationOnFilteredDXCCList(qsoInfo.stnInfo.DXCCPrefix);
            excludedDXCC = IsStationOnExcludedDXCCList(qsoInfo.stnInfo.DXCCPrefix);
            matchedRegion = IsRegionOnFilteredRegionList(qsoInfo,ref matchedDXCCRegion);
            excludedRegion = IsRegionOnExcludedRegionList(qsoInfo.SRX);
            applicable = !excludedDXCC && matchedDXCC;
            multiAllowed = true;
        }
        //  SEE IF THIS CAN BE FIXED   allDXCCRegions=multiList[0].RegionMultiContainerList.SelectMany(x => x.DXCCRegionList).ToList<DXCCRegions>().SelectMany(y => y.Regions).ToList<Region>();


        public List<Region> GetRegionsFlat() { // Flatttens all regions as DXCC/Code mappings for all DXCC. Use until a better solution is found
            List<Region> allDXCCRegions = RegMultiContainer.DXCCRegionList.SelectMany(y => y.Regions).ToList<Region>();
            return allDXCCRegions;
        }

        public bool IsStationOnFilteredDXCCList(string dxccPfx) {
            var allDXCCRegions = GetRegionsFlat();
            if(allDXCCRegions.Count == 0) return true;
            return allDXCCRegions.Any(x => x.DXCCPrefix == dxccPfx);
        }

        public bool IsStationOnExcludedDXCCList(string dxccPfx) {
            if(ExcludedDXCCPrefixMultipliers.Count == 0) return false;
            return ExcludedDXCCPrefixMultipliers.Any(x => x.DXCCPrefix == dxccPfx);
        }

        /// <summary>
        /// Check if region is on filtered list
        /// </summary>
        public bool IsRegionOnFilteredRegionList(QSOInfo qsoInfo,ref bool matchedDXCCRegion) {
            if(DXCCPrefixMultipliers.Count == 0) {
                matchedDXCCRegion = true;
                return true;
            }

            bool hasDXCC = DXCCPrefixMultipliers.Any(x => x.DXCCPrefix == qsoInfo.stnInfo.DXCCPrefix);
            matchedDXCCRegion = hasDXCC;
            if(!hasDXCC) return false;

            if(MatchingMode == MatchingMode.SRX) return true;
            if(MatchingMode == MatchingMode.LIST) {
                Region reg = null;
                DXCCRegions regs = RegMultiContainer.DXCCRegionList.Where(x => x.DXCC.DXCCPrefix==qsoInfo.stnInfo.DXCCPrefix).FirstOrDefault();// find the region list for the DXCC
                if(regs!=null) {
                  reg=   regs.Regions.Where(x => x.RegionCode== qsoInfo.SRX).FirstOrDefault();
                }
                return reg!=null;
            }
            return false;
        }

        /// <summary>
        /// Check if region is on excluded list
        /// </summary>
        public bool IsRegionOnExcludedRegionList(string region) {

            // return true; // fix this
            DXCCRegions regs = ExcludedRegContainer.DXCCRegionList.Where(x => x.DXCC.DXCCPrefix==region).FirstOrDefault();// 
            if(regs!=null) {
                return true;
            }
            //.

            //DXCCRegions reg = null;
            //foreach(RegionMultiContainer regCont in RegionMultiContainerList) {
            //    reg = ExcludedRegionMultipliers.GetDXCCRegionForDXCCPrefix(dxccPfx);



            //    DXCCRegions regs = ExcludedRegionMultipliers.DXCCRegionList.SelectAny()
            //if(ExcludedRegionMultipliers.DXCCRegionList.Count == 0) return false;
            //    return ExcludedRegionMultipliers.DXCCRegionList.Any(x => x.Regions. == region);
            //}
            return false;
        }
        public DXCCRegions GetRegionsForDxcc(string dxccPfx) {
            DXCCRegions reg = null;
                reg = RegMultiContainer.GetDXCCRegionForDXCCPrefix(dxccPfx);
            return reg;

        }
        public Region GetRegionForDxccAndRegionCode(string dxccPfx,string regionCode) {
            DXCCRegions regs = GetRegionsForDxcc(dxccPfx);
            Region reg = null;
            if(regs!=null) {
                reg=    regs.Regions.Where(x => x.RegionCode==regionCode).FirstOrDefault();
            }

            return reg;
        }
    }
    public class SpecialCallMultiplier {
        public string Call { get; set; }
        public string Description { get; set; }
        public string Code { get; set; }

        public static List<SpecialCallMultiplier> GetSpecialCallMultipliers(ContestEnum contest) {
            //https://www.ng3k.com/Dxcc/dxccsa.html    use Specifications Excel to build lines.

            List<SpecialCallMultiplier> specialCallMultis = new List<SpecialCallMultiplier>();
            switch(contest) {
            case ContestEnum.GC: //Yuri Gagarin
                specialCallMultis.Add(new SpecialCallMultiplier { Call="RJ1O",Description="Plesetsk Cosmodrome",Code="KP" });
                specialCallMultis.Add(new SpecialCallMultiplier { Call="RT2C",Description="Mission Control Center",Code="CU" });
                specialCallMultis.Add(new SpecialCallMultiplier { Call="RT3F",Description="Cosmonaut Training Center",Code="CP" });
                specialCallMultis.Add(new SpecialCallMultiplier { Call="R5AG",Description="Aerospace Laboratory",Code="AL" });
                specialCallMultis.Add(new SpecialCallMultiplier { Call="UA5L",Description="The birthplace of Yuri Gagarin",Code="RG" });
                specialCallMultis.Add(new SpecialCallMultiplier { Call="R3LA",Description="The birthplace of Yuri Gagarin",Code="YG" });
                specialCallMultis.Add(new SpecialCallMultiplier { Call="RT4D",Description="The landing place of Yuri Gagarin",Code="MP" });
                specialCallMultis.Add(new SpecialCallMultiplier { Call="RG2X",Description="The birthplace of Russian Cosmonautics",Code="RK" });
                specialCallMultis.Add(new SpecialCallMultiplier { Call="RC3XC",Description="Laboratory of Aerospace",Code="LA" });
                specialCallMultis.Add(new SpecialCallMultiplier { Call="RW0A",Description="Reshetnev Siberian State University of Science and Technology",Code="SA" });
                specialCallMultis.Add(new SpecialCallMultiplier { Call="RW0J",Description="Vostochny Cosmodrome",Code="KV" });
                specialCallMultis.Add(new SpecialCallMultiplier { Call="R2VA",Description="Parashute Design Research Institute",Code="PC" });
                specialCallMultis.Add(new SpecialCallMultiplier { Call="U4MIR",Description="Cosmonaut Alexander Volkov",Code="CV" });
                specialCallMultis.Add(new SpecialCallMultiplier { Call="R8TT",Description="Orenburg Aviation School",Code="AU" });
                specialCallMultis.Add(new SpecialCallMultiplier { Call="RZ0SSS",Description="International Space Station",Code="SS" });
                break;
            case ContestEnum.EA_MAJESTAD_CW: //His Maj. King of Spain Contest. Special station  
                specialCallMultis.Add(new SpecialCallMultiplier { Call="EF0F",Description="Special station His Majesty The King of Spain",Code="SMR" });
                break;

            case ContestEnum.IARU_HF: //IARY July   multis: https://www.dropbox.com/s/g0wojif6zo0ffu5/HQ2022.txt?dl=0

                string[] calls = new string[] { "3V8CB","4K7Z","4L0HQ","4O1HQ","4V0HQ","4X4ARC","5C8A","5Z4RS","7A1HQ","7X2HQ","8N1HQ","8N2HQ","8N6HQ","8N7HQ","8N8HQ","8N9HQ","9A0HQ","9H1MRL","9K9HQ","9M2A","9M2IR","9V9HQ","9Y4HQ","9Y4T","A25HQ","A47RS","A71HQ","AT1HQ","B1HQ","B2HQ","B4HQ","B6HQ","B7HQ","BV0HQ","C4HQ","C91HQ","CB1HQ","CR5HQ","CX1AA","DA0HQ","DJ3HW","DX0HQ","E2HQ","E7HQ","EF4HQ","EI0HQ","EK0HQ","EL2BEN","EM5HQ","ER7HQ","ES9A","ET3AA","EW5HQ","EX9HQ","F4GKR","GR2HQ","HB0HQ","HB9HQ","HB9JOE","HC0T","HG0HQ","HI3RCD","HK3LR","HL0HQ","HP0HQ","HQ2W","HS1FVL","HU0YS","IO1HQ","IO2HQ","IO5HQ","IO6HQ","IO8HQ","IO9HQ","IV3KKW","J73Z","J87HQ","JA1CJP","JH1NBN","JT1KAA","L21RCA","LA2RR","LN2HQ","LU1BCE","LX8HQ","LZ0HQ","NU1AW/5","OA4O","OE0HQ","OH0HQ","OH2HQ","OM8HQ","OP0HQ","OY1CT","OZ1HQ","P40HQ","PA6HQ","PB2T","PJ2HQ","PT2ADM","PX2HQ","R2HQ","R3HQ","R4HQ","R6HQ","R7HQ","S50HQ","S77SARA","SK9HQ","SM6EAN","SN0HQ","SU1SK","SX0HQ","T40HQ","T70HQ","TC3HQ","TF3HQ","TG0AA","TI0HQ","TM0HQ","TU2CI","UN1HQ","V31HQ","V55HQ","V8FEO","VA2RAC","VE3YV","VE6SH/7","VK3TX","VP2M","VP2V","VP9HQ","VR2HK","W1AW/3","W5ZN","W6ROD","XE1KK","XE1LM","XR3HQ","XV4Y","XX9A","YD1PRY","YL4HQ","YR0HQ","YS0YS","YS1MS","YT0HQ","YV5AJ","Z30HQ","Z32TO","Z60A","ZA1A","ZF1A","ZL2TLL","ZL6HQ","ZP5AA","ZS4BS","ZS9HQ","W1AW","VE6SH","NU1AW" };
                string[] codes = new string[] { "ARAT","FRS","NARG","MARP","RCH","IARC","ARRAM","ARSK","ORARI","ARA","JARL","JARL","JARL","JARL","JARL","JARL","HRS","MARL","KARS","MARTS","R3","SARTS","TTARS","R2","BARS","ROARS","QARS","ARSI","CRAC","CRAC","CRAC","CRAC","CRAC","CTARL","CARS","LREM","RCCH","REP","RCU","DARC","R1","PARA","RAST","BA","URE","IRTS","FRRA","LRAA","UARL","ARM","ERAU","EARS","BFRR","ARUKR","R1","RSGB","AFVL","USKA","R1","GRC","MRASZ","RCD","LCRA","KARL","LPRA","RCH","R3","CRAS","ARI","ARI","ARI","ARI","ARI","ARI","R1","DARCI","SVGRS","R3","R3","MRSF","RCA","AC","NRRL","R2","RL","BFRA","IARU","RCP","OV","SRAL","SRAL","SARA","UBA","FRA","EDR","AARC","VERON","R1","VRONA","R2","LABRE","SRR","SRR","SRR","SRR","SRR","ZRS","SARA","SSA","R1","PZK","ERASD","RAAG","FRC","ARRSM","TRAC","IRA","CRAG","RCCR","REF","ARAI","KFRR","BARC","NARL","BDARA","RAC","R2","AC","R3","MARS","BVIRL","RSB","HARTS","ARRL","AC","R2","R2","FMRE","RCCH","VARC","ARM","R3","LRAL","FRR","CRAS","R2","SRS","RCV","RSM","R1","SHRAK","AARA","CARS","R3","NZART","RCP","R1","SARL","ARRL","AC","IARU" };

                for(int seq = 0;seq<calls.Length;seq++) {
                    specialCallMultis.Add(new SpecialCallMultiplier { Call=calls[seq],Code=codes[seq] });
                }

                break;
            }
            return specialCallMultis;
        }
    }
    public class Region {

        public string DXCCPrefix { get; set; }
        public string PFX { get; set; }  // Call PFX. Some contests (YO) have regions organised by PFX
        public string Area { get; set; } // for arrl US areas 0-9, canada Area Canada
        public string RegionName { get; set; }
        public string RegionCode { get; set; }
        //       public string Prefixes { get; set; }
        public Region() { DXCCPrefix=""; PFX=""; RegionName=""; RegionCode=""; }
    }


    public class DXCC {
        public string DXCCPrefix { get; set; }
        public string DXCCName { get; set; }
        public DXCC(string dxccPrefix) {
            DXCCPrefix=dxccPrefix;
        }

    }
    /*

   



    */

    public class DXCCRegions {
        public DXCC DXCC { get; set; }
        public List<Region> Regions { get; set; }
        public string RegionLabel { get; set; }
        public DXCCRegions(string DXCCPrefix_,string RegionLabel_) {

            DXCC=new DXCC(DXCCPrefix_);  // Read from DB
            RegionLabel=RegionLabel_;
            Regions=MultiWrapper.GetRegionMultipliersByDXCC(DXCCPrefix_);  //    List<Region>();
        }
        public DXCCRegions() {
            Regions=new List<Region>();
        }




    }

    public enum RegionMultiContainerType {
        DXCC,
        CONTEST, // Contest specific regions
    }

    public class RegionMultiContainer {
        public string RegionCode { get; set; } // used for ExcludedRegContainer
        public string Prefixes { get; set; }
        public string Comment { get; set; }
        public List<DXCCRegions> DXCCRegionList { get; set; }

        public RegionMultiContainer() {
            DXCCRegionList=new List<DXCCRegions>();
        }


        public void PopulateRegionListByDXCC(List<Tuple<string,string>> dxccPrefixList) {
            List<Region> regions = new List<Region>();

            foreach(Tuple<string,string> tuple in dxccPrefixList) {  // Add regions for each DXCC as separate list
                string dxccPrefix = tuple.Item1;
                string regLabel = tuple.Item2; // Label for the region, e.g. "Prov" or "State"
                List<Region> regionList = MultiWrapper.GetRegionMultipliersByDXCC(dxccPrefix);
                regions.AddRange(regionList);
                DXCCRegions dxccReg = new DXCCRegions { DXCC=new DXCC(dxccPrefix),Regions=regionList,RegionLabel=regLabel };
                this.DXCCRegionList.Add(dxccReg);
            }
        }
        public void PopulateRegionListByContest(ContestEnum contest,string regLabel) {
            List<Region> regions = MultiWrapper.GetRegionMultipliersByContest(contest);
            SplitMultisByDXCC(regLabel,regions);
        }
        public void PopulateRegionListARRLRegionMultipliersByDXCCandSection(string dXCCPrefix,string regLabel) {
            List<Region> regions = MultiWrapper.GetARRLRegionMultipliersByDXCCandSection(dXCCPrefix);
            SplitMultisByDXCC(regLabel,regions);
        }

        private void SplitMultisByDXCC(string regLabel,List<Region> regions) {
            List<string> dxccList = regions.Select(x => x.DXCCPrefix).Distinct().OrderBy(x => x).ToList();
            foreach(string dxcc in dxccList) {  // Add regions for each DXCC as separate list
                List<Region> regionList = regions.Where(x => x.DXCCPrefix==dxcc).ToList();
                DXCCRegions dxccReg = new DXCCRegions { DXCC=new DXCC(dxcc),Regions=regionList,RegionLabel=regLabel };
                this.DXCCRegionList.Add(dxccReg);
            }
        }
        public DXCCRegions GetDXCCRegionForDXCCPrefix(string dxccPfx) {
            return DXCCRegionList.Where(x => x.DXCC.DXCCPrefix==dxccPfx).FirstOrDefault();
        }
    }

    public class DXCCPrefixMultiplier {
        public string DXCCPrefix { get; set; }
        public string Comment { get; set; }
        public DXCCPrefixMultiplier() { // Default. replace this with arguments in Excel and in the code

        }

        public DXCCPrefixMultiplier(string exclDXCC,string exclReason = "") {
            DXCCPrefix=exclDXCC;
            Comment=exclReason;
        }
    }

    public static class MultiWrapper {
        static MultiWrapper() {
        }
        public static List<DXCC> DXCCList { get; set; }
        
        public static List<Region> GetARRLRegionMultipliersByDXCCandSection(string dXCCPrefix)  //  NEW Use for DXCC to region mapping   Use Specifications Excel to create items
         {
            List<Region> Regions = new List<Region>();

            switch(dXCCPrefix) {  // new 2025-08-18 added PFX for YO regions  Extent PFX to List YO8, YP8  YQ6A, YR
            case "VE":
                Regions.Add(new Region { DXCCPrefix="VE",Area="Canada",RegionCode="Alberta ",RegionName="AB" });
                Regions.Add(new Region { DXCCPrefix="VE",Area="Canada",RegionCode="British Columbia ",RegionName="BC" });
                Regions.Add(new Region { DXCCPrefix="VE",Area="Canada",RegionCode="Ontario Golden Horseshoe ",RegionName="GH" });
                Regions.Add(new Region { DXCCPrefix="VE",Area="Canada",RegionCode="Manitoba ",RegionName="MB" });
                Regions.Add(new Region { DXCCPrefix="VE",Area="Canada",RegionCode="New Brunswick ",RegionName="NB" });
                Regions.Add(new Region { DXCCPrefix="VE",Area="Canada",RegionCode="Newfoundland and Labrador ",RegionName="NL" });
                Regions.Add(new Region { DXCCPrefix="VE",Area="Canada",RegionCode="Nova Scotia ",RegionName="NS" });
                Regions.Add(new Region { DXCCPrefix="VE",Area="Canada",RegionCode="Ontario East ",RegionName="ONE" });
                Regions.Add(new Region { DXCCPrefix="VE",Area="Canada",RegionCode="Ontario North ",RegionName="ONN" });
                Regions.Add(new Region { DXCCPrefix="VE",Area="Canada",RegionCode="Ontario South ",RegionName="ONS" });
                Regions.Add(new Region { DXCCPrefix="VE",Area="Canada",RegionCode="Prince Edward Island ",RegionName="PE" });
                Regions.Add(new Region { DXCCPrefix="VE",Area="Canada",RegionCode="Quebec ",RegionName="QC" });
                Regions.Add(new Region { DXCCPrefix="VE",Area="Canada",RegionCode="Saskatchewan ",RegionName="SK" });
                Regions.Add(new Region { DXCCPrefix="VE",Area="Canada",RegionCode="Territories ",RegionName="TER" });

                break;
            case "K":
                Regions.Add(new Region { DXCCPrefix="K",Area="Area0",RegionCode="Colorado ",RegionName="CO" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area0",RegionCode="Iowa ",RegionName="IA" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area0",RegionCode="Kansas ",RegionName="KS" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area0",RegionCode="Minnesota ",RegionName="MN" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area0",RegionCode="Missouri ",RegionName="MO" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area0",RegionCode="North Dakota ",RegionName="ND" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area0",RegionCode="Nebraska ",RegionName="NE" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area0",RegionCode="South Dakota ",RegionName="SD" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area1",RegionCode="Connecticut ",RegionName="CT" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area1",RegionCode="Eastern Massachusetts ",RegionName="EMA" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area1",RegionCode="Maine ",RegionName="ME" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area1",RegionCode="New Hampshire ",RegionName="NH" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area1",RegionCode="Rhode Island ",RegionName="RI" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area1",RegionCode="Vermont ",RegionName="VT" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area1",RegionCode="Western Massachusetts ",RegionName="WMA" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area2",RegionCode="Eastern New York ",RegionName="ENY" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area2",RegionCode="New York City-Long Island ",RegionName="NLI" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area2",RegionCode="Northern New Jersey ",RegionName="NNJ" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area2",RegionCode="Northern New York ",RegionName="NNY" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area2",RegionCode="Southern New Jersey ",RegionName="SNJ" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area2",RegionCode="Western New York ",RegionName="WNY" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area3",RegionCode="Delaware ",RegionName="DE" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area3",RegionCode="Eastern Pennsylvania ",RegionName="EPA" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area3",RegionCode="Maryland-DC ",RegionName="MDC" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area3",RegionCode="Western Pennsylvania ",RegionName="WPA" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area4",RegionCode="Alabama ",RegionName="AL" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area4",RegionCode="Georgia ",RegionName="GA" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area4",RegionCode="Kentucky ",RegionName="KY" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area4",RegionCode="North Carolina ",RegionName="NC" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area4",RegionCode="Northern Florida ",RegionName="NFL" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area4",RegionCode="Puerto Rico ",RegionName="PR" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area4",RegionCode="South Carolina ",RegionName="SC" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area4",RegionCode="Southern Florida ",RegionName="SFL" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area4",RegionCode="Tennessee ",RegionName="TN" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area4",RegionCode="Virginia ",RegionName="VA" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area4",RegionCode="US Virgin Islands ",RegionName="VI" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area4",RegionCode="West Central Florida ",RegionName="WCF" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area5",RegionCode="Arkansas ",RegionName="AR" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area5",RegionCode="Louisiana ",RegionName="LA" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area5",RegionCode="Mississippi ",RegionName="MS" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area5",RegionCode="New Mexico ",RegionName="NM" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area5",RegionCode="North Texas ",RegionName="NTX" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area5",RegionCode="Oklahoma ",RegionName="OK" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area5",RegionCode="South Texas ",RegionName="STX" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area5",RegionCode="West Texas ",RegionName="WTX" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area6",RegionCode="East Bay ",RegionName="EB" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area6",RegionCode="Los Angeles ",RegionName="LAX" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area6",RegionCode="Orange ",RegionName="ORG" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area6",RegionCode="Pacific ",RegionName="PAC" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area6",RegionCode="Santa Barbara ",RegionName="SB" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area6",RegionCode="Santa Clara Valley ",RegionName="SCV" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area6",RegionCode="San Diego ",RegionName="SDG" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area6",RegionCode="San Francisco ",RegionName="SF" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area6",RegionCode="San Joaquin Valley ",RegionName="SJV" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area6",RegionCode="Sacramento Valley ",RegionName="SV" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area7",RegionCode="Alaska ",RegionName="AK" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area7",RegionCode="Arizona ",RegionName="AZ" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area7",RegionCode="Eastern Washington ",RegionName="EWA" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area7",RegionCode="Idaho ",RegionName="ID" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area7",RegionCode="Montana ",RegionName="MT" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area7",RegionCode="Nevada ",RegionName="NV" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area7",RegionCode="Oregon ",RegionName="OR" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area7",RegionCode="Utah ",RegionName="UT" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area7",RegionCode="Western Washington ",RegionName="WWA" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area7",RegionCode="Wyoming ",RegionName="WY" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area8",RegionCode="Michigan ",RegionName="MI" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area8",RegionCode="Ohio ",RegionName="OH" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area8",RegionCode="West Virginia ",RegionName="WV" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area9",RegionCode="Illinois ",RegionName="IL" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area9",RegionCode="Indiana ",RegionName="IN" });
                Regions.Add(new Region { DXCCPrefix="K",Area="Area9",RegionCode="Wisconsin ",RegionName="WI" });

                break;
            case "XE":
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Baja California ",RegionName="BAC" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Baja California Sur ",RegionName="BCS" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Campeche ",RegionName="CAM" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Chiapas ",RegionName="CHI" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Chihuahua ",RegionName="CHH" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Ciudad de México ",RegionName="CMX" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Coahuila ",RegionName="COA" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Colima ",RegionName="COL" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Durango ",RegionName="DGO" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Estado de México ",RegionName="EMX" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Guanajuato ",RegionName="GTO" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Guerrero ",RegionName="GRO" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Hidalgo ",RegionName="HGO" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Jalisco ",RegionName="JAL" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Michoacán ",RegionName="MIC" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Morelos ",RegionName="MOR" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Nayarit ",RegionName="NAY" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Nuevo León ",RegionName="NLE" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Oaxaca ",RegionName="OAX" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Puebla ",RegionName="PUE" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Querétaro ",RegionName="QRO" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Quintana Roo ",RegionName="QUI" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="San Luis Potosí ",RegionName="SLP" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Sinaloa ",RegionName="SIN" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Sonora ",RegionName="SON" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Tabasco ",RegionName="TAB" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Tamaulipas ",RegionName="TAM" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Tlaxcala ",RegionName="TLX" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Veracruz ",RegionName="VER" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Yucatán ",RegionName="YUC" });
                Regions.Add(new Region { DXCCPrefix="XE",Area="Mexico",RegionCode="Zacatecas ",RegionName="ZAC" });

                break;
            }
            return Regions;
        }

        public static List<Region> GetRegionMultipliersByDXCC(string dXCCPrefix)  //  NEW Use for DXCC to region mapping   Use Specifications Excel to create items
          {
            List<Region> Regions = new List<Region>();

            switch(dXCCPrefix) {  // new 2025-08-18 added PFX for YO regions  Extent PFX to List YO8, YP8  YQ6A, YR
            case "YO":
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO2",RegionCode="AR",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO2",RegionCode="CS",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO2",RegionCode="HD",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO2",RegionCode="TM",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO3",RegionCode="BU",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO3",RegionCode="IF",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO4",RegionCode="CT",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO4",RegionCode="BR",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO4",RegionCode="GL",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO4",RegionCode="TL",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO4",RegionCode="VN",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO5",RegionCode="AB",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO5",RegionCode="BH",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO5",RegionCode="BN",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO5",RegionCode="CJ",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO5",RegionCode="SM",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO5",RegionCode="SJ",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO5",RegionCode="MM",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO6",RegionCode="BV",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO6",RegionCode="CV",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO6",RegionCode="HR",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO6",RegionCode="MS",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO6",RegionCode="SB",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO7",RegionCode="AG",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO7",RegionCode="DJ",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO7",RegionCode="GJ",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO7",RegionCode="MH",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO7",RegionCode="OT",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO7",RegionCode="VL",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO8",RegionCode="BC",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO8",RegionCode="BT",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO8",RegionCode="IS",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO8",RegionCode="NT",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO8",RegionCode="SV",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO8",RegionCode="VS",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO9",RegionCode="BZ",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO9",RegionCode="CL",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO9",RegionCode="DB",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO9",RegionCode="GR",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO9",RegionCode="IL",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO9",RegionCode="PH",RegionName="X" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,PFX="YO9",RegionCode="TR",RegionName="X" });

                break;

            case "VE":
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="NS",RegionName="Nova Scotia" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="QC",RegionName="Quebec" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="ON",RegionName="Ontario" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="MB",RegionName="Manitoba" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="SK",RegionName="Saskatchewan" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="AB",RegionName="Alberta" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="BC",RegionName="British Columbia" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="NT",RegionName="Northwest Territories" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="NB",RegionName="New Brunswick" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="NL",RegionName="Newfoundland" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="NL",RegionName="Labrador" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="YT",RegionName="Yukon" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="PE",RegionName="Prince Edward Island" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="NU",RegionName="Nunavut" });
                break;
            case "K": //USA
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="AL",RegionName="Alabama" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="AK",RegionName="Alaska" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="AZ",RegionName="Arizona" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="AR",RegionName="Arkansas" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="CA",RegionName="California" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="CO",RegionName="Colorado" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="CT",RegionName="Connecticut" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="DC",RegionName="District of Columbia" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="DE",RegionName="Delaware" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="FL",RegionName="Florida" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="GA",RegionName="Georgia" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="HI",RegionName="Hawaii" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="ID",RegionName="Idaho" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="IL",RegionName="Illinois" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="IN",RegionName="Indiana" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="IA",RegionName="Iowa" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="KS",RegionName="Kansas" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="KY",RegionName="Kentucky" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="LA",RegionName="Louisiana" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="ME",RegionName="Maine" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="MD",RegionName="Maryland" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="MA",RegionName="Massachusetts" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="MI",RegionName="Michigan" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="MN",RegionName="Minnesota" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="MS",RegionName="Mississippi" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="MO",RegionName="Missouri" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="MT",RegionName="Montana" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="NE",RegionName="Nebraska" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="NV",RegionName="Nevada" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="NH",RegionName="New Hampshire" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="NJ",RegionName="New Jersey" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="NM",RegionName="New Mexico" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="NY",RegionName="New York" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="NC",RegionName="North Carolina" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="ND",RegionName="North Dakota" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="OH",RegionName="Ohio" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="OK",RegionName="Oklahoma" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="OR",RegionName="Oregon" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="PA",RegionName="Pennsylvania" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="RI",RegionName="Rhode Island" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="SC",RegionName="South Carolina" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="SD",RegionName="South Dakota" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="TN",RegionName="Tennessee" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="TX",RegionName="Texas" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="UT",RegionName="Utah" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="VT",RegionName="Vermont" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="VA",RegionName="Virginia" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="WA",RegionName="Washington" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="WV",RegionName="West Virginia" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="WI",RegionName="Wisconsin" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="WY",RegionName="Wyoming" });
                break;
            case "XE": //Mexico
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="",RegionName="" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="AGS",RegionName="Aguascalientes" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="BAC",RegionName="Baja California" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="BCS",RegionName="Baja California Sur" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="CAM",RegionName="Campeche" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="CHI",RegionName="Chiapas" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="CHH",RegionName="Chihuahua" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="CMX",RegionName="Ciudad de México" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="COA",RegionName="Coahuila" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="COL",RegionName="Colima" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="DGO",RegionName="Durango" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="EMX",RegionName="Estado de México" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="GTO",RegionName="Guanajuato" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="GRO",RegionName="Guerrero" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="HGO",RegionName="Hidalgo" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="JAL",RegionName="Jalisco" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="MIC",RegionName="Michoacán" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="MOR",RegionName="Morelos" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="NAY",RegionName="Nayarit" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="NLE",RegionName="Nuevo León" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="OAX",RegionName="Oaxaca" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="PUE",RegionName="Puebla" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="QRO",RegionName="Querétaro" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="QUI",RegionName="Quintana Roo" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="SLP",RegionName="San Luis Potosí" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="SIN",RegionName="Sinaloa" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="SON",RegionName="Sonora" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="TAB",RegionName="Tabasco" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="TAM",RegionName="Tamaulipas" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="TLX",RegionName="Tlaxcala" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="VER",RegionName="Veracruz" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="YUC",RegionName="Yucatán" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="ZAC",RegionName="Zacatecas" });
                break;
            //case "SP": //Poland
            //    Regions.Add(new Region { DXCCPrefix=dXCCPrefix, RegionCode="B ", RegionName="lubuskie" });
            //    Regions.Add(new Region { DXCCPrefix=dXCCPrefix, RegionCode="C ", RegionName="łódzkie" });
            //    Regions.Add(new Region { DXCCPrefix=dXCCPrefix, RegionCode="D ", RegionName="dolnośląskie" });
            //    Regions.Add(new Region { DXCCPrefix=dXCCPrefix, RegionCode="F ", RegionName="pomorskie" });
            //    Regions.Add(new Region { DXCCPrefix=dXCCPrefix, RegionCode="G ", RegionName="śląskie" });
            //    Regions.Add(new Region { DXCCPrefix=dXCCPrefix, RegionCode="J ", RegionName="warmińsko" });
            //    Regions.Add(new Region { DXCCPrefix=dXCCPrefix, RegionCode="K ", RegionName="podkarpackie" });
            //    Regions.Add(new Region { DXCCPrefix=dXCCPrefix, RegionCode="L ", RegionName="lubelskie" });
            //    Regions.Add(new Region { DXCCPrefix=dXCCPrefix, RegionCode="M ", RegionName="małopolskie" });
            //    Regions.Add(new Region { DXCCPrefix=dXCCPrefix, RegionCode="O ", RegionName="podlaskie" });
            //    Regions.Add(new Region { DXCCPrefix=dXCCPrefix, RegionCode="P ", RegionName="kujawsko" });
            //    Regions.Add(new Region { DXCCPrefix=dXCCPrefix, RegionCode="R", RegionName="mazowieckie" });
            //    Regions.Add(new Region { DXCCPrefix=dXCCPrefix, RegionCode="S ", RegionName="świętokrzyskie" });
            //    Regions.Add(new Region { DXCCPrefix=dXCCPrefix, RegionCode="U", RegionName="opolskie" });
            //    Regions.Add(new Region { DXCCPrefix=dXCCPrefix, RegionCode="W", RegionName="wielkopolskie" });
            //    Regions.Add(new Region { DXCCPrefix=dXCCPrefix, RegionCode="Z", RegionName="zachodniopomorskie" });
            //    break;
            case "CT": //Portugal
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Aveiro",RegionCode="AV" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Beja",RegionCode="BJ" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Braga",RegionCode="BR" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Bragança",RegionCode="BG" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Castelo Branco",RegionCode="CB" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Coimbra",RegionCode="CO" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Évora",RegionCode="EV" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Faro",RegionCode="FR" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Guarda",RegionCode="GD" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Leiria",RegionCode="LR" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Lisboa",RegionCode="GD" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Portalegre",RegionCode="PG" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Porto",RegionCode="PT" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Santarem",RegionCode="SR" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Setubal",RegionCode="ST" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Viana do Castelo",RegionCode="VC" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Vila Real",RegionCode="VR" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Viseu",RegionCode="VS" });
                break;
            case "CT3": //Madeira Islands
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Calheta",RegionCode="CAL" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Camara de Lobos",RegionCode="CMB" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Funchal",RegionCode="FU" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Machico",RegionCode="MC" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Ponta do Sol",RegionCode="PS" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Porto Moniz",RegionCode="PM" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Porto Santo",RegionCode="PTS" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Ribeira Brava",RegionCode="RB" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Santa Cruz",RegionCode="SCM" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Santana",RegionCode="STM" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="São Vicente",RegionCode="SV" });
                break;
            case "CU": //Azores
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Angra do Heroismo",RegionCode="AH" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Calheta",RegionCode="CA" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Sta. Cruz da Graciosa",RegionCode="SCG" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Velas",RegionCode="VL" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Vila Praia da Vitória",RegionCode="PV" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Corvo",RegionCode="CV" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Horta",RegionCode="HT" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Lajes das Flores",RegionCode="LJF" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Lajes do Pico",RegionCode="LJP" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Madalena",RegionCode="MD" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Sta. Cruz Das Flores",RegionCode="SCF" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="São Roque do Pico",RegionCode="SRP" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Lagoa",RegionCode="LG" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Nordeste",RegionCode="ND" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Ponta Delgada",RegionCode="PD" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Povoação",RegionCode="PO" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Ribeira Grande",RegionCode="RG" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Vila France do Campo",RegionCode="FC" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionName="Vila do Porto",RegionCode="VP" });
                break;

            case "9A":
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="BJ",RegionName="Bjelovar, Čazma, Garešnica" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="BM",RegionName="Beli Manastir" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="CK",RegionName="Čakovec, Mursko Središće, Prelog" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="DA",RegionName="Daruvar, Grubišno Polje, Lipik, Pakrac" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="DE",RegionName="Čabar, Delnice" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="DJ",RegionName="Đakovo" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="DU",RegionName="Dubrovnik, Korčula, Metković, Opuzen, Ploče" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="GS",RegionName="Gospić, Novalja, Otočac, Senj" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="IM",RegionName="Imotski" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="KA",RegionName="Duga Resa, Karlovac, Slunj, Ozalj" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="KC",RegionName="Đurđevac, Koprivnica" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="KR",RegionName="Donja Stubica, Klanjec, Krapina, Oroslavje, Pregrada, Zabok, Zlatar" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="KT",RegionName="Kutina, Novska, Popovača" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="KZ",RegionName="Križevci" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="MA",RegionName="Makarska, Vrgorac" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="NA",RegionName="Donji Miholjac, Našice" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="NG",RegionName="Nova Gradiška" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="OG",RegionName="Ogulin" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="OS",RegionName="Belišće, Osijek, Valpovo" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="PU",RegionName="Buje, Buzet, Labin, Novigrad, Pazin, Poreč, Pula, Rovinj, Umag, Vodnjan" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="PZ",RegionName="Kutjevo, Pleternica, Požega" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="RI",RegionName="Bakar, Cres, Crikvenica, Kastav, Kraljevica, Krk, Mali Lošinj, Novi Vinodolski, Opatija, Rab, Rijeka, Vrbovsko" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="SB",RegionName="Slavonski Brod" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="SK",RegionName="Glina, Hrvatska Kostajnica, Sisak, Petrinja" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="SL",RegionName="Orahovica, Slatina" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="ST",RegionName="Hvar, Kaštela, Komiža, Omiš, Sinj, Solin, Split, Stari Grad, Supetar, Trilj, Trogir, Vis, Vrlika" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="SI",RegionName="Drniš, Knin, Skradin, Šibenik, Vodice" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="VK",RegionName="Otok, Vinkovci" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="VT",RegionName="Virovitica" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="VU",RegionName="Ilok, Vukovar" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="VZ",RegionName="Ivanec, Lepoglava, Ludbreg, Novi Marof, Varaždin, Varaždinske Toplice" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="ZD",RegionName="Benkovac, Biograd na Moru, Nin, Obrovac, Pag, Zadar" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="ZG",RegionName="Dugo Selo, Ivanić-Grad, Jastrebarsko, Samobor, Sveta Nedelja, Sveti Ivan Zelina, Velika Gorica, Vrbovec, Zagreb, Zaprešić" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="ZU",RegionName="Županja" });
                break;

            case "HA":
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="BN",RegionName="Bács-Kiskun" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="BA",RegionName="Baranya" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="BE",RegionName="Békés" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="BO",RegionName="Borsod-Abaúj-Zemplén" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="CS",RegionName="Csongrád" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="FE",RegionName="Fejér" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="GY",RegionName="Győr-Moson-Sopron" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="HB",RegionName="Hajdú-Bihar" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="HE",RegionName="Heves" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="SZ",RegionName="Jász-Nagykun-Szolnok" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="KO",RegionName="Komárom-Esztergom" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="NG",RegionName="Nógrád" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="PE",RegionName="Pest" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="SO",RegionName="Somogy" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="SA",RegionName="Szabolcs-Szatmár-Bereg" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="TO",RegionName="Tolna" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="VA",RegionName="Vas" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="VE",RegionName="Veszprém" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="ZA",RegionName="Zala" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="BP",RegionName="Budapest (capital)" });
                break;
            case "PY": // Brazil states
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="AC",RegionName="Acre" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="AL",RegionName="Alagoas" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="AP",RegionName="Amapá" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="AM",RegionName="Amazonas" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="BA",RegionName="Bahia" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="CE",RegionName="Ceará" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="DF",RegionName="Distrito Federal" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="ES",RegionName="Espírito Santo" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="GO",RegionName="Goiás" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="MA",RegionName="Maranhão" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="MT",RegionName="Mato Grosso" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="MS",RegionName="Mato Grosso do Sul" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="MG",RegionName="Minas Gerais" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="PA",RegionName="Pará" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="PB",RegionName="Paraíba" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="PR",RegionName="Paraná" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="PE",RegionName="Pernambuco" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="PI",RegionName="Piauí" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="RJ",RegionName="Rio de Janeiro" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="RN",RegionName="Rio Grande do Norte" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="RS",RegionName="Rio Grande do Sul" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="RO",RegionName="Rondônia RO" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="RR",RegionName="Roraima" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="SC",RegionName="Santa Catarina" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="SP",RegionName="São Paulo" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="SE",RegionName="Sergipe" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="TO",RegionName="Tocantins" });

                break;
            case "UA": //Russia Oblasts 
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="SP",RegionName="Saint Petersburg" });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="LO",RegionName="Leningradskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="KO",RegionName="Komi" });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="KL",RegionName="Karelia" });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="AR",RegionName="Arkhangelskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="NO",RegionName="Nenetsky AO" });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="VO",RegionName="Vologodskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="NV",RegionName="Novgorodskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="PS",RegionName="Pskovskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="MU",RegionName="Murmanskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="MA",RegionName="Moscow city" });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="MO",RegionName="Moskovskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="OR",RegionName="Orlovskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="MO",RegionName="Moskovskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="LP",RegionName="Lipetskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="TV",RegionName="Tverskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="VR",RegionName="Voronezhskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="SM",RegionName="Smolenskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="YR",RegionName="Yaroslavskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="KS",RegionName="Kostromskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="TL",RegionName="Tulskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="VR",RegionName="Voronezhskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="TB",RegionName="Tambovskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="RA",RegionName="Ryazanskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="NN",RegionName="Nizhegorodskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="IV",RegionName="Ivanovskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="VL",RegionName="Vladimirskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="KU",RegionName="Kurskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="KG",RegionName="Kaluzhskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="BR",RegionName="Bryanskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="BO",RegionName="Belgorodskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="VG",RegionName="Volgogradskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="SA",RegionName="Saratovskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="PE",RegionName="Penzenskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="SR",RegionName="Samarskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="UL",RegionName="Ulyanovskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="KI",RegionName="Kirovskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="TA",RegionName="Tatarstan" });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="MR",RegionName="Mary El" });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="MD",RegionName="Mordoviya" });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="UD",RegionName="Udmurtiya" });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="CU",RegionName="Chuvashiya" });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="KR",RegionName="Krasnodarskiy kray" });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="KC",RegionName="Karachaevo-Cherkesiya" });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="ST",RegionName="Stavropolskiy kray" });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="KM",RegionName="Kalmykiya" });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="SO",RegionName="Severnaya Osetiya" });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="RK",RegionName="Crimea" });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="RO",RegionName="Rostovskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="CN",RegionName="Chechnya" });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="IN",RegionName="Ingushetiya" });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="SE",RegionName="Sevastopol" });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="AO",RegionName="Astrakhanskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="DA",RegionName="Dagestan" });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="KB",RegionName="Kabardino-Balkariya" });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="AD",RegionName="Adygeya" });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="DO",RegionName="Donetsk People's Rep." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="HE",RegionName="Khersonskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="LU",RegionName="Luhansk People's Rep." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="ZP",RegionName="Zaporozhskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA",RegionCode="KO",RegionName="Komi" });
               
                break;
            case "UA0": //Asiatic Russia Oblasts 
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="CB",RegionName="Chelyabinskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="SV",RegionName="Sverdlovskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="PM",RegionName="Permskiy kray" });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="TO",RegionName="Tomskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="HM",RegionName="Hanty-Mansiysky AO" });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="YN",RegionName="Yamalo-Nenetsky AO" });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="TN",RegionName="Tyumenskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="OM",RegionName="Omskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="NS",RegionName="Novosibirskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="KN",RegionName="Kurganskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="OB",RegionName="Orenburgskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="KE",RegionName="Kemerovskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="BA",RegionName="Bashkortostan" });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="AL",RegionName="Altaysky kray" });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="GA",RegionName="Altai Republic" });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="KK",RegionName="Krasnoyarskiy kray" });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="HK",RegionName="Khabarovsky kray" });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="EA",RegionName="Evreyskaya АО" });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="SL",RegionName="Sakhalinskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="MG",RegionName="Magadanskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="AM",RegionName="Amurskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="CK",RegionName="Chukotsky AO" });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="PK",RegionName="Primorsky kray" });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="BU",RegionName="Buryatiya" });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="YA",RegionName="Sakha" });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="IR",RegionName="Irkutskaya obl." });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="ZK",RegionName="Zabaykalskiy kray" });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="HA",RegionName="Hakasia" });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="TU",RegionName="Tuva" });
                Regions.Add(new Region { DXCCPrefix="UA0",RegionCode="KT",RegionName="Kamchatskiy kray" });
                break;
            case "UA2": //Kaliningrad Oblast 
                Regions.Add(new Region { DXCCPrefix="UA2",RegionCode="KA",RegionName="Kaliningradskaya obl." });

                break;
            case "PA": // PA provinces - PACC
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="DR",RegionName="Drenthe" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="NB",RegionName="Noord-Brabant" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="FL",RegionName="Flevoland" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="NH",RegionName="Noord-Holland" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="FR",RegionName="Friesland" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="OV",RegionName="Overijssel" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="GD",RegionName="Gelderland" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="UT",RegionName="Utrecht" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="GR",RegionName="Groningen" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="ZH",RegionName="Zuid-Holland" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="LB",RegionName="Limburg" });
                Regions.Add(new Region { DXCCPrefix=dXCCPrefix,RegionCode="ZL",RegionName="Zeeland" });
                break;
            case "I": // Italy provinces - ARI
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="AL",RegionName="I1" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="AT",RegionName="I1" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="BI",RegionName="I1" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="CN",RegionName="I1" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="GE",RegionName="I1" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="IM",RegionName="I1" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="NO",RegionName="I1" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="SP",RegionName="I1" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="SV",RegionName="I1" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="TO",RegionName="I1" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="VB",RegionName="I1" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="VC",RegionName="I1" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="AO",RegionName="IX1" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="BG",RegionName="I2" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="BS",RegionName="I2" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="CO",RegionName="I2" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="CR",RegionName="I2" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="LC",RegionName="I2" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="LO",RegionName="I2" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="MB",RegionName="I2" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="MI",RegionName="I2" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="MN",RegionName="I2" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="PV",RegionName="I2" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="SO",RegionName="I2" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="VA",RegionName="I2" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="BL",RegionName="I3" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="PD",RegionName="I3" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="RO",RegionName="I3" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="TV",RegionName="I3" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="VE",RegionName="I3" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="VI",RegionName="I3" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="VR",RegionName="I3" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="BZ",RegionName="IN3" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="TN",RegionName="IN3" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="GO",RegionName="IV3" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="PN",RegionName="IV4" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="TS",RegionName="IV5" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="UD",RegionName="IV6" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="BO",RegionName="I4" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="FC",RegionName="I4" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="FE",RegionName="I4" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="MO",RegionName="I4" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="PC",RegionName="I4" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="PR",RegionName="I4" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="RA",RegionName="I4" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="RE",RegionName="I4" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="RN",RegionName="I4" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="AR",RegionName="I5" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="FI",RegionName="I5" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="GR",RegionName="I5" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="LI",RegionName="I5" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="LU",RegionName="I5" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="MS",RegionName="I5" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="PI",RegionName="I5" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="PO",RegionName="I5" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="PT",RegionName="I5" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="SI",RegionName="I5" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="AN",RegionName="I6" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="AP",RegionName="I6" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="AQ",RegionName="I6" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="CH",RegionName="I6" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="FM",RegionName="I6" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="MC",RegionName="I6" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="PE",RegionName="I6" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="PU",RegionName="I6" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="TE",RegionName="I6" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="BA",RegionName="I7" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="BR",RegionName="I7" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="BT",RegionName="I7" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="FG",RegionName="I7" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="LE",RegionName="I7" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="MT",RegionName="I7" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="TA",RegionName="I7" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="AV",RegionName="I8" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="BJ",RegionName="I8" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="CB",RegionName="I8" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="CE",RegionName="I8" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="CS",RegionName="I8" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="CZ",RegionName="I8" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="IS",RegionName="I8" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="KR",RegionName="I8" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="NA",RegionName="I8" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="PZ",RegionName="I8" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="RC",RegionName="I8" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="SA",RegionName="I8" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="VV",RegionName="I8" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="FR",RegionName="I0" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="LT",RegionName="I0" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="PG",RegionName="I0" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="RI",RegionName="I0" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="RM",RegionName="I0" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="TR",RegionName="I0" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="VT",RegionName="I0" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="AG",RegionName="IT9" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="CL",RegionName="IT9" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="CT",RegionName="IT9" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="EN",RegionName="IT9" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="ME",RegionName="IT9" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="PA",RegionName="IT9" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="RG",RegionName="IT9" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="SR",RegionName="IT9" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="TP",RegionName="IT9" });

                break;
            case "IS0": // Sardinia provinces - ARI
                Regions.Add(new Region { DXCCPrefix="IS0",RegionCode="CA",RegionName="IS0" });
                Regions.Add(new Region { DXCCPrefix="IS0",RegionCode="NU",RegionName="IS0" });
                Regions.Add(new Region { DXCCPrefix="IS0",RegionCode="OR",RegionName="IS0" });
                Regions.Add(new Region { DXCCPrefix="IS0",RegionCode="SS",RegionName="IS0" });
                Regions.Add(new Region { DXCCPrefix="IS0",RegionCode="SU",RegionName="IS0" });

                break;
            case "YU": // Serbian Counties YU DX contest
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="BGD",RegionName="Beograd" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="PIR",RegionName="Pirotski upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="BOR",RegionName="Borski upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="POD",RegionName="Podunavski upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="BRA",RegionName="Braničevski upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="POM",RegionName="Pomoravski upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="JAB",RegionName="Jablanički upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="PRI",RegionName="Prizrenski upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="JBB",RegionName="Južnobački upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="RAN",RegionName="Rasinski upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="JBN",RegionName="Južnobanatski upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="RAS",RegionName="Raški upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="KMO",RegionName="Kosovsko-mitrovački upravni" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="SBB",RegionName="Severnobački upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="KOL",RegionName="Kolubarski upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="SBN",RegionName="Srednjebanatski upravni okr" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="KOS",RegionName="Kosovski upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="SBT",RegionName="Severnobanatski upravni okr" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="KPO",RegionName="Kosovsko-pomoravski upravni" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="SRM",RegionName="Sremski upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="MAC",RegionName="Mačvanski upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="SUM",RegionName="Šumadijski upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="MOR",RegionName="Moravički upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="TOP",RegionName="Toplički upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="NIS",RegionName="Nišavski upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="ZAJ",RegionName="Zaječarski upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="PCI",RegionName="Pčinjski upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="ZBB",RegionName="Zapadnobački upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="PEC",RegionName="Pećki upravni okrug" });
                Regions.Add(new Region { DXCCPrefix="YU",RegionCode="ZLA",RegionName="Zlatiborski upravni okrug" });
                break;


            case "OL":  // OK

                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="BBN",RegionName="Benesov-Central Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="BBE",RegionName="Beroun-Central Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="BKD",RegionName="Kladno-Central Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="BKO",RegionName="Kolin-Central Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="BKH",RegionName="Kutna Hora-Central Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="BME",RegionName="Melnik-Central Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="BMB",RegionName="Mlada Boleslav-Central Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="BNY",RegionName="Nymburk-Central Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="BPV",RegionName="Praha vychod-Central Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="BPZ",RegionName="Praha zapad-Central Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="BPB",RegionName="Pribram-Central Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="BRA",RegionName="Rakovnik-Central Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="FCR",RegionName="Chrudim-Eastern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="FHB",RegionName="Havlickuv Brod-Eastern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="FHK",RegionName="Hradec Kralove-Eastern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="FJI",RegionName="Jicin-Eastern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="FNA",RegionName="Nachod-Eastern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="FPA",RegionName="Pardubice-Eastern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="FRK",RegionName="Rychn n. Kneznou-Eastern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="FSE",RegionName="Semily-Eastern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="FSV",RegionName="Svitavy-Eastern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="FTR",RegionName="Trutnov-Eastern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="FUO",RegionName="Usti nad Orlici-Eastern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="ECL",RegionName="Ceska Lipa-Northern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="ECH",RegionName="Chomutov-Northern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="EDE",RegionName="Decin-Northern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="EJA",RegionName="Jablonec n. Nisou-Northern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="ELI",RegionName="Liberec-Northern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="ELT",RegionName="Litomerice-Northern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="ELO",RegionName="Louny-Northern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="EMO",RegionName="Most-Northern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="ETE",RegionName="Teplice-Northern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="EUL",RegionName="Usti nad Labem-Northern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="HBR",RegionName="Bruntal-Northern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="HFM",RegionName="Frydek - Mistek-Northern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="HJE",RegionName="Jesenik-Northern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="HKA",RegionName="Karvina-Northern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="HNJ",RegionName="Novy Jicin-Northern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="HOL",RegionName="Olomouc-Northern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="HOP",RegionName="Opava-Northern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="HOS",RegionName="Ostrava-Northern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="HPR",RegionName="Prerov-Northern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="HSU",RegionName="Sumperk-Northern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="HVS",RegionName="Vsetin-Northern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="APA",RegionName="Praha 1-Praha" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="APJ",RegionName="Praha 10-Praha" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="APB",RegionName="Praha 2-Praha" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="APC",RegionName="Praha 3-Praha" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="APD",RegionName="Praha 4-Praha" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="APE",RegionName="Praha 5-Praha" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="APF",RegionName="Praha 6-Praha" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="APG",RegionName="Praha 7-Praha" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="APH",RegionName="Praha 8-Praha" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="API",RegionName="Praha 9-Praha" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="CBU",RegionName="Ceske Budejovice-Southern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="CCK",RegionName="Cesky Krumlov-Southern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="CJH",RegionName="Jindrichuv Hradec-Southern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="CPE",RegionName="Pelhrimov-Southern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="CPI",RegionName="Pisek-Southern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="CPR",RegionName="Prachatice-Southern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="CST",RegionName="Strakonice-Southern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="CTA",RegionName="Tabor-Southern Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="GBL",RegionName="Blansko-Southern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="GBR",RegionName="Breclav-Southern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="GBM",RegionName="Brno mesto-Southern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="GBV",RegionName="Brno venkov-Southern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="GHO",RegionName="Hodonin-Southern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="GJI",RegionName="Jihlava-Southern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="GKR",RegionName="Kromeriz-Southern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="GPR",RegionName="Prostejov-Southern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="GTR",RegionName="Trebic-Southern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="GUH",RegionName="Uherske Hradiste-Southern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="GVY",RegionName="Vyskov-Southern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="GZS",RegionName="Zdar nad Sazavou-Southern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="GZL",RegionName="Zlin-Southern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="GZN",RegionName="Znojmo-Southern Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="DCH",RegionName="Cheb-Western Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="DDO",RegionName="Domazlice-Western Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="DKV",RegionName="Karlovy Vary-Western Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="DKL",RegionName="Klatovy-Western Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="DPJ",RegionName="Plzen jih-Western Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="DPM",RegionName="Plzen mesto-Western Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="DPS",RegionName="Plzen sever-Western Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="DRO",RegionName="Rokycany-Western Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="DSO",RegionName="Sokolov-Western Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="DTA",RegionName="Tachov-Western Bohemia" });
                break;
            case "OM":
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="KRU",RegionName="Krupina-Banska Bystrica, prefix OM10" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="LUC",RegionName="Lucenec-Banska Bystrica, prefix OM11" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="POL",RegionName="Poltar-Banska Bystrica, prefix OM12" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="REV",RegionName="Revuca-Banska Bystrica, prefix OM13" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="RSO",RegionName="Rimavska Sobota-Banska Bystrica, prefix OM14" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="VKR",RegionName="Velky Krtis-Banska Bystrica, prefix OM15" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="ZVO",RegionName="Zvolen-Banska Bystrica, prefix OM16" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="ZAR",RegionName="Zarnovica-Banska Bystrica, prefix OM17" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="ZIH",RegionName="Ziar nad Hronom-Banska Bystrica, prefix OM18" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="BST",RegionName="Banska Stiavnica-Banska Bystrica, prefix OM19" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="BBY",RegionName="Banska Bystrica-Banska Bystrica, prefix OM7" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="BRE",RegionName="Brezno-Banska Bystrica, prefix OM8" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="DET",RegionName="Detva-Banska Bystrica, prefix OM9" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="BAA",RegionName="Bratislava 1-Bratislava, prefix OM1" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="BAB",RegionName="Bratislava 2-Bratislava, prefix OM2" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="BAC",RegionName="Bratislava 3-Bratislava, prefix OM3" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="BAD",RegionName="Bratislava 4-Bratislava, prefix OM4" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="BAE",RegionName="Bratislava 5-Bratislava, prefix OM5" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="MAL",RegionName="Malacky-Bratislava, prefix OM6" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="PEZ",RegionName="Pezinok-Bratislava, prefix OM7" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="SEN",RegionName="Senec-Bratislava, prefix OM8" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="KEC",RegionName="Kosice 3-Kosice, prefix OM10" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="KED",RegionName="Kosice 4-Kosice, prefix OM11" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="KEO",RegionName="Kosice okolie-Kosice, prefix OM12" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="GEL",RegionName="Gelnica-Kosice, prefix OM13" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="MIC",RegionName="Michalovce-Kosice, prefix OM14" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="ROZ",RegionName="Roznava-Kosice, prefix OM15" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="SOB",RegionName="Sobrance-Kosice, prefix OM16" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="SNV",RegionName="Spisska Nova Ves-Kosice, prefix OM17" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="TRE",RegionName="Trebisov-Kosice, prefix OM18" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="KEA",RegionName="Kosice 1-Kosice, prefix OM8" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="KEB",RegionName="Kosice 2-Kosice, prefix OM9" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="TOP",RegionName="Topolcany-Nitra, prefix OM10" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="ZMO",RegionName="Zlate Moravce-Nitra, prefix OM11" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="NIT",RegionName="Nitra-Nitra, prefix OM5" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="KOM",RegionName="Komarno-Nitra, prefix OM6" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="LVC",RegionName="Levice-Nitra, prefix OM7" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="NZA",RegionName="Nove Zamky-Nitra, prefix OM8" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="SAL",RegionName="Sala-Nitra, prefix OM9" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="PRE",RegionName="Presov-Presov, prefix OM0" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="PRE",RegionName="Presov-Presov, prefix OM0" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="BAR",RegionName="Bardejov-Presov, prefix OM1" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="BAR",RegionName="Bardejov-Presov, prefix OM1" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="STR",RegionName="Stropkov-Presov, prefix OM10" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="STR",RegionName="Stropkov-Presov, prefix OM10" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="SVI",RegionName="Svidnik-Presov, prefix OM11" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="SVI",RegionName="Svidnik-Presov, prefix OM11" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="VRT",RegionName="Vranov nad Toplou-Presov, prefix OM12" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="VRT",RegionName="Vranov nad Toplou-Presov, prefix OM12" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="HUM",RegionName="Humenne-Presov, prefix OM2" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="HUM",RegionName="Humenne-Presov, prefix OM2" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="KEZ",RegionName="Kezmarok-Presov, prefix OM3" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="KEZ",RegionName="Kezmarok-Presov, prefix OM3" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="LEV",RegionName="Levoca-Presov, prefix OM4" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="LEV",RegionName="Levoca-Presov, prefix OM4" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="MED",RegionName="Medzilaborce-Presov, prefix OM5" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="MED",RegionName="Medzilaborce-Presov, prefix OM5" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="POP",RegionName="Poprad-Presov, prefix OM6" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="POP",RegionName="Poprad-Presov, prefix OM6" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="SAB",RegionName="Sabinov-Presov, prefix OM7" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="SAB",RegionName="Sabinov-Presov, prefix OM7" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="SNI",RegionName="Snina-Presov, prefix OM8" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="SNI",RegionName="Snina-Presov, prefix OM8" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="SLU",RegionName="Stara Lubovna-Presov, prefix OM9" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="SLU",RegionName="Stara Lubovna-Presov, prefix OM9" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="PBY",RegionName="Povazska Bystrica-Trencin, prefix OM10" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="PRI",RegionName="Prievidza-Trencin, prefix OM11" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="PUC",RegionName="Puchov-Trencin, prefix OM12" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="TNC",RegionName="Trencin-Trencin, prefix OM4" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="BAN",RegionName="Banovce n. Bebr.-Trencin, prefix OM5" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="ILA",RegionName="Ilava-Trencin, prefix OM6" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="MYJ",RegionName="Myjava-Trencin, prefix OM7" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="NMV",RegionName="Nove Mesto n. Vah-Trencin, prefix OM8" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="PAR",RegionName="Partizanske-Trencin, prefix OM9" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="TRN",RegionName="Trnava-Trnava, prefix OM2" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="DST",RegionName="Dunajska Streda-Trnava, prefix OM3" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="GAL",RegionName="Galanta-Trnava, prefix OM4" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="HLO",RegionName="Hlohovec-Trnava, prefix OM5" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="PIE",RegionName="Piestany-Trnava, prefix OM6" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="SEA",RegionName="Senica-Trnava, prefix OM7" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="SKA",RegionName="Skalica-Trnava, prefix OM8" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="KNM",RegionName="Kysucke N. Mesto-Zilina, prefix OM10" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="LMI",RegionName="Liptovsky Mikulas-Zilina, prefix OM11" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="MAR",RegionName="Martin-Zilina, prefix OM12" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="NAM",RegionName="Namestovo-Zilina, prefix OM13" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="RUZ",RegionName="Ruzomberok-Zilina, prefix OM14" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="TTE",RegionName="Turcianske Teplice-Zilina, prefix OM15" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="TVR",RegionName="Tvrdosin-Zilina, prefix OM16" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="ZIL",RegionName="Zilina-Zilina, prefix OM6" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="BYT",RegionName="Bytca-Zilina, prefix OM7" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="CAD",RegionName="Cadca-Zilina, prefix OM8" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="DKU",RegionName="Dolny Kubin-Zilina, prefix OM9" });

                break;


            }
            return Regions;
        }
        public static List<Region> GetRegionMultipliersByContest(ContestEnum contest)  // NEW Use for Contest to region Per DXCC mapping !!!!!  NEW method for regions for each DXCC in the contest
            { // Use Specificatins Excel to create items
            List<Region> Regions = new List<Region>();
            switch(contest) {
            case ContestEnum.FCG_FQP: // Florida QSO Party
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="ALC",RegionName="ALACHUA" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="BAK",RegionName="BAKER" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="BAY",RegionName="BAY" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="BRA",RegionName="BRADFORD" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="BRE",RegionName="BREVARD" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="BRO",RegionName="BROWARD" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="CAH",RegionName="CALHOUN" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="CHA",RegionName="CHARLOTTE" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="CIT",RegionName="CITRUS" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="CLA",RegionName="CLAY" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="CLR",RegionName="COLLIER" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="CLM",RegionName="COLUMBIA" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="DAD",RegionName="MIAMI-DADE" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="DES",RegionName="DESOTO" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="DIX",RegionName="DIXIE" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="DUV",RegionName="DUVAL" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="ESC",RegionName="ESCAMBIA" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="FLG",RegionName="FLAGLER" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="FRA",RegionName="FRANKLIN" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="GAD",RegionName="GADSDEN" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="GIL",RegionName="GILCHRIST" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="GLA",RegionName="GLADES" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="GUL",RegionName="GULF" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="HAM",RegionName="HAMILTON" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="HAR",RegionName="HARDEE" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="HEN",RegionName="HENDRY" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="HER",RegionName="HERNANDO" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="HIG",RegionName="HIGHLANDS" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="HIL",RegionName="HILLSBOROUGH" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="HOL",RegionName="HOLMES" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="IDR",RegionName="INDIAN RIVER" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="JAC",RegionName="JACKSON" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="JEF",RegionName="JEFFERSON" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="LAF",RegionName="LAFAYETTE" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="LAK",RegionName="LAKE" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="LEE",RegionName="LEE" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="LEO",RegionName="LEON" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="LEV",RegionName="LEVY" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="LIB",RegionName="LIBERTY" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="MAD",RegionName="MADISON" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="MTE",RegionName="MANATEE" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="MAO",RegionName="MARION" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="MRT",RegionName="MARTIN" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="MON",RegionName="MONROE" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="NAS",RegionName="NASSAU" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="OKA",RegionName="OKALOOSA" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="OKE",RegionName="OKEECHOBEE" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="ORA",RegionName="ORANGE" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="OSC",RegionName="OSCEOLA" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="PAL",RegionName="PALM BEACH" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="PAS",RegionName="PASCO" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="PIN",RegionName="PINELLAS" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="POL",RegionName="POLK" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="PUT",RegionName="PUTNAM" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="SAN",RegionName="SANTA ROSA" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="SAR",RegionName="SARASOTA" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="SEM",RegionName="SEMINOLE" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="STJ",RegionName="ST. JOHNS" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="STL",RegionName="ST. LUCIE" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="SUM",RegionName="SUMTER" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="SUW",RegionName="SUWANNEE" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="TAY",RegionName="TAYLOR" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="UNI",RegionName="UNION" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="VOL",RegionName="VOLUSIA" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="WAK",RegionName="WAKULLA" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="WAL",RegionName="WALTON" });
                Regions.Add(new Region { DXCCPrefix="K",RegionCode="WAG",RegionName="WASHINGTON" });

                break;
            case ContestEnum.SPDX:
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="B",RegionName="lubuskie" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="C",RegionName="łódzkie" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="D",RegionName="dolnośląskie" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="F",RegionName="pomorskie" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="G",RegionName="śląskie" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="J",RegionName="warmińskomazurskie" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="K",RegionName="podkarpackie" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="L",RegionName="lubelskie" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="M",RegionName="małopolskie" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="O",RegionName="podlaskie" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="P",RegionName="kujawskopomorskie" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="R",RegionName="mazowieckie" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="S",RegionName="świętokrzyskie" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="U",RegionName="opolskie" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="W",RegionName="wielkopolskie" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="Z",RegionName="zachodniopomorskie" });

                break;
            case ContestEnum.EUDXC:
                Regions.Add(new Region { DXCCPrefix="5B",RegionCode="CY01",RegionName="Nicosia" });
                Regions.Add(new Region { DXCCPrefix="5B",RegionCode="CY02",RegionName="Famagusta" });
                Regions.Add(new Region { DXCCPrefix="5B",RegionCode="CY03",RegionName="Limassol" });
                Regions.Add(new Region { DXCCPrefix="5B",RegionCode="CY04",RegionName="Larnaca" });
                Regions.Add(new Region { DXCCPrefix="5B",RegionCode="CY05",RegionName="Paphos" });
                Regions.Add(new Region { DXCCPrefix="9A",RegionCode="HR01",RegionName="Central Croatia" });
                Regions.Add(new Region { DXCCPrefix="9A",RegionCode="HR02",RegionName="Central and Southern Adriatic (Dalmatia)" });
                Regions.Add(new Region { DXCCPrefix="9A",RegionCode="HR03",RegionName="East Croatia (Slavonia)" });
                Regions.Add(new Region { DXCCPrefix="9A",RegionCode="HR04",RegionName="North Adria&Lika" });
                Regions.Add(new Region { DXCCPrefix="9A",RegionCode="HR05",RegionName="North-West Croatia" });
                Regions.Add(new Region { DXCCPrefix="9H",RegionCode="MT01",RegionName="San Ġwann" });
                Regions.Add(new Region { DXCCPrefix="9H",RegionCode="MT02",RegionName="Gozo Region" });
                Regions.Add(new Region { DXCCPrefix="9H",RegionCode="MT03",RegionName="Northern Region" });
                Regions.Add(new Region { DXCCPrefix="9H",RegionCode="MT04",RegionName="South Eastern Region" });
                Regions.Add(new Region { DXCCPrefix="9H",RegionCode="MT05",RegionName="Southern Region" });

                //Portugal -> Madeira CT3=PT06  others.


                Regions.Add(new Region { DXCCPrefix="CT",RegionCode="PT01",RegionName="Açores" });
                Regions.Add(new Region { DXCCPrefix="CT",RegionCode="PT02",RegionName="Alentejo" });
                Regions.Add(new Region { DXCCPrefix="CT",RegionCode="PT03",RegionName="Algarve" });
                Regions.Add(new Region { DXCCPrefix="CT",RegionCode="PT04",RegionName="Centro" });
                Regions.Add(new Region { DXCCPrefix="CT",RegionCode="PT05",RegionName="Lisboa e Vale Do Tejo" });
                Regions.Add(new Region { DXCCPrefix="CT3",RegionCode="PT06",RegionName="Madeira, AF" });
                Regions.Add(new Region { DXCCPrefix="CT",RegionCode="PT07",RegionName="Norte" });
                Regions.Add(new Region { DXCCPrefix="DL",RegionCode="DE01",RegionName="Baden-Württemberg" });
                Regions.Add(new Region { DXCCPrefix="DL",RegionCode="DE02",RegionName="Bavaria" });
                Regions.Add(new Region { DXCCPrefix="DL",RegionCode="DE03",RegionName="Berlin" });
                Regions.Add(new Region { DXCCPrefix="DL",RegionCode="DE04",RegionName="Brandenburg" });
                Regions.Add(new Region { DXCCPrefix="DL",RegionCode="DE05",RegionName="Bremen" });
                Regions.Add(new Region { DXCCPrefix="DL",RegionCode="DE06",RegionName="Hamburg" });
                Regions.Add(new Region { DXCCPrefix="DL",RegionCode="DE07",RegionName="Hesse" });
                Regions.Add(new Region { DXCCPrefix="DL",RegionCode="DE08",RegionName="Lower Saxony" });
                Regions.Add(new Region { DXCCPrefix="DL",RegionCode="DE09",RegionName="Mecklenburg-Vorpommern" });
                Regions.Add(new Region { DXCCPrefix="DL",RegionCode="DE10",RegionName="North Rhine-Westphalia" });
                Regions.Add(new Region { DXCCPrefix="DL",RegionCode="DE11",RegionName="Rhineland-Palatinate" });
                Regions.Add(new Region { DXCCPrefix="DL",RegionCode="DE12",RegionName="Saarland" });
                Regions.Add(new Region { DXCCPrefix="DL",RegionCode="DE13",RegionName="Saxony" });
                Regions.Add(new Region { DXCCPrefix="DL",RegionCode="DE14",RegionName="Saxony-Anhalt" });
                Regions.Add(new Region { DXCCPrefix="DL",RegionCode="DE15",RegionName="Schleswig-Holstein" });
                Regions.Add(new Region { DXCCPrefix="DL",RegionCode="DE16",RegionName="Thuringia" });
                Regions.Add(new Region { DXCCPrefix="EA",RegionCode="ES01",RegionName="Andalusia" });
                Regions.Add(new Region { DXCCPrefix="EA",RegionCode="ES02",RegionName="Catalonia" });
                Regions.Add(new Region { DXCCPrefix="EA",RegionCode="ES03",RegionName="Community of Madrid" });
                Regions.Add(new Region { DXCCPrefix="EA",RegionCode="ES04",RegionName="Valencian Community" });
                Regions.Add(new Region { DXCCPrefix="EA",RegionCode="ES05",RegionName="Galicia" });
                Regions.Add(new Region { DXCCPrefix="EA",RegionCode="ES06",RegionName="Castile and León" });
                Regions.Add(new Region { DXCCPrefix="EA",RegionCode="ES07",RegionName="Basque Autonomous Community" });
                Regions.Add(new Region { DXCCPrefix="EA",RegionCode="ES08",RegionName="Castilla-La Mancha" });
                Regions.Add(new Region { DXCCPrefix="EA8",RegionCode="ES09",RegionName="Canary Islands , AF" });
                Regions.Add(new Region { DXCCPrefix="EA",RegionCode="ES10",RegionName="Region of Murcia" });
                Regions.Add(new Region { DXCCPrefix="EA",RegionCode="ES11",RegionName="Aragon" });
                Regions.Add(new Region { DXCCPrefix="EA",RegionCode="ES12",RegionName="Extremadura" });
                Regions.Add(new Region { DXCCPrefix="EA6",RegionCode="ES13",RegionName="Balearic Islands" });
                Regions.Add(new Region { DXCCPrefix="EA",RegionCode="ES14",RegionName="Principality of Asturias" });
                Regions.Add(new Region { DXCCPrefix="EA",RegionCode="ES15",RegionName="Chartered Community of Navarre" });
                Regions.Add(new Region { DXCCPrefix="EA",RegionCode="ES16",RegionName="Cantabria" });
                Regions.Add(new Region { DXCCPrefix="EA",RegionCode="ES17",RegionName="La Rioja" });
                Regions.Add(new Region { DXCCPrefix="EA9",RegionCode="ES18",RegionName="Ceuta, AF" });
                Regions.Add(new Region { DXCCPrefix="EA",RegionCode="ES19",RegionName="Melilla, AF" });
                Regions.Add(new Region { DXCCPrefix="EI",RegionCode="IE01",RegionName="Leinster" });
                Regions.Add(new Region { DXCCPrefix="EI",RegionCode="IE02",RegionName="Ulster" });
                Regions.Add(new Region { DXCCPrefix="EI",RegionCode="IE03",RegionName="Munster" });
                Regions.Add(new Region { DXCCPrefix="EI",RegionCode="IE04",RegionName="Connacht" });
                Regions.Add(new Region { DXCCPrefix="ES",RegionCode="EE01",RegionName="Northern Estonia (Harju County)" });
                Regions.Add(new Region { DXCCPrefix="ES",RegionCode="EE02",RegionName="Western Estonia (Hiiu County, Lääne County, Pärnu County, Saare County)" });
                Regions.Add(new Region { DXCCPrefix="ES",RegionCode="EE03",RegionName="Central Estonia (Järva County, Lääne-Viru County, Rapla County)" });
                Regions.Add(new Region { DXCCPrefix="ES",RegionCode="EE04",RegionName="Northeastern Estonia (Ida-Viru County)" });
                Regions.Add(new Region { DXCCPrefix="ES",RegionCode="EE05",RegionName="Southern Estonia (Jõgeva County, Põlva County, Tartu County, Valga County, Viljandi County, Võru County)" });
                Regions.Add(new Region { DXCCPrefix="F",RegionCode="FR01",RegionName="Auvergne-Rhône-Alpes" });
                Regions.Add(new Region { DXCCPrefix="F",RegionCode="FR02",RegionName="Bourgogne-Franche-Comté" });
                Regions.Add(new Region { DXCCPrefix="F",RegionCode="FR03",RegionName="Bretagne (Brittany)" });
                Regions.Add(new Region { DXCCPrefix="F",RegionCode="FR04",RegionName="Centre-Val de Loire" });
                Regions.Add(new Region { DXCCPrefix="F",RegionCode="FR05",RegionName="Corse (Corsica)" });
                Regions.Add(new Region { DXCCPrefix="F",RegionCode="FR06",RegionName="Grand Est (Great East)" });
                Regions.Add(new Region { DXCCPrefix="F",RegionCode="FR07",RegionName="Hauts-de-France" });
                Regions.Add(new Region { DXCCPrefix="F",RegionCode="FR08",RegionName="Île-de-France" });
                Regions.Add(new Region { DXCCPrefix="F",RegionCode="FR09",RegionName="Normandie (Normandy)" });
                Regions.Add(new Region { DXCCPrefix="F",RegionCode="FR10",RegionName="Nouvelle-Aquitaine (New Aquitaine)" });
                Regions.Add(new Region { DXCCPrefix="F",RegionCode="FR11",RegionName="Occitanie" });
                Regions.Add(new Region { DXCCPrefix="F",RegionCode="FR12",RegionName="Pays de la Loire" });
                Regions.Add(new Region { DXCCPrefix="F",RegionCode="FR13",RegionName="Provence-Alpes-Côte d’Azur" });
                Regions.Add(new Region { DXCCPrefix="FY",RegionCode="FR14",RegionName="French Guiana" });
                Regions.Add(new Region { DXCCPrefix="F",RegionCode="FR15",RegionName="Guadeloupe" });
                Regions.Add(new Region { DXCCPrefix="F",RegionCode="FR16",RegionName="Martinique" });
                Regions.Add(new Region { DXCCPrefix="F",RegionCode="FR17",RegionName="Mayotte" });
                Regions.Add(new Region { DXCCPrefix="FR",RegionCode="FR18",RegionName="La Réunion" });
                Regions.Add(new Region { DXCCPrefix="F",RegionCode="FR19",RegionName="Saint Martin" });
                Regions.Add(new Region { DXCCPrefix="F",RegionCode="FR20",RegionName="France d’outre-mer (2)" });
                Regions.Add(new Region { DXCCPrefix="HA",RegionCode="HU01",RegionName="Northern Hungary" });
                Regions.Add(new Region { DXCCPrefix="HA",RegionCode="HU02",RegionName="Northern Great Plain" });
                Regions.Add(new Region { DXCCPrefix="HA",RegionCode="HU03",RegionName="Southern Great Plain" });
                Regions.Add(new Region { DXCCPrefix="HA",RegionCode="HU04",RegionName="Central Hungary" });
                Regions.Add(new Region { DXCCPrefix="HA",RegionCode="HU05",RegionName="Central Transdanubia" });
                Regions.Add(new Region { DXCCPrefix="HA",RegionCode="HU06",RegionName="Western Transdanubia" });
                Regions.Add(new Region { DXCCPrefix="HA",RegionCode="HU07",RegionName="Southern Transdanubia" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="IT01",RegionName="Abruzzo" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="IT02",RegionName="Aosta Valley" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="IT03",RegionName="Puglia" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="IT04",RegionName="Basilicata" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="IT05",RegionName="Calabria" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="IT06",RegionName="Campania" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="IT07",RegionName="Emilia-Romagna" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="IT08",RegionName="Friuli-Venezia Giulia" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="IT09",RegionName="Lazio" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="IT10",RegionName="Liguria" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="IT11",RegionName="Lombardia" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="IT12",RegionName="Marche" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="IT13",RegionName="Molise" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="IT14",RegionName="Piemonte" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="IT15",RegionName="Sardegna" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="IT16",RegionName="Sicilia" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="IT17",RegionName="Pantelleria/Lampedusa/Linosa, AF" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="IT18",RegionName="Trentino-Alto Adige" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="IT19",RegionName="Toscana" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="IT20",RegionName="Umbria" });
                Regions.Add(new Region { DXCCPrefix="I",RegionCode="IT21",RegionName="Veneto" });
                Regions.Add(new Region { DXCCPrefix="LX",RegionCode="LX01",RegionName="Luxembourg" });
                Regions.Add(new Region { DXCCPrefix="LY",RegionCode="LT01",RegionName="North (Šiauliai County, Panevėžys County)" });
                Regions.Add(new Region { DXCCPrefix="LY",RegionCode="LT02",RegionName="East (Vilnius County, Utena County)" });
                Regions.Add(new Region { DXCCPrefix="LY",RegionCode="LT03",RegionName="Central (Tauragė County, Kaunas County)" });
                Regions.Add(new Region { DXCCPrefix="LY",RegionCode="LT04",RegionName="West (Klaipėda County, Telšiai County)" });
                Regions.Add(new Region { DXCCPrefix="LY",RegionCode="LT05",RegionName="South ( Marijampolė County, Alytus County)" });
                Regions.Add(new Region { DXCCPrefix="LZ",RegionCode="BG01",RegionName="Severozapaden" });
                Regions.Add(new Region { DXCCPrefix="LZ",RegionCode="BG02",RegionName="Severen tsentralen" });
                Regions.Add(new Region { DXCCPrefix="LZ",RegionCode="BG03",RegionName="Severoiztochen" });
                Regions.Add(new Region { DXCCPrefix="LZ",RegionCode="BG04",RegionName="Yugoiztochen" });
                Regions.Add(new Region { DXCCPrefix="LZ",RegionCode="BG05",RegionName="Yugozapaden" });
                Regions.Add(new Region { DXCCPrefix="LZ",RegionCode="BG06",RegionName="Yuzhen tsentralen" });
                Regions.Add(new Region { DXCCPrefix="OE",RegionCode="AT01",RegionName="Vienna" });
                Regions.Add(new Region { DXCCPrefix="OE",RegionCode="AT02",RegionName="Salzburg" });
                Regions.Add(new Region { DXCCPrefix="OE",RegionCode="AT03",RegionName="Lower Austria" });
                Regions.Add(new Region { DXCCPrefix="OE",RegionCode="AT04",RegionName="Burgenland" });
                Regions.Add(new Region { DXCCPrefix="OE",RegionCode="AT05",RegionName="Upper Austria" });
                Regions.Add(new Region { DXCCPrefix="OE",RegionCode="AT06",RegionName="Styria" });
                Regions.Add(new Region { DXCCPrefix="OE",RegionCode="AT07",RegionName="Tyrol" });
                Regions.Add(new Region { DXCCPrefix="OE",RegionCode="AT08",RegionName="Carinthia" });
                Regions.Add(new Region { DXCCPrefix="OE",RegionCode="AT09",RegionName="Vorarlberg" });
                Regions.Add(new Region { DXCCPrefix="OH",RegionCode="FI01",RegionName="Lapland" });
                Regions.Add(new Region { DXCCPrefix="OH",RegionCode="FI02",RegionName="North Ostrobothnia" });
                Regions.Add(new Region { DXCCPrefix="OH",RegionCode="FI03",RegionName="Kainuu" });
                Regions.Add(new Region { DXCCPrefix="OH",RegionCode="FI04",RegionName="North Karelia" });
                Regions.Add(new Region { DXCCPrefix="OH",RegionCode="FI05",RegionName="North Savo" });
                Regions.Add(new Region { DXCCPrefix="OH",RegionCode="FI06",RegionName="South Savo" });
                Regions.Add(new Region { DXCCPrefix="OH",RegionCode="FI07",RegionName="South Karelia" });
                Regions.Add(new Region { DXCCPrefix="OH",RegionCode="FI08",RegionName="Central Finland" });
                Regions.Add(new Region { DXCCPrefix="OH",RegionCode="FI09",RegionName="South Ostrobothnia" });
                Regions.Add(new Region { DXCCPrefix="OH",RegionCode="FI10",RegionName="Ostrobothnia" });
                Regions.Add(new Region { DXCCPrefix="OH",RegionCode="FI11",RegionName="Central Ostrobothnia" });
                Regions.Add(new Region { DXCCPrefix="OH",RegionCode="FI12",RegionName="Pirkanmaa" });
                Regions.Add(new Region { DXCCPrefix="OH",RegionCode="FI13",RegionName="Satakunta" });
                Regions.Add(new Region { DXCCPrefix="OH",RegionCode="FI14",RegionName="Päijät-Häme" });
                Regions.Add(new Region { DXCCPrefix="OH",RegionCode="FI15",RegionName="Kanta-Häme" });
                Regions.Add(new Region { DXCCPrefix="OH",RegionCode="FI16",RegionName="Kymenlaakso" });
                Regions.Add(new Region { DXCCPrefix="OH",RegionCode="FI17",RegionName="Uusimaa" });
                Regions.Add(new Region { DXCCPrefix="OH",RegionCode="FI18",RegionName="Southwest Finland" });
                Regions.Add(new Region { DXCCPrefix="OH",RegionCode="FI19",RegionName="Åland" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="CZ01",RegionName="Prague" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="CZ02",RegionName="South Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="CZ03",RegionName="South Moravia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="CZ04",RegionName="Karlovy Vary" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="CZ05",RegionName="Hradec Králové" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="CZ06",RegionName="Liberec" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="CZ07",RegionName="Moravia-Silesia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="CZ08",RegionName="Olomouc" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="CZ09",RegionName="Pardubice" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="CZ10",RegionName="Plzeň" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="CZ11",RegionName="Central Bohemia" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="CZ12",RegionName="Ustí nad Labem" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="CZ13",RegionName="Zlín" });
                Regions.Add(new Region { DXCCPrefix="OL",RegionCode="CZ14",RegionName="Vysočina" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="SK01",RegionName="Bratislava" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="SK02",RegionName="Trnava" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="SK03",RegionName="Trenčín" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="SK04",RegionName="Nitra" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="SK05",RegionName="Žilina" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="SK06",RegionName="Banská Bystrica" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="SK07",RegionName="Prešov" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="SK08",RegionName="Košice" });
                Regions.Add(new Region { DXCCPrefix="OM",RegionCode="RO01",RegionName="Nord-Est development region" });
                Regions.Add(new Region { DXCCPrefix="ON",RegionCode="BE01",RegionName="Bruxelles-Capital Region" });
                Regions.Add(new Region { DXCCPrefix="ON",RegionCode="BE02",RegionName="Walloon Brabant" });
                Regions.Add(new Region { DXCCPrefix="ON",RegionCode="BE03",RegionName="Flemish Brabant" });
                Regions.Add(new Region { DXCCPrefix="ON",RegionCode="BE04",RegionName="Antwerp" });
                Regions.Add(new Region { DXCCPrefix="ON",RegionCode="BE05",RegionName="Limburg" });
                Regions.Add(new Region { DXCCPrefix="ON",RegionCode="BE06",RegionName="Liège" });
                Regions.Add(new Region { DXCCPrefix="ON",RegionCode="BE07",RegionName="Namur" });
                Regions.Add(new Region { DXCCPrefix="ON",RegionCode="BE08",RegionName="Hainaut" });
                Regions.Add(new Region { DXCCPrefix="ON",RegionCode="BE09",RegionName="Luxemburg" });
                Regions.Add(new Region { DXCCPrefix="ON",RegionCode="BE10",RegionName="West Flanders" });
                Regions.Add(new Region { DXCCPrefix="ON",RegionCode="BE11",RegionName="East Flanders" });
                Regions.Add(new Region { DXCCPrefix="OZ",RegionCode="DK01",RegionName="Region Hovedstaden" });
                Regions.Add(new Region { DXCCPrefix="OZ",RegionCode="DK02",RegionName="Region Midtjylland" });
                Regions.Add(new Region { DXCCPrefix="OZ",RegionCode="DK03",RegionName="Region Nordjylland" });
                Regions.Add(new Region { DXCCPrefix="OZ",RegionCode="DK04",RegionName="Region Sjælland" });
                Regions.Add(new Region { DXCCPrefix="OZ",RegionCode="DK05",RegionName="Region Syddanmark" });
                Regions.Add(new Region { DXCCPrefix="OZ",RegionCode="DK06",RegionName="Overseas Countries and Territories (1)" });
                Regions.Add(new Region { DXCCPrefix="PA",RegionCode="NL01",RegionName="Drenthe" });
                Regions.Add(new Region { DXCCPrefix="PA",RegionCode="NL02",RegionName="Flevoland" });
                Regions.Add(new Region { DXCCPrefix="PA",RegionCode="NL03",RegionName="Friesland" });
                Regions.Add(new Region { DXCCPrefix="PA",RegionCode="NL04",RegionName="Gelderland" });
                Regions.Add(new Region { DXCCPrefix="PA",RegionCode="NL05",RegionName="Groningen" });
                Regions.Add(new Region { DXCCPrefix="PA",RegionCode="NL06",RegionName="Limburg" });
                Regions.Add(new Region { DXCCPrefix="PA",RegionCode="NL07",RegionName="North Brabant" });
                Regions.Add(new Region { DXCCPrefix="PA",RegionCode="NL08",RegionName="North Holland" });
                Regions.Add(new Region { DXCCPrefix="PA",RegionCode="NL09",RegionName="Overijssel" });
                Regions.Add(new Region { DXCCPrefix="PA",RegionCode="NL10",RegionName="South Holland" });
                Regions.Add(new Region { DXCCPrefix="PA",RegionCode="NL11",RegionName="Utrecht" });
                Regions.Add(new Region { DXCCPrefix="PA",RegionCode="NL12",RegionName="Zeeland" });
                Regions.Add(new Region { DXCCPrefix="PA",RegionCode="NL13",RegionName="Overseas Countries and Territories (3)" });
                Regions.Add(new Region { DXCCPrefix="S5",RegionCode="SI01",RegionName="Primorska" });
                Regions.Add(new Region { DXCCPrefix="S5",RegionCode="SI02",RegionName="Osrednja Slovenija" });
                Regions.Add(new Region { DXCCPrefix="S5",RegionCode="SI03",RegionName="Koroška" });
                Regions.Add(new Region { DXCCPrefix="S5",RegionCode="SI04",RegionName="Štajerska" });
                Regions.Add(new Region { DXCCPrefix="S5",RegionCode="SI05",RegionName="Prekmurje" });
                Regions.Add(new Region { DXCCPrefix="S5",RegionCode="SI06",RegionName="Ljubljana mesto" });
                Regions.Add(new Region { DXCCPrefix="SM",RegionCode="SE01",RegionName="Stockholm" });
                Regions.Add(new Region { DXCCPrefix="SM",RegionCode="SE02",RegionName="Uppsala" });
                Regions.Add(new Region { DXCCPrefix="SM",RegionCode="SE03",RegionName="Södermanland" });
                Regions.Add(new Region { DXCCPrefix="SM",RegionCode="SE04",RegionName="Östergötland" });
                Regions.Add(new Region { DXCCPrefix="SM",RegionCode="SE05",RegionName="Jönköping" });
                Regions.Add(new Region { DXCCPrefix="SM",RegionCode="SE06",RegionName="Kronoberg" });
                Regions.Add(new Region { DXCCPrefix="SM",RegionCode="SE07",RegionName="Kalmar" });
                Regions.Add(new Region { DXCCPrefix="SM",RegionCode="SE08",RegionName="Gotland" });
                Regions.Add(new Region { DXCCPrefix="SM",RegionCode="SE09",RegionName="Blekinge" });
                Regions.Add(new Region { DXCCPrefix="SM",RegionCode="SE10",RegionName="Skåne" });
                Regions.Add(new Region { DXCCPrefix="SM",RegionCode="SE11",RegionName="Halland" });
                Regions.Add(new Region { DXCCPrefix="SM",RegionCode="SE12",RegionName="Västra Götaland" });
                Regions.Add(new Region { DXCCPrefix="SM",RegionCode="SE13",RegionName="Värmland" });
                Regions.Add(new Region { DXCCPrefix="SM",RegionCode="SE14",RegionName="Örebro" });
                Regions.Add(new Region { DXCCPrefix="SM",RegionCode="SE15",RegionName="Västmanland" });
                Regions.Add(new Region { DXCCPrefix="SM",RegionCode="SE16",RegionName="Dalarna" });
                Regions.Add(new Region { DXCCPrefix="SM",RegionCode="SE17",RegionName="Gävleborg" });
                Regions.Add(new Region { DXCCPrefix="SM",RegionCode="SE18",RegionName="Västernorrland" });
                Regions.Add(new Region { DXCCPrefix="SM",RegionCode="SE19",RegionName="Jämtland" });
                Regions.Add(new Region { DXCCPrefix="SM",RegionCode="SE20",RegionName="Västerbotten" });
                Regions.Add(new Region { DXCCPrefix="SM",RegionCode="SE21",RegionName="Norrbotten" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="PL01",RegionName="Greater Poland" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="PL02",RegionName="Kuyavia-Pomerania" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="PL03",RegionName="Lesser Poland" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="PL04",RegionName="Łódź" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="PL05",RegionName="Lower Silesia" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="PL06",RegionName="Lublin" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="PL07",RegionName="Lubusz" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="PL08",RegionName="Masovia" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="PL09",RegionName="Opole" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="PL10",RegionName="Podlaskie" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="PL11",RegionName="Pomerania" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="PL12",RegionName="Silesia" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="PL13",RegionName="Subcarpathia" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="PL14",RegionName="Holy Cross Province" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="PL15",RegionName="Warmia-Masuria" });
                Regions.Add(new Region { DXCCPrefix="SP",RegionCode="PL16",RegionName="West Pomerania" });
                Regions.Add(new Region { DXCCPrefix="SV",RegionCode="GR01",RegionName="Attica" });
                Regions.Add(new Region { DXCCPrefix="SV",RegionCode="GR02",RegionName="Central Greece" });
                Regions.Add(new Region { DXCCPrefix="SV",RegionCode="GR03",RegionName="Central Macedonia" });
                Regions.Add(new Region { DXCCPrefix="SV",RegionCode="GR04",RegionName="Crete" });
                Regions.Add(new Region { DXCCPrefix="SV",RegionCode="GR05",RegionName="Eastern Macedonia and Thrace" });
                Regions.Add(new Region { DXCCPrefix="SV",RegionCode="GR06",RegionName="Epirus" });
                Regions.Add(new Region { DXCCPrefix="SV",RegionCode="GR07",RegionName="Ionian Islands" });
                Regions.Add(new Region { DXCCPrefix="SV",RegionCode="GR08",RegionName="North Aegean" });
                Regions.Add(new Region { DXCCPrefix="SV",RegionCode="GR09",RegionName="Peloponnese" });
                Regions.Add(new Region { DXCCPrefix="SV",RegionCode="GR10",RegionName="South Aegean" });
                Regions.Add(new Region { DXCCPrefix="SV",RegionCode="GR11",RegionName="Thessaly" });
                Regions.Add(new Region { DXCCPrefix="SV",RegionCode="GR12",RegionName="Western Greece" });
                Regions.Add(new Region { DXCCPrefix="SV",RegionCode="GR13",RegionName="Western Macedonia" });
                Regions.Add(new Region { DXCCPrefix="YL",RegionCode="LV01",RegionName="Riga" });
                Regions.Add(new Region { DXCCPrefix="YL",RegionCode="LV02",RegionName="Kurzeme" });
                Regions.Add(new Region { DXCCPrefix="YL",RegionCode="LV03",RegionName="Latgale" });
                Regions.Add(new Region { DXCCPrefix="YL",RegionCode="LV04",RegionName="Zemgale" });
                Regions.Add(new Region { DXCCPrefix="YL",RegionCode="LV05",RegionName="Vidzeme" });
                Regions.Add(new Region { DXCCPrefix="YL",RegionCode="LV06",RegionName="Selija" });
                Regions.Add(new Region { DXCCPrefix="YO",RegionCode="RO01",RegionName="Nord-Est development region" });
                Regions.Add(new Region { DXCCPrefix="YO",RegionCode="RO02",RegionName="Sud-Est development region" });
                Regions.Add(new Region { DXCCPrefix="YO",RegionCode="RO03",RegionName="Sud – Muntenia development region" });
                Regions.Add(new Region { DXCCPrefix="YO",RegionCode="RO04",RegionName="Sud-Vest Oltenia development region" });
                Regions.Add(new Region { DXCCPrefix="YO",RegionCode="RO05",RegionName="Vest development region" });
                Regions.Add(new Region { DXCCPrefix="YO",RegionCode="RO06",RegionName="Nord-Vest development region" });
                Regions.Add(new Region { DXCCPrefix="YO",RegionCode="RO07",RegionName="Centru development region" });
                Regions.Add(new Region { DXCCPrefix="YO",RegionCode="RO08",RegionName="București – Ilfov development region" });
                break;
            }


            return Regions;


        }

            public static RegionMultiContainer GetRegionMultipliers(string region) {  // use Specifications Excel to create lines
            RegionMultiContainer regionMultis = new RegionMultiContainer();
            return regionMultis;
        }

        public static List<DXCCPrefixMultiplier> GetDXCCPrefixMultipliers(string region) {
            //https://www.ng3k.com/Dxcc/dxccsa.html    use Specifications Excel to build lines.

            List<DXCCPrefixMultiplier> regionMultis = new List<DXCCPrefixMultiplier>();
            switch(region) {
            case "EU_MEMBERS":
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="OE" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="ON" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="LZ" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="OK" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="5B" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="9A" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="OZ" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="ES" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="OH" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="F" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="DL" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="SV" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="HA" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="EI" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="I" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="YL" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="LY" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="LX" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="9H" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="PA" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="SP" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="CT" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="YO" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="OM" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="S5" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="EA" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="SM" });
                break;

            case "ADD_EUDX":

                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="OX" }); //DK06 

                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="FO" }); //FR20  French Polynesia
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="FK" }); //FR20  New Caledonia
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="FW" }); //FR20  Wallis-et-Futuna
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="FJ" }); //FR20   St Barthelemy
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="FP" }); //FR20  Saint-Pierre-et-Miquelon
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="xx" }); //FR20  French Southern and Antarctic Lands

                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="PJ2 " }); //NL13 Curaçao Overseas Countries and Territories   
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="P4 " }); //NL13 Aruba / Saba & Sint Eustatius Overseas Countries and Territories   
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="PJ5 " }); //NL13 Sint Maarten Overseas Countries and Territories   
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="PJ4 " }); //NL13 Bonaire Overseas Countries and Territories   

                break;

            case "FRENCH_TERRITORIES":
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="FG" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="FJ" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="FH" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="FK" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="FM" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="FO" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="FP" });

                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="FR" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="FT" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="FW" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="FY" });
                break;

            case "SA_DXCC":

                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="9Y" });                               //Trinidad & Tobago,11, 9	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="CE" });                               //Chile,14, 16, 12	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="CE0X" });                                 //San Felix,14, 12	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="CE0Y" });                                 //Easter Is,63, 12	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="CE0Z" });                                 //Juan Fernandez,14, 12	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="CP" });                               //Bolivia,12, 14, 10	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="CX" });                               //Uruguay,14, 13	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="FY" });                               //French Guiana,12, 9	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="HC" });                               //Ecuador,12, 10	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="HC8" });                                  //Galapagos Is,12, 10	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="HK" });                               //Colombia,12, 9	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="HK0" });                                  //Malpelo Is,12, 9	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="LU" });                               //Argentina,14, 16, 13	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="OA" });                               //Peru,12, 10	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="P4" });                               //Aruba,11, 9	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="PJ2" });                                  //Curacao,11, 9	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="PJ2*" });                                 //Neth Antilles,11, 9	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="PJ4" });                                  //Bonaire,11, 9	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="PY" });                               //Brazil,12, 13, 15, 11	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="PY0F" });                                 //Fernando de Noronha,13, 11	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="PY0P" });                                 //St Peter & St Paul Rocks,13, 11	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="PY0T" });                                 //Trindade & Martin Vaz Is,15, 11	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="PZ" });                               //Surinam,12, 9	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="VP8" });                                  //Falkland Is,16, 13	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="VP8" });                                  //So Georgia Is,73, 13	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="VP8" });                                  //So Orkney Is,73, 13	
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="VP8" });                                  //So Sandwich Is,73, 13	
                break;
            
            case "UK":
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="G" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="GD" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="GI" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="GJ" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="GM" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="GU" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="GW" });
                break;
            case "EA-MAJESTAD":
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="EA" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="EA6" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="EA8" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="EA9" });
                break;

            case "TISZA-CUP":
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="UR" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="YO" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="OM" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="HA" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="YU" });
                break;

            case "SCANDINAVIA":
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="JW" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="JX" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="LA" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="OH" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="OH0" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="OJ0" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="OX" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="OY" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="OZ" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="SM" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="TF" });
                break;

            case "WAE":
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="1A0" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="3A" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="4O" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="4U1I" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="4U1V" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="9A" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="9H" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="C3" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="CT" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="CU" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="DL" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="E7" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="EA" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="EA6" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="EI" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="ER" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="ES" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="EU" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="F" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="G" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="GD" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="GI" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="GJ" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="GM" });

                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="GU" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="GW" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="HA" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="HB" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="HB0" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="HV" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="I" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="IS" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="IT" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="JW" });

                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="JX" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="LA" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="LX" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="LY" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="LZ" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="OE" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="OH" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="OH0" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="OJ0" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="OK" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="OM" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="ON" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="OY" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="OZ" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="PA" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="R1F" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="RA" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="RA2" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="S5" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="SM" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="SP" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="SV" });

                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="SV5" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="SV9" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="T7" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="TA1" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="TF" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="TK" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="UR" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="UT" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="YL" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="YO" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="YU" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="Z6" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="Z3" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="ZA" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="ZB" });


                break;


            case "NA_DXCC_Except_K_VE":

                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="4U1U",Comment="United Nations (New York)" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="6Y",Comment="Jamaica" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="8P",Comment="Barbados" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="C6",Comment="Bahamas" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="CO",Comment="Cuba" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="CY0",Comment="Sable Island" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="CY9",Comment="St. Paul Island" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="FG",Comment="Guadeloupe" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="FJ",Comment="Saint Barthelemy" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="FM",Comment="Martinique" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="FO0",Comment="Clipperton Island" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="FP",Comment="Saint Pierre & Miquelon Island" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="FS",Comment="Saint Martin, Leeward Islands" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="HH",Comment="Haiti" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="HI",Comment="Dominican Republic" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="HK0B",Comment="Bajo Nuevo" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="HK0S",Comment="Serrana Bank & Roncador Cay" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="HK0-S",Comment="San Andres & Providencia" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="HP",Comment="Panama" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="HR",Comment="Honduras" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="J3",Comment="Grenada" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="J6",Comment="Saint Lucia" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="J7",Comment="Dominica" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="J8",Comment="St Vincent" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="KG4",Comment="Guantanamo Bay" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="KL7",Comment="Alaska" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="KP1",Comment="Navassa Island" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="KP2",Comment="U.S. Virgin Islands" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="KP4",Comment="Puerto Rico" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="KP5",Comment="Desecheo Island" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="KS4",Comment="Swan Island" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="KZ5",Comment="Canal Zone" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="OX",Comment="Greenland" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="PJ5",Comment="Saba" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="PJ5",Comment="St Eustatius Is." });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="PJ7",Comment="St Maarten" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="PJ8",Comment="Sint Maarten, Saba, Saint Eustatius" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="TG",Comment="Guatemala" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="TI",Comment="Costa Rica" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="TI9",Comment="Cocos Island" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="V2",Comment="Antigua and Barbuda" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="V3",Comment="Belize" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="V4",Comment="St Kitts and Nevis Islands" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="VO",Comment="Newfoundland and Labrador" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="VP2-E",Comment="Anguilla" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="VP2-M",Comment="Montserrat" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="VP2-V",Comment="British Virgin Islands" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="VP5",Comment="Turks and Caicos Islands" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="VP9",Comment="Bermuda" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="XE",Comment="Mexico" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="XF4",Comment="Revillagigedo Archipelago" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="YN",Comment="Nicaragua" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="YS",Comment="El Salvador" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="YV0",Comment="Aves Island" });
                regionMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="ZF",Comment="Cayman Islands" });


                break;
            }
            return regionMultis;
        }


    }
}

