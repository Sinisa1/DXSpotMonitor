using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Linq;
namespace Data {
    // Utility class representing a callsign entry in the supercheck partial DB
    public enum SCMode:short {
        CW = 0,
        Phone = 1,
        FM = 2,
        RTTY = 3,
        Digital = 4
    }

    public enum SCGeo:short {
        DX = 0, // non-US/CA
        US_Canada = 1,
        SweepstakesEligible = 2 // US, Canada, US possessions
    }

    public enum SCVerified:short {
        SCPSubmitter = 0, // sent a Cabrillo log
        ContestReceivedLogsPage = 1,
        RegulatoryDatabase = 2, // FCC/ISED/Ofcom
        LoTWUserList = 3,
        QRZCom = 4,
        HamQTHCom = 5,
        ManuallyAdded = 6
    }

    public enum SCContest:short {
        CQ_WW = 0,
        CQ_WPX = 1,
        CQ_160 = 2,
        ARRL_DX = 3,
        ARRL_SS = 4,
        ARRL_10 = 5,
        ARRL_160 = 6,
        ARRL_RTTY = 7,
        ARRL_FIELD_DAY = 8,
        ARRL_VHF = 9,
        NAQP = 10,
        IARU_HF = 11,
        DARC_WAEDC = 12,
        RSGB = 13,
        CW_OPS = 14,
        RDXC = 15,
        SAC = 16,
        NA_SPRINT = 17,
        STEW_PERRY = 18,
        WW_DIGI = 19,
        PACC = 20,
        WAG = 21,
        HA_DX = 22,
        LZ_DX = 23,
        SP_DX = 24,
        OK_OM_DX = 25,
        EUDXC = 26
    }
        public class SCCallsign {
        public string Callsign { get; set; }
        public int AnnualRate { get; set; }
        public int Modes { get; set; }
        public int Contests { get; set; }
        public int Geo { get; set; }
        public int Verified { get; set; }
        // Lists of Enums
        public List<SCMode> ModeList { get; set; }
        public List<SCGeo> GeoList { get; set; }
        public List<SCVerified> VerifiedList { get; set; }
        public List<SCContest> ContestList { get; set; }
    }

    // Main utility and parser class for supercheck partial SQLite DB
    public class SCPartial {

        public List<SCCallsign> LoadCallsigns(string dbPath) {
            var list = new List<SCCallsign>();
            string connStr = $"Data Source={dbPath}";
            using var conn = new SqliteConnection(connStr);
            conn.Open();
            using var cmd = new SqliteCommand("SELECT * FROM callsigns", conn);
            using var reader = cmd.ExecuteReader();
            while(reader.Read()) {
                var call = ParseCallsign(reader);

                // Populate enum lists from bitfields
                call.ModeList = GetEnumList<SCMode>(call.Modes);
                call.ContestList = GetEnumList<SCContest>(call.Contests);
                call.GeoList = GetEnumList<SCGeo>(call.Geo);
                call.VerifiedList = GetEnumList<SCVerified>(call.Verified);

                list.Add(call);
            }
            return list;
        }

        // Helper to extract set bits as enum values
        private List<TEnum> GetEnumList<TEnum>(int bitfield) where TEnum : Enum {
            var result = new List<TEnum>();
            foreach (TEnum value in Enum.GetValues(typeof(TEnum))) {
                int bit = Convert.ToInt32(value);
                if ((bitfield & (1 << bit)) != 0)
                    result.Add(value);
            }
            return result;
        }

        // Example: Parse a SCCallsign from a SQLiteDataReader
        public SCCallsign ParseCallsign(SqliteDataReader reader) {
            return new SCCallsign {
                Callsign = reader["callsign"].ToString(),
                AnnualRate = Convert.ToInt32(reader["annual_rate"]),
                Modes = Convert.ToInt32(reader["modes"]),
                Contests = Convert.ToInt32(reader["contests"]),
                Geo = Convert.ToInt32(reader["geo"]),
                Verified = Convert.ToInt32(reader["verified"])
            };
        }
    }
}