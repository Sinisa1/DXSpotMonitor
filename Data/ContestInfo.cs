using Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Data {
    public enum ContestExchangeType { // Expand this to make DXCC specific - ARI contest.
        SERIAL,
        CQZ,
        ITUZ,
        REGION_CONT,  // REGION/MUNICIPALITY/STATE/PROVINCE/OTHER FIXED CODES
        DEFAULT

    }
    public class ContestInfo {
        public LogContestSettings ContestSettings = new LogContestSettings();

        // These simplify references from inner objects
        public bool MatchBand { get { return ContestSettings.MatchBand; } }
        public bool MatchMode { get { return ContestSettings.MatchMode; } }
        public ContestExchangeType ContestExchangeType = ContestExchangeType.SERIAL;
        public bool SerialRx1Mandatory = true;
        public bool SerialRx2Mandatory = true; //  Active only if SerialTx2Enabled=true. Some contest may have SRX/STX2 but it is not mandatory RSGB_IOTA
        public bool SerialTX1AutoIncrement { get { return (ContestSettings.SerialTX1AutoIncrement>0); } }
        public bool SerialTx2Enabled { get { return ContestSettings.SerialTx2Enabled; } }
        public bool SerialTX2AutoIncrement { get { return (ContestSettings.SerialTX2AutoIncrement>0); } }
        public double ContestExpiryMonthsOverride { get { return ContestSettings.ContestExpiryMonthsOverride; } }

        public string SerialTX2Value { get { return ContestSettings.SerialTX2Value; } }
        public string SerialTX1Value { get { return ContestSettings.SerialTX1Value; } }

        public ContestEnum Code = ContestEnum.IGNORE; //   CQ_WW_DX_CW   Dashes cannot be


        public string N1MMContestCode = "";  // Use for ADIF export for N1MM scoring. If contest definition does not exist, then import contest definition: https://n1mmwp.hamdocs.com/manual-supported/contests-setup/setup-udc-contests/
        public string CabrilloCode { get; set; }// Populate with initial velue when instantated if there is override CRO_9A-DX. If empty, convert enum.
        public List<MultiType> GetContestMultiTypeZone() { // This is probably redundant. Take the values from contest definition.
            List<MultiType> mtList = new List<MultiType>();
            MultiType mt1 = MultiType.IGNORE;
            MultiType mt2 = MultiType.IGNORE;
            switch (Code) {
                case ContestEnum.CQ_WPX_CW:
                case ContestEnum.CQ_WPX_SSB:
                    mt1=MultiType.PFX;
                    break;
                case ContestEnum.AADX_CW:   // Asian PFX only
                    mt1=MultiType.PFX_CONTINENT;
                    break;
                case ContestEnum.CQ_WW_CW:
                case ContestEnum.CQ_WW_SSB:
                    mt1=MultiType.DXCC;
                    mt2=MultiType.CQZ;
                    break;
                default: // Try to automate. If too complex , hard code for important contests
                    // DXCC First, then CQZ, ITUZ     CQ WW has DXCC and CQZ
                    ContestMultiplier multi = Multipliers.Where(x => x.MtType==MultiType.DXCC).FirstOrDefault();
                    if (multi!=null) {
                        mt1=multi.MtType;
                    } else {
                        mt1=MultiType.IGNORE;
                    }

                    multi=Multipliers.Where(x => x.MtType==MultiType.CQZ||x.MtType==MultiType.ITUZ).FirstOrDefault();
                    if (multi!=null) {
                        mt2=multi.MtType;

                    } else {
                        mt2=MultiType.IGNORE;
                    }
                    break;
            }
            mtList=new List<MultiType> { mt1, mt2 };
            return mtList;
        }

        public void SetContestCode(string cabrilloCode) {
            string codeCabOver = cabrilloCode;
            Code=CabriloToEnum(codeCabOver);
        }
        public static ContestEnum CabriloToEnum(string cabrillo) {
            return Util.ParseEnum<ContestEnum>(cabrillo, true);
        }

        public Schedule Schedule { get; set; }

        public string EnumCodeToCabrillo() {
            return Code.ToString().Replace("_", "-"); //CQ_WW_DX_CW => CQ-WW-DX-CW           
        }
        public string EnumCodeToCabrillo(ContestEnum code) {
            return code.ToString().Replace("_", "-"); //CQ_WW_DX_CW => CQ-WW-DX-CW           
        }
        public void IncrementSerialNumbers(bool ContestMode) {
            ContestSettings.IncrementSerialNumbers(ContestMode);

        }
        public List<ContestMultiplier> Multipliers { get; set; } // 20, 15
        public ContestInfo() {
            CabrilloCode=""; // if empty, override with enum Code to string
            Schedule=new Schedule { Month=99, Week=0 };// If undefined, push to the bottom of the list
        }

        public bool anyMultiCQZ { get { return Multipliers.Any(x => x.MtType== MultiType.CQZ); } }
        public bool anyMultiITUZ { get { return Multipliers.Any(x => x.MtType== MultiType.ITUZ); } }

        public int GetContestexpiryHours() {
            int expHours = Schedule.ExpiryHours;
            if (ContestExpiryMonthsOverride>0.0) {
                expHours=int.Parse(Math.Round(30.0*24.0*ContestExpiryMonthsOverride, 0.0).ToString());
            }
            return expHours;
        }

        public string GetMultiplierDescriptions() {
            string desc = "";
            foreach (ContestMultiplier multi in Multipliers) { // 
                desc+=(multi.CodeDescription.Length==0 ? multi.MtType.ToString() : multi.CodeDescription)+", "; // If custom descr is missing, just put the type
            }
            if (desc.Length>0) {
                desc=desc.Substring(0, desc.Length-2);
            } else {
                desc="Multis undefined";
            }
            return desc;
        }
        public static List<ContestInfo> PopulateContestInfoList() {
            //
            // ITUZ, CQZ and DXCC are taken from Perl country file
            // WPX is taken from Perl file - Prefix
            //SRX is usually province/state or district. It is taken from search Call: space SRX
            ContestMultiplier contestMultiplier = null;
            List<ContestMultiplier> multiList = new List<ContestMultiplier>();
            List<ContestInfo> contInfo = new List<ContestInfo>();
            List<DXCCPrefixMultiplier> DxccMultis = null;
            RegionMultiContainer regMultiContainer = new RegionMultiContainer();

            List<SpecialCallMultiplier> specialCallMultis = new List<SpecialCallMultiplier>();
            List<Region> allDXCCRegions = new List<Region>();

            contInfo.Add(new ContestInfo { Code=ContestEnum.IGNORE, Multipliers=new List<ContestMultiplier>(), Schedule=new Schedule { Month=0, Week=0 } }); //Dummy contest for normal QSOs

            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // CONTESTS WITH IRREGULAR SCHEDULES
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=false });
            DxccMultis=MultiWrapper.GetDXCCPrefixMultipliers("UK");
            DxccMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="EI" });
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION, CodeSrc=QSOItemCodes.SRX2, Band=true, Mode=false, DXCCPrefixMultipliers=DxccMultis, CodeDescription="DIstrict" });
            contInfo.Add(new ContestInfo { Code=ContestEnum.UKEI_DX_CW, Multipliers=multiList }); //  Various times - Januar to May
            contInfo.Add(new ContestInfo { Code=ContestEnum.UKEI_DX_SSB, Multipliers=multiList }); //  Various times

            contInfo.Add(new ContestInfo { Code=ContestEnum.WIA_AUSTRALIADAY, Multipliers=new List<ContestMultiplier>(), Schedule=new Schedule { Month=1, Week=4 } }); // RST + GRID FN03 


            /////


            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.SRX, Band=true, Mode=false, CodeDescription="Age" });  //SRX is Age
            contInfo.Add(new ContestInfo { Code=ContestEnum.YOTA, Multipliers=multiList, Schedule=new Schedule { Month=7, Week=4 } }); //  May also

            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.SRX, Band=true, Mode=false, CodeDescription="Age" });  //SRX is Age
            contInfo.Add(new ContestInfo { Code=ContestEnum.YOTA, Multipliers=multiList, Schedule=new Schedule { Month=4, Week=3 } }); //  May also

            contInfo.Add(new ContestInfo { Code=ContestEnum.CWOPS_OPEN, Multipliers=new List<ContestMultiplier>(), Schedule=new Schedule { Month=9, Week=1, ExpiryHours=5 } }); // 3 periods of 4 hours.  



            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // QSO PARTIES
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++


            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            regMultiContainer=new RegionMultiContainer();
            regMultiContainer.PopulateRegionListByContest(ContestEnum.FCG_FQP, "County");

            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION, Band=false, Mode=true, RegMultiContainer=regMultiContainer });
            contInfo.Add(new ContestInfo { Code=ContestEnum.FCG_FQP, Multipliers=multiList, Schedule=new Schedule { Month=4, Week=4 }, N1MMContestCode="FL-QSO-PARTY" });

            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            regMultiContainer=new RegionMultiContainer();
            regMultiContainer.PopulateRegionListByContest(ContestEnum.ON_QSO_PARTY,"County");
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION,Band=false,Mode=true,RegMultiContainer=regMultiContainer });
            contInfo.Add(new ContestInfo { Code=ContestEnum.ON_QSO_PARTY,Multipliers=multiList,Schedule=new Schedule { Month=4,Week=3 },N1MMContestCode="ON-QSO-PARTY" });



            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // UNFINISHED CONTESTS //TO FIX
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++


            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION, CodeSrc=QSOItemCodes.SRX, Band=true, Mode=false, DXCCPrefixMultipliers=DxccMultis, CodeDescription="Mix/Ignore" });
            contInfo.Add(new ContestInfo { Code=ContestEnum.QRS_10, Multipliers=multiList, Schedule=new Schedule { Month=1, Week=1 } }); // Monthly contest. Multis brazilian states and DXCC https://hampass.org/mqrs10

            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // CONTESTS WITH REGULAR SCHEDULES
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++


            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { CodeSrc=QSOItemCodes.DXCC, Band=true, Mode=false });
            DxccMultis=new List<DXCCPrefixMultiplier> { new DXCCPrefixMultiplier { DXCCPrefix="YB" } };
            multiList.Add(new ContestMultiplier { MtType=MultiType.PFX, Band=true, Mode=false, DXCCPrefixMultipliers=DxccMultis });
            contInfo.Add(new ContestInfo { Code=ContestEnum.YB_DX_CONTEST, Multipliers=multiList, Schedule=new Schedule { Month=1, Week=2 } }); //  YB Pfx are multis


            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION, CodeSrc=QSOItemCodes.SRX, Band=true, Mode=false, CodeDescription="PMC Ref" });
            contInfo.Add(new ContestInfo { Code=ContestEnum.PMC, Multipliers=multiList, Schedule=new Schedule { Month=1, Week=1 } });

    

            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            
            regMultiContainer=new RegionMultiContainer();
            regMultiContainer.PopulateRegionListByDXCC(new List<Tuple<string, string>> { new Tuple<string, string>("HA", "County") });
           
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION, Band=true, Mode=true, RegMultiContainer=regMultiContainer });
           
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=false, ExcludedDXCCPrefixMultipliers=new List<DXCCPrefixMultiplier> { new DXCCPrefixMultiplier ("HA") } });
            contInfo.Add(new ContestInfo { Code=ContestEnum.HA_DX, Multipliers=multiList, Schedule=new Schedule { Month=1, Week=3 }, N1MMContestCode="HA-DX" });




            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            DxccMultis=new List<DXCCPrefixMultiplier> { new DXCCPrefixMultiplier { DXCCPrefix="F" } };
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION, Band=true, Mode=false, DXCCPrefixMultipliers=DxccMultis, CodeDescription="F Depts" });// F departments
            DxccMultis=MultiWrapper.GetDXCCPrefixMultipliers("FRENCH_TERRITORIES");
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION, Band=true, Mode=false, DXCCPrefixMultipliers=DxccMultis, CodeSrc=QSOItemCodes.SRX, CodeDescription="Fr Ter PFX" }); // Overseas prefixes.  Prefixes sent as SRX

            contInfo.Add(new ContestInfo { Code=ContestEnum.REF_CW, Multipliers=multiList, Schedule=new Schedule { Month=1, Week=4 } }); //  


            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            
            regMultiContainer=new RegionMultiContainer();

            regMultiContainer.PopulateRegionListByContest(ContestEnum.EUDXC,"Region");
           
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION_LIST, Band=true, Mode=false, RegMultiContainer=regMultiContainer });
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=false });

            contInfo.Add(new ContestInfo { Code=ContestEnum.EUDXC, Multipliers=multiList, Schedule=new Schedule { Month=2, Week=1 } });




            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------            
            regMultiContainer=new RegionMultiContainer();
            regMultiContainer.PopulateRegionListByDXCC(new List<Tuple<string, string>> { new Tuple<string, string>("PA", "Prov.") });
           
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION_LIST, Band=true, Mode=true, RegMultiContainer=regMultiContainer });
            contInfo.Add(new ContestInfo { Code=ContestEnum.PACC, Multipliers=multiList, Schedule=new Schedule { Month=2, Week=2 }, N1MMContestCode="PACC" });



            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=true ,ExcludedDXCCPrefixMultipliers=new List<DXCCPrefixMultiplier> { new DXCCPrefixMultiplier("VE"),new DXCCPrefixMultiplier("K") } });
            contInfo.Add(new ContestInfo { Code=ContestEnum.ARRL_DX_CW, Multipliers=multiList, Schedule=new Schedule { Month=2, Week=3 } }); // dx only no ve/usa

            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            DxccMultis=MultiWrapper.GetDXCCPrefixMultipliers("EU_MEMBERS");
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=false, DXCCPrefixMultipliers=DxccMultis }); // Only Union countries count.
            DxccMultis=new List<DXCCPrefixMultiplier> { new DXCCPrefixMultiplier { DXCCPrefix="ON" } }; // Belgium
            multiList.Add(new ContestMultiplier { MtType=MultiType.PFX, Band=true, Mode=false, DXCCPrefixMultipliers=DxccMultis });
            contInfo.Add(new ContestInfo { Code=ContestEnum.UBA_DX_CW, Multipliers=multiList, Schedule=new Schedule { Month=2, Week=4 } });

            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.SRX_REGION, CodeSrc=QSOItemCodes.SRX, Band=true, Mode=true, DXCCPrefixMultipliers=new List<DXCCPrefixMultiplier>(), CodeDescription="Grid SQ." });//Grid squares
            contInfo.Add(new ContestInfo { Code=ContestEnum.WWPDX, Multipliers=multiList, Schedule=new Schedule { Month=2, Week=4 } }); //  World Wide Patagonia DX Contest

            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=true });
            contInfo.Add(new ContestInfo { Code=ContestEnum.ARRL_DX_SSB, Multipliers=multiList, Schedule=new Schedule { Month=3, Week=1 } }); // dx only no ve/usa


            //////                                                                                                                                  /////
            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            DxccMultis=MultiWrapper.GetDXCCPrefixMultipliers("SA_DXCC");
            multiList.Add(new ContestMultiplier { MtType=MultiType.PFX, CodeSrc=QSOItemCodes.PFX, Band=true, Mode=false, DXCCPrefixMultipliers=DxccMultis }); // SA prefixes.  Prefixes sent as SRX
            multiList.Add(new ContestMultiplier { MtType=MultiType.CQZ, CodeSrc=QSOItemCodes.SRX, Band=true, Mode=false }); // Take CQZ from SRX, not from lookup

            contInfo.Add(new ContestInfo { Code=ContestEnum.SA10M, Multipliers=multiList, Schedule=new Schedule { Month=3, Week=2 }, N1MMContestCode="SA10_DX" });

            contInfo.Add(new ContestInfo { Code=ContestEnum.TESLA_HF, Multipliers=new List<ContestMultiplier>(), Schedule=new Schedule { Month=3, Week=2 } }); //
            contInfo.Add(new ContestInfo { Code=ContestEnum.RSGB_COMMONWEALTH, Multipliers=new List<ContestMultiplier>(), Schedule=new Schedule { Month=3, Week=2 }, N1MMContestCode="RSGBBERU" }); // BERU CW, No multis
            contInfo.Add(new ContestInfo { Code=ContestEnum.ALL_AFRICA, Multipliers=new List<ContestMultiplier>(), Schedule=new Schedule { Month=3, Week=3 } }); //complex multis

            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC,Band=true,Mode=true });
            regMultiContainer=new RegionMultiContainer();
            regMultiContainer.PopulateRegionListByDXCC(new List<Tuple<string,string>> { new Tuple<string,string>("UA","Oblast"),new Tuple<string,string>("UA0","Oblast"),new Tuple<string,string>("UA2","Oblast") });
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION_LIST,Band=true,Mode=true,RegMultiContainer=regMultiContainer });
            contInfo.Add(new ContestInfo { Code=ContestEnum.RDXC,Multipliers=multiList,Schedule=new Schedule { Month=3,Week=3 } }); //

            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.PFX, Band=false, Mode=false, CodeDescription="PFX" }); // Exch: Serial, Extract PFX from station call // Pfx Multi only once(any band)
            contInfo.Add(new ContestInfo { Code=ContestEnum.CQ_WPX_SSB, N1MMContestCode="CQWPXSSB", Multipliers=multiList, Schedule=new Schedule { Month=3, Week=4 } });

            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.ITUZ, Band=true, Mode=false });
            specialCallMultis=new List<SpecialCallMultiplier>();
            specialCallMultis.AddRange(SpecialCallMultiplier.GetSpecialCallMultipliers(ContestEnum.GC));
            multiList.Add(new ContestMultiplier { MtType=MultiType.SPECIAL_CALLS, Band=true, Mode=true, SpecialCallMultipliers=specialCallMultis, CodeDescription="Spec Station" }); // Special calls send codes as SRX 
            contInfo.Add(new ContestInfo { Code=ContestEnum.GC, Multipliers=multiList, Schedule=new Schedule { Month=4, Week=2 } }); // GAGARIN-CUP Yuri Gagarin International DX Contest

            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            DxccMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="JA" });
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION, Band=true, Mode=false, DXCCPrefixMultipliers=DxccMultis, CodeDescription="Prefecture" });
            contInfo.Add(new ContestInfo { Code=ContestEnum.JIDX_CW, Multipliers=multiList, Schedule=new Schedule { Month=4, Week=2 } });


            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            
            regMultiContainer=new RegionMultiContainer();
            regMultiContainer.PopulateRegionListByContest(ContestEnum.SPDX, "Prov");

           
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION, Band=true, Mode=true, RegMultiContainer=regMultiContainer });
            contInfo.Add(new ContestInfo { Code=ContestEnum.SPDX, Multipliers=multiList, Schedule=new Schedule { Month=4, Week=1 } });

        

            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            DxccMultis=MultiWrapper.GetDXCCPrefixMultipliers("SA_DXCC"); //CQMMDX  CW
            multiList.Add(new ContestMultiplier { MtType=MultiType.PFX, Band=true, Mode=true, DXCCPrefixMultipliers=DxccMultis }); // SA prefixes. 
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=false, Mode=false }); // DXCC Once only
            contInfo.Add(new ContestInfo { Code=ContestEnum.CQMMDX, Multipliers=multiList, Schedule=new Schedule { Month=4, Week=3 } });


            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC,Band=true,Mode=true }); // SWISS CANTONS - UPDATE MULTIS
            contInfo.Add(new ContestInfo { Code=ContestEnum.HELVETIA,Multipliers=multiList,Schedule=new Schedule { Month=4,Week=4 } }); //Serial

            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC,Band=false,Mode=true });   // Multi:DXCC Once only, CW Only
            contInfo.Add(new ContestInfo { Code=ContestEnum.SMMC,Multipliers=multiList,Schedule=new Schedule { Month=4,Week=4 } }); //ITUZ Samuel Morse Memorial Contest: 1400Z-2000Z, Apr 26

            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            DxccMultis=new List<DXCCPrefixMultiplier> { new DXCCPrefixMultiplier { DXCCPrefix="LZ" } };
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION,CodeSrc=QSOItemCodes.SRX,Band=true,Mode=false,DXCCPrefixMultipliers=DxccMultis,CodeDescription="LZ District" });  //SRX LZ: district, Others ITU
            multiList.Add(new ContestMultiplier { MtType=MultiType.ITUZ,Band=true,Mode=false,ExcludedDXCCPrefixMultipliers={ new DXCCPrefixMultiplier { DXCCPrefix="LZ" } } });  // !!! EXCLUDE LZ !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            contInfo.Add(new ContestInfo { Code=ContestEnum.LZ_DX,Multipliers=multiList,Schedule=new Schedule { Month=11,Week=3 } });




            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            
            regMultiContainer=new RegionMultiContainer();
            regMultiContainer.PopulateRegionListByDXCC(new List<Tuple<string, string>> { new Tuple<string, string>("I", "Province"), new Tuple<string, string>("IS0", "Province") });
           
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION, Band=true, Mode=true, RegMultiContainer=regMultiContainer });

            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=false, ExcludedDXCCPrefixMultipliers=new List<DXCCPrefixMultiplier> { new   DXCCPrefixMultiplier("HA") } });
            contInfo.Add(new ContestInfo { Code=ContestEnum.ARI_DX, Multipliers=multiList, Schedule=new Schedule { Month=5, Week=1 } });





            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=true }); // Russian 150 list similar to DXCC
            contInfo.Add(new ContestInfo { Code=ContestEnum.CQ_M, Multipliers=multiList, Schedule=new Schedule { Month=5, Week=2 } }); //Serial
            // =======

            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=false }); // Each EADX100 entity once per band similar to DXCC

            DxccMultis=MultiWrapper.GetDXCCPrefixMultipliers("EA-MAJESTAD");
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION, Band=true, Mode=false, DXCCPrefixMultipliers=DxccMultis, CodeDescription="EA Prov." });

            specialCallMultis=new List<SpecialCallMultiplier>();
            specialCallMultis.AddRange(SpecialCallMultiplier.GetSpecialCallMultipliers(ContestEnum.EA_MAJESTAD_CW));
            multiList.Add(new ContestMultiplier { MtType=MultiType.SPECIAL_CALLS, Band=true, Mode=true, SpecialCallMultipliers=specialCallMultis, CodeDescription="SpecCall" }); // Special calls send codes as SRX 

            contInfo.Add(new ContestInfo { Code=ContestEnum.EA_MAJESTAD_CW, Multipliers=multiList, Schedule=new Schedule { Month=5, Week=3 }, N1MMContestCode="KINGEACW" }); //His Maj. King of Spain Contest, CW
            // =======                                                                                                                                                       

            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=false });
            DxccMultis=new List<DXCCPrefixMultiplier> { new DXCCPrefixMultiplier { DXCCPrefix="UN" } };
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION, Band=true, Mode=false, DXCCPrefixMultipliers=DxccMultis, CodeDescription="District" });
            contInfo.Add(new ContestInfo { Code=ContestEnum.UN_DX, Multipliers=multiList, Schedule=new Schedule { Month=5, Week=3 } }); //RST


            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.PFX, Band=false, Mode=false, CodeDescription="PFX" }); // Exch: Serial, Extract PFX from station call // Pfx Multi only once(any band)
            contInfo.Add(new ContestInfo { Code=ContestEnum.CQ_WPX_CW, Multipliers=multiList, Schedule=new Schedule { Month=5, Week=4 }, N1MMContestCode="CQWPXCW" });

            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            DxccMultis=MultiWrapper.GetDXCCPrefixMultipliers("TISZA-CUP");
            multiList.Add(new ContestMultiplier { MtType=MultiType.PFX, Band=true, Mode=false, CodeDescription="Tisza PFX", DXCCPrefixMultipliers=DxccMultis }); // TISZACUP prefixes. 
            multiList.Add(new ContestMultiplier { MtType=MultiType.CQZ, Band=true, Mode=false });
            contInfo.Add(new ContestInfo { Code=ContestEnum.TISZA_CUP, ContestExchangeType= ContestExchangeType.CQZ, Multipliers=multiList, Schedule=new Schedule { Month=6, Week=1 }  });


            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST --------------------------------- // NOT IN THE CONTEST LIST, BUT WORKED 2022 JUNE, 5
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=false });
            contInfo.Add(new ContestInfo { Code=ContestEnum.IARU_FD_R1_DARC_CW, Multipliers=multiList, Schedule=new Schedule { Month=6, Week=1 } });


            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            
            regMultiContainer=new RegionMultiContainer();
            regMultiContainer.PopulateRegionListByDXCC(new List<Tuple<string, string>> { new Tuple<string, string>("CT", "District"), new Tuple<string, string>("CT3", "County"), new Tuple<string, string>("CU", "County") });
           
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION, Band=true, Mode=false, RegMultiContainer=regMultiContainer });
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=false });

            contInfo.Add(new ContestInfo { Code=ContestEnum.PORTUGAL_DAY, Multipliers=multiList, Schedule=new Schedule { Month=6, Week=2 } });



            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.CQZ, Band=true, Mode=true });
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=true });
            contInfo.Add(new ContestInfo { Code=ContestEnum.GACW, Multipliers=multiList, Schedule=new Schedule { Month=6, Week=2 } });

            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.SRX_REGION, CodeSrc=QSOItemCodes.SRX, Band=true, Mode=true, DXCCPrefixMultipliers=new List<DXCCPrefixMultiplier>(), CodeDescription="Grid SQ." });//Grid squares
            contInfo.Add(new ContestInfo { Code=ContestEnum.ARRL_VHF_JUN, Multipliers=multiList, Schedule=new Schedule { Month=6, Week=2 } }); //  World Wide Patagonia DX Contest


            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            contestMultiplier=new ContestMultiplier { CodeSrc=QSOItemCodes.PFX, MtType=MultiType.PFX_CONTINENT, Band=true, Mode=true, ContinentMultipliers=new List<ContinentMultiplier> { new ContinentMultiplier { Continent="AS" } } };
            multiList.Add(contestMultiplier); //All PFX from AS continent
            contInfo.Add(new ContestInfo { Code=ContestEnum.AADX_CW ,ContestExchangeType= ContestExchangeType.DEFAULT, Multipliers=multiList, Schedule=new Schedule { Month=6, Week=2 } }); //complex multis Asia PFX . Exchange is age - DEFAULT do not add zeroes on STX



            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
             contInfo.Add(new ContestInfo { Code=ContestEnum.ARRL_FD,ContestExchangeType= ContestExchangeType.DEFAULT,Multipliers=multiList,Schedule=new Schedule { Month=6,Week=4 } }); // Exchange 1D GH

            

            multiList =new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            
            regMultiContainer=new RegionMultiContainer();
            regMultiContainer.PopulateRegionListByDXCC(new List<Tuple<string, string>> { new Tuple<string, string> ( "VE", "Prov"), new Tuple<string, string>("XE", "State") });

           
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION, Band=true, Mode=true, RegMultiContainer=regMultiContainer });
            contInfo.Add(new ContestInfo { Code=ContestEnum.CANADA_DAY, ContestExchangeType= ContestExchangeType.REGION_CONT,   Multipliers=multiList, Schedule=new Schedule { Month=7, Week=1 } , N1MMContestCode="RAC"});

            // different cabrillo for submission:   See 2026 log  QSO: 50     CW 2026-07-04 1557 VE3ZR               FN03 W4ATL               EM74

            contInfo.Add(new ContestInfo { Code=ContestEnum.CQ_VHF_SSBCW,ContestExchangeType= ContestExchangeType.DEFAULT,Multipliers=new List<ContestMultiplier>(),Schedule=new Schedule { Month=7,Week=1 } });



            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=false });
            contInfo.Add(new ContestInfo { Code=ContestEnum.MMC_HF_CW, Multipliers=multiList, Schedule=new Schedule { Month=7, Week=1 }, N1MMContestCode="MARCONIMEM" }); // Marconi Memorial HF Contest
          
            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=false }); // Add YV regions multiplier
            contInfo.Add(new ContestInfo { Code=ContestEnum.VENEZUELA_IND_DAY, Multipliers=multiList, Schedule=new Schedule { Month=7, Week=1 } }); // Marconi Memorial HF Contest


            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.ITUZ, Band=true, Mode=false });
            specialCallMultis=new List<SpecialCallMultiplier>();
            specialCallMultis.AddRange(SpecialCallMultiplier.GetSpecialCallMultipliers(ContestEnum.IARU_HF));
            multiList.Add(new ContestMultiplier { MtType=MultiType.SPECIAL_CALLS, Band=true, Mode=true, SpecialCallMultipliers=specialCallMultis });
            contInfo.Add(new ContestInfo { Code=ContestEnum.IARU_HF,  ContestExchangeType=  ContestExchangeType.ITUZ,  Multipliers=multiList, Schedule=new Schedule { Month=7, Week=2 } });

            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            
            regMultiContainer=new RegionMultiContainer();



            regMultiContainer=new RegionMultiContainer();
            regMultiContainer.PopulateRegionListByDXCC(new List<Tuple<string, string>> { new Tuple<string, string>("PY", "State") });
           
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION, Band=true, Mode=false, RegMultiContainer=regMultiContainer });

            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=false, ExcludedDXCCPrefixMultipliers=new List<DXCCPrefixMultiplier> { new DXCCPrefixMultiplier("HA") } });
            contInfo.Add(new ContestInfo { Code=ContestEnum.LABRE_DX, Multipliers=multiList, Schedule=new Schedule { Month=7, Week=3 }, N1MMContestCode="LABRE" }); //RST



            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.SRX2, Band=true, Mode=false, CodeDescription="IOTA SRX2"  });  //SRX is Age
            contInfo.Add(new ContestInfo { Code=ContestEnum.RSGB_IOTA, SerialRx2Mandatory=false,   Multipliers=multiList, Schedule=new Schedule { Month=7, Week=4 } }); //  May also

            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            
            regMultiContainer=new RegionMultiContainer();
            regMultiContainer.PopulateRegionListByDXCC(new List<Tuple<string, string>> { new Tuple<string, string>("VE", "Prov"), new Tuple<string, string>("K", "State") });

           
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION, CodeSrc= QSOItemCodes.SRX2, Band=true, Mode=true, RegMultiContainer=regMultiContainer });

            DxccMultis=MultiWrapper.GetDXCCPrefixMultipliers("NA_DXCC_Except_K_VE");
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=true, DXCCPrefixMultipliers=DxccMultis, ExcludedDXCCPrefixMultipliers= new List<DXCCPrefixMultiplier> { new DXCCPrefixMultiplier( "K"), new DXCCPrefixMultiplier("VE") },    CodeDescription="NA DXCC" });
            contInfo.Add(new ContestInfo { Code=ContestEnum.NAQP_CW, N1MMContestCode="NAQPCW", Multipliers=multiList, Schedule=new Schedule { Month=8, Week=1 } }); //


            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            DxccMultis=MultiWrapper.GetDXCCPrefixMultipliers("WAE");
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=true, CodeDescription="WAE Cntry", DXCCPrefixMultipliers=DxccMultis });
            contInfo.Add(new ContestInfo { Code=ContestEnum.DARC_WAEDC_CW, Multipliers=multiList, Schedule=new Schedule { Month=8, Week=2 } });


            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            contInfo.Add(new ContestInfo { Code=ContestEnum.RDAC, Multipliers=multiList, Schedule=new Schedule { Month=8, Week=2 } }); // make a new list RDA Districts. https://rdaward.org/rda_eng.txt

    
            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            
            regMultiContainer=new RegionMultiContainer();
            regMultiContainer.PopulateRegionListByDXCC(new List<Tuple<string, string>> { new Tuple<string, string>("YO", "County") });
           
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION,Band=true,Mode=true,RegMultiContainer=regMultiContainer });
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=false });
            contInfo.Add(new ContestInfo { Code=ContestEnum.YO_DX_HF, Multipliers=multiList, ContestExchangeType= ContestExchangeType.SERIAL, Schedule=new Schedule { Month=8, Week=4 } });

      
            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            DxccMultis=MultiWrapper.GetDXCCPrefixMultipliers("SCANDINAVIA");
            //         multis.Add(new ContestMultiplier { MtType=MultiType.PFX_NUMBER, CodeSrc=QSOItemCodes.SRX, Band=true, Mode=false, DXCCPrefixMultipliers=DxccMultis, CodeDescription="Scan DXCC" });
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, CodeSrc=QSOItemCodes.SRX, Band=true, Mode=true, DXCCPrefixMultipliers=DxccMultis, CodeDescription="Scan DXCC" });
            contInfo.Add(new ContestInfo { Code=ContestEnum.SAC_CW, Multipliers=multiList, Schedule=new Schedule { Month=9, Week=3 } });



            //ME_QSO_PARTY
            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=true }); //All DXCC

            regMultiContainer= new RegionMultiContainer();
            // All NA states and provinces
            regMultiContainer =MultiWrapper.GetRegionMultipliers("CANADA_PROVINCES");
            regMultiContainer.DXCCRegionList.AddRange(MultiWrapper.GetRegionMultipliers("US_STATES").DXCCRegionList);  //SIMPLIFY THIS !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            DxccMultis=new List<DXCCPrefixMultiplier> { new DXCCPrefixMultiplier { DXCCPrefix="VE" }, new DXCCPrefixMultiplier { DXCCPrefix="K" } };// Regions must be in VE or K
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION, Band=true, Mode=true, CodeDescription="NA St/Prov", DXCCPrefixMultipliers=DxccMultis, RegMultiContainer=regMultiContainer }); // TISZA-CUP prefixes. 

            //All  Maine counties as SRX, Exclude DX and state/prov
            contestMultiplier=new ContestMultiplier { MtType=MultiType.SRX_REGION, Band=true, Mode=true, CodeDescription="ME County." };
            contestMultiplier.DXCCPrefixMultipliers=new List<DXCCPrefixMultiplier> { new DXCCPrefixMultiplier { DXCCPrefix="K" } };// Counties must be in K
            contestMultiplier.ExcludedRegContainer.DXCCRegionList.AddRange(regMultiContainer.DXCCRegionList   )   ;  //Exclude all NA states/provinces

            contestMultiplier.ExcludedRegContainer.DXCCRegionList.AddRange(    (new RegionMultiContainer { RegionCode="DX", Comment="Excl DX" }).DXCCRegionList); // Exclude DX as well
            multiList.Add(contestMultiplier);
            contInfo.Add(new ContestInfo { Code=ContestEnum.ME_QSO_PARTY, Multipliers=multiList, Schedule=new Schedule { Month=9, Week=3 } }); //
        
            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            
            regMultiContainer=new RegionMultiContainer();
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=false });

            regMultiContainer.PopulateRegionListByDXCC(new List<Tuple<string, string>> { new Tuple<string, string>("YU", "County") });
           
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION, Band=true, Mode=false, RegMultiContainer=regMultiContainer });

            contInfo.Add(new ContestInfo { Code=ContestEnum.YUDX, Multipliers=multiList, Schedule=new Schedule { Month=9, Week=3 }, N1MMContestCode="YUDX" }); 

            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=true }); //All DXCC
            contInfo.Add(new ContestInfo { Code=ContestEnum.TRC_DX, Multipliers=multiList, Schedule=new Schedule { Month=10, Week=1 }, N1MMContestCode="TRCDX" }); //


            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            contestMultiplier=new ContestMultiplier { CodeSrc=QSOItemCodes.PFX, MtType=MultiType.PFX_CONTINENT, Band=true, Mode=true, ContinentMultipliers=new List<ContinentMultiplier> { new ContinentMultiplier { Continent="OC" } } };
            multiList.Add(contestMultiplier); //All PFX from OC continent

            contInfo.Add(new ContestInfo { Code=ContestEnum.OCEANIA_DX_CW, Multipliers=multiList, Schedule=new Schedule { Month=10, Week=2 } }); //


            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            contestMultiplier=new ContestMultiplier { CodeSrc=QSOItemCodes.SRX, MtType=MultiType.WAG, CodeDescription="DOK District", Band=true, Mode=false, DXCCPrefixMultipliers=new List<DXCCPrefixMultiplier> { new DXCCPrefixMultiplier { DXCCPrefix="DL" } } };
            multiList.Add(contestMultiplier); //Work GErman stations only. Members give DOK. Xnn. First letter of DOK is a district and it is multiplier DL non membes give NM and that is not an multiplier
            contInfo.Add(new ContestInfo { Code=ContestEnum.WAG, Multipliers=multiList, Schedule=new Schedule { Month=10, Week=3 } }); //


            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.CQZ, Band=true, Mode=true });
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=true });
            contInfo.Add(new ContestInfo { Code=ContestEnum.CQ_WW_SSB, Multipliers=multiList, Schedule=new Schedule { Month=10, Week=4 } });


            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            contInfo.Add(new ContestInfo { Code=ContestEnum.ARRL_SS_CW, Multipliers=multiList, Schedule=new Schedule { Month=11, Week=1 } });



            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            
            regMultiContainer=new RegionMultiContainer();
            regMultiContainer.PopulateRegionListByDXCC(new List<Tuple<string, string>> { new Tuple<string, string>("OL", "District"), new Tuple<string, string>("OM", "District") });
           
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION, Band=true, Mode=true, RegMultiContainer=regMultiContainer });

            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=false });
            contInfo.Add(new ContestInfo { Code=ContestEnum.OK_OM_DX_CW, Multipliers=multiList, Schedule=new Schedule { Month=11, Week=1 } });



            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.SRX, Band=true, Mode=true, CodeDescription="ITU_SQ" });  // ITUzone + square 27JN, 28JN, 28KN, 42MN, 42MM, 32NO, 32OO, 4FN
            contInfo.Add(new ContestInfo { Code=ContestEnum.HAM_SPIRIT, Multipliers=multiList, Schedule=new Schedule { Month=11, Week=2 } }); //  May also




            // !!! EXCLUDE LZ !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            DxccMultis=new List<DXCCPrefixMultiplier> { new DXCCPrefixMultiplier { DXCCPrefix="LZ" } };
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION, CodeSrc=QSOItemCodes.SRX, Band=true, Mode=false, DXCCPrefixMultipliers=DxccMultis, CodeDescription="LZ District" });  //SRX LZ: district, Others ITU
            multiList.Add(new ContestMultiplier { MtType=MultiType.ITUZ, Band=true, Mode=false, ExcludedDXCCPrefixMultipliers={ new DXCCPrefixMultiplier { DXCCPrefix="LZ" } } });  // !!! EXCLUDE LZ !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            contInfo.Add(new ContestInfo { Code=ContestEnum.LZ_DX, Multipliers=multiList, Schedule=new Schedule { Month=11, Week=3 } });

            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.CQZ, Band=true, Mode=true });
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=true });
            contInfo.Add(new ContestInfo { Code=ContestEnum.CQ_WW_CW,  ContestExchangeType= ContestExchangeType.CQZ,   Multipliers=multiList, Schedule=new Schedule { Month=11, Week=4 } });


            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.PFX, Band=true, Mode=true, CodeDescription="PFX" }); // Extract PFX from station call
            contInfo.Add(new ContestInfo { Code=ContestEnum.PCC, Multipliers=multiList, Schedule=new Schedule { Month=12, Week=1 } });//   Romanian  PRO CW CONTEST (PCC)   http://www.procontestclub.ro/PCC%20Rules.html



            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST --------------------------------- // Undefined multipliers
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=true });
            DxccMultis=new List<DXCCPrefixMultiplier>();
            DxccMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="VE" });
            DxccMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="K" });
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION, Band=true, Mode=true, DXCCPrefixMultipliers=DxccMultis, CodeDescription="Section" });
            contInfo.Add(new ContestInfo { Code=ContestEnum.ARRL_160, Multipliers=multiList, Schedule=new Schedule { Month=12, Week=1 } }); //


            


            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC,Band=true,Mode=true }); // All DXCC
            
            regMultiContainer=new RegionMultiContainer();
            regMultiContainer=MultiWrapper.GetRegionMultipliers("RUSSIA_OBLASTS");
            DxccMultis=new List<DXCCPrefixMultiplier> { new DXCCPrefixMultiplier { DXCCPrefix="UA" } };
            DxccMultis.AddRange(new List<DXCCPrefixMultiplier> { new DXCCPrefixMultiplier { DXCCPrefix="UA0" } });


            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION_LIST,MatchingMode=MatchingMode.LIST,Band=true,Mode=true,DXCCPrefixMultipliers=DxccMultis,RegMultiContainer=regMultiContainer,CodeDescription="UA Oblast." });
            contInfo.Add(new ContestInfo { Code=ContestEnum.RUS_WW_MM,Multipliers=multiList,Schedule=new Schedule { Month=8,Week=4 } });

            contInfo.Add(new ContestInfo { Code=ContestEnum.FOCBWQP,Multipliers=new List<ContestMultiplier>(),Schedule=new Schedule { Month=3,Week=3 } }); // No multis STX Name





            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            
            regMultiContainer=new RegionMultiContainer();
            regMultiContainer.PopulateRegionListByDXCC(new List<Tuple<string,string>> { new Tuple<string,string>("VE","Prov."),new Tuple<string,string>("K","State") });
           
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION_LIST,Band=true,Mode=true,RegMultiContainer=regMultiContainer });

            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC,Band=true,Mode=false });
            contInfo.Add(new ContestInfo { Code=ContestEnum.CQ_160_CW,Multipliers=multiList,Schedule=new Schedule { Month=1,Week=4 } });


            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            
            regMultiContainer=new RegionMultiContainer();

            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC,Band=true,Mode=true });
            DxccMultis=MultiWrapper.GetDXCCPrefixMultipliers("CVA");

            regMultiContainer=MultiWrapper.GetRegionMultipliers("BRAZIL_STATES");
            DxccMultis=new List<DXCCPrefixMultiplier> { new DXCCPrefixMultiplier { DXCCPrefix="PY" } };
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION_LIST,MatchingMode=MatchingMode.LIST,Band=true,Mode=true,DXCCPrefixMultipliers=DxccMultis,RegMultiContainer=regMultiContainer,CodeDescription="PY State." });
            contInfo.Add(new ContestInfo { Code=ContestEnum.CVA_DX_CW , ContestExchangeType= ContestExchangeType.REGION_CONT,Multipliers=multiList,Schedule=new Schedule { Month=8,Week=3 } });



            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------

            multiList.Add(new ContestMultiplier { MtType=MultiType.DXCC, Band=true, Mode=true });
            regMultiContainer=new RegionMultiContainer();
            


            regMultiContainer.PopulateRegionListByDXCC(new List<Tuple<string,string>> { new Tuple<string,string>("VE","Prov."),new Tuple<string,string>("K","State") ,new Tuple<string,string>("XE","State") });
           
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION_LIST,Band=true,Mode=true,RegMultiContainer=regMultiContainer });


            contInfo.Add(new ContestInfo { Code=ContestEnum.ARRL_10, Multipliers=multiList, Schedule=new Schedule { Month=12, Week=2 } }); //




            // Compare the new definition below with commented above.
            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST ---------------------------------
            
            regMultiContainer=new RegionMultiContainer();
            regMultiContainer.PopulateRegionListByDXCC(new List<Tuple<string, string>> { new Tuple<string, string>("9A", "County") });
           
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION, Band=true, Mode=true, RegMultiContainer=regMultiContainer });


            contestMultiplier=new ContestMultiplier { MtType=MultiType.ITUZ, CodeSrc=QSOItemCodes.SRX1, Band=true, Mode=true };
            contestMultiplier.ExcludedDXCCPrefixMultipliers=new List<DXCCPrefixMultiplier>();
            multiList.Add(contestMultiplier);
            contInfo.Add(new ContestInfo { Code=ContestEnum.CRO_9A_DX, CabrilloCode="9A-DX", Multipliers=multiList, Schedule=new Schedule { Month=12, Week=3 }, N1MMContestCode="9A-DX" });
            // CRO contest: set Cabrillo because the Code is not the same as the contest code. 9A-DX is the Cabrillo code for CRO_9A_DX


            multiList=new List<ContestMultiplier>();//-------------------- NEW CONTEST --------------------------------- 
            DxccMultis=new List<DXCCPrefixMultiplier>();
            DxccMultis.Add(new DXCCPrefixMultiplier { DXCCPrefix="VE" });
            multiList.Add(new ContestMultiplier { MtType=MultiType.REGION, Band=true, Mode=true, DXCCPrefixMultipliers=DxccMultis, CodeDescription="VE Prov" });
            contInfo.Add(new ContestInfo { Code=ContestEnum.CANADA_WINTER, Multipliers=multiList, Schedule=new Schedule { Month=12, Week=3 } }); //


            contInfo.Add(new ContestInfo { Code=ContestEnum.STEW_PERRY, Multipliers=new List<ContestMultiplier>(), Schedule=new Schedule { Month=12, Week=4 } }); //several times a year


            contInfo=contInfo.OrderBy(x => x.Schedule.MonthWeekNum).ToList<ContestInfo>();

            foreach (ContestInfo cont in contInfo) {
                if (cont.CabrilloCode.Length==0) {
                    cont.CabrilloCode=cont.EnumCodeToCabrillo(cont.Code);
                }
            }

            return contInfo;
        }
    }

}
