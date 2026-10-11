using System;

namespace ClubLogMatcher {
    public class DxccMatchResult {
        public string Callsign { get; set; } = string.Empty;
        public short Adif { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string PrimaryPrefix { get; set; } = string.Empty;
        public byte CqZone { get; set; }
        public string Continent { get; set; } = string.Empty;
        public decimal? Longitude { get; set; }
        public decimal? Latitude { get; set; }
        public bool IsInvalid { get; set; }
        public string MatchType { get; set; } = string.Empty; // "Exception", "Prefix", "Invalid"
    }

    public class EntityRecord {
        public short Adif { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public bool Deleted { get; set; }
        public byte Cqz { get; set; }
        public string Cont { get; set; } = string.Empty;
        public decimal? Longitude { get; set; }
        public decimal? Latitude { get; set; }
    }

    public class RuleRecord {
        public int RecordId { get; set; }
        public string Call { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public short Adif { get; set; }
        public byte? Cqz { get; set; }
        public string? Cont { get; set; }
        public decimal? Longitude { get; set; }
        public decimal? Latitude { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }

        //public bool IsActiveAt(DateTime qsoTime) =>
        //    ( qsoTime >= Start.Value && qsoTime <= End.Value)
        //     ||End==null;
        /*
         * 
                RecordId	Call	Entity	Adif	Cqz	Cont	Longitude	Latitude	Start	End
        clPrefix	5446	XU	CHINA	    318	24	AS	116.400000	40.000000	NULL	            1949-12-31 18:59:59
         clPrefix	7258	XU	CAMBODIA	312	26	AS	104.800000	11.700000	1949-12-31 19:00:00	NULL

      */

        public bool IsActiveAt(DateTime qsoTime) {
            bool result = false;
            DateTime start = Start ==null ? new DateTime(1990,01,01) : (DateTime)Start;
            DateTime end = End ==null ? new DateTime(2999,12,31) : (DateTime)End;
            result =(qsoTime >= start && qsoTime <= end);
            
            return result;
        }
    }

    public class ZoneOverrideRecord {
        public string Call { get; set; } = string.Empty;
        public byte Zone { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }

        public bool IsActiveAt(DateTime qsoTime) =>
            (!Start.HasValue || qsoTime >= Start.Value) &&
            (!End.HasValue || qsoTime <= End.Value);
    }
}