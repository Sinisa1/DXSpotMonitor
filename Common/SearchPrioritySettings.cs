using System;
using System.Collections.Generic;
using System.Globalization;

namespace Common {
    /// <summary>
    /// Multiplicative weight-based scoring for call search matching.
    ///
    /// STORAGE FORMAT:  Integer × 100  (prorated)
    ///   GUI shows / DB stores integers:  100 = ×1.00,  150 = ×1.50,  300 = ×3.00
    ///   Range 1–999.  No decimals in UI or database.
    ///
    /// CALCULATION:  Use the *Calc read-only properties (divides raw by 100.0).
    ///   e.g. WeightStartsWith = 300  →  WeightStartsWithCalc = 3.00
    ///
    /// All AppSetting keys start with "callMatch". DataType = "int" for all.
    /// </summary>
    public class SearchPrioritySettings {

        // ===== RAW PRORATED INTEGER VALUES (stored in DB / shown in XAML) =====
        public int WeightSimilarity { get; set; } = 100;  // ×1.00 – neutral base
        public int WeightStartsWith { get; set; } = 300;  // ×3.00
        public int WeightContains { get; set; } = 150;  // ×1.50
        public int WeightSuperCheck { get; set; } = 130;  // ×1.30
        public int WeightWorked { get; set; } = 150;  // ×1.50 base for any worked call
        public int WorkedCountFactor { get; set; } = 10;   // ×0.10 log-scale per extra QSO

        // ===== COMPUTED CALCULATION VALUES (raw ÷ 100.0) =====
        public double WeightSimilarityCalc => WeightSimilarity  / 100.0;
        public double WeightStartsWithCalc => WeightStartsWith  / 100.0;
        public double WeightContainsCalc => WeightContains    / 100.0;
        public double WeightSuperCheckCalc => WeightSuperCheck  / 100.0;
        public double WeightWorkedCalc => WeightWorked      / 100.0;
        public double WorkedCountFactorCalc => WorkedCountFactor / 100.0;

        // ===== FILTER / RESULT LIMITS (already int, no proration) =====
        public int MinPercentThresholdSimilarity { get; set; } = 30;
        public int TakeTopMatchesNumber { get; set; } = 50;

        // ===== UNIFIED MATCH COUNT LIMITS =====
        public int MaxStartsWith { get; set; } = 15;
        public int MaxContains { get; set; } = 8;
        public int MaxOther { get; set; } = 4;
        public int MaxTotal { get; set; } = 30;

        // ========== FACTORY ==========
        public static SearchPrioritySettings GetDefaultSettings() => new SearchPrioritySettings();

        // ========== DATA-TYPE REGISTRY ==========
        // All entries are "int" — weights are prorated integers, limits are plain integers.
        private static readonly Dictionary<string,string> _dataTypes =
            new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase) {
                { "callMatchMinSimilarity",     "int" },
                { "callMatchTakeTopMatches",    "int" },
                { "callMatchWeightSimilarity",  "int" },
                { "callMatchWeightStartsWith",  "int" },
                { "callMatchWeightContains",    "int" },
                { "callMatchWeightSuperCheck",  "int" },
                { "callMatchWeightWorked",      "int" },
                { "callMatchWorkedCountFactor", "int" },
                { "callMatchMaxStartsWith",     "int" },
                { "callMatchMaxContains",       "int" },
                { "callMatchMaxOther",          "int" },
                { "callMatchMaxTotal",          "int" },
            };

        /// <summary>Returns the canonical lowercase DataType for a given key. Unknown keys → "string".</summary>
        public static string GetDataTypeForKey(string key) =>
            _dataTypes.TryGetValue(key ?? "",out var dt) ? dt : "string";

        // ========== SAVE TO DB ==========
        public List<AppSettingDB> GetSettingsForSave() {
            return new List<AppSettingDB> {
                S("callMatchMinSimilarity",     MinPercentThresholdSimilarity.ToString(), "int", "30"),
                S("callMatchTakeTopMatches",    TakeTopMatchesNumber.ToString(),          "int", "50"),
                S("callMatchWeightSimilarity",  WeightSimilarity.ToString(),              "int", "100"),
                S("callMatchWeightStartsWith",  WeightStartsWith.ToString(),              "int", "300"),
                S("callMatchWeightContains",    WeightContains.ToString(),                "int", "150"),
                S("callMatchWeightSuperCheck",  WeightSuperCheck.ToString(),              "int", "130"),
                S("callMatchWeightWorked",      WeightWorked.ToString(),                  "int", "150"),
                S("callMatchWorkedCountFactor", WorkedCountFactor.ToString(),             "int", "10"),
                S("callMatchMaxStartsWith",     MaxStartsWith.ToString(),                 "int", "15"),
                S("callMatchMaxContains",       MaxContains.ToString(),                   "int", "8"),
                S("callMatchMaxOther",          MaxOther.ToString(),                      "int", "4"),
                S("callMatchMaxTotal",          MaxTotal.ToString(),                      "int", "30"),
            };
        }

        private static AppSettingDB S(string code,string value,string dataType,string def) =>
            new AppSettingDB { Code = code,ValueString = value,DataType = dataType,DefaultValueString = def };

        // ========== LOAD FROM DB ==========
        public void LoadFromAppSettings(Dictionary<string,AppSettingDB> s) {
            if(s == null) return;

            // Plain int settings
            TryLoad(s,"callMatchMinSimilarity",v => MinPercentThresholdSimilarity = int.Parse(v));
            TryLoad(s,"callMatchTakeTopMatches",v => TakeTopMatchesNumber          = int.Parse(v));
            TryLoad(s,"callMatchMaxStartsWith",v => MaxStartsWith                 = int.Parse(v));
            TryLoad(s,"callMatchMaxContains",v => MaxContains                   = int.Parse(v));
            TryLoad(s,"callMatchMaxOther",v => MaxOther                      = int.Parse(v));
            TryLoad(s,"callMatchMaxTotal",v => MaxTotal                      = int.Parse(v));

            // Prorated int settings — ParseProrated handles BOTH legacy "1.5" and current "150"
            TryLoad(s,"callMatchWeightSimilarity",v => WeightSimilarity  = ParseProrated(v));
            TryLoad(s,"callMatchWeightStartsWith",v => WeightStartsWith  = ParseProrated(v));
            TryLoad(s,"callMatchWeightContains",v => WeightContains    = ParseProrated(v));
            TryLoad(s,"callMatchWeightSuperCheck",v => WeightSuperCheck  = ParseProrated(v));
            TryLoad(s,"callMatchWeightWorked",v => WeightWorked      = ParseProrated(v));
            TryLoad(s,"callMatchWorkedCountFactor",v => WorkedCountFactor = ParseProrated(v));
        }

        /// <summary>
        /// Parses a weight value stored as either:
        ///   • New format — prorated integer string: "150" → 150
        ///   • Legacy format — decimal string: "1.5" → 150  (auto-migrates on first load)
        /// </summary>
        private static int ParseProrated(string v) {
            if(v.Contains('.') || v.Contains(',')) {
                double d = double.Parse(v,CultureInfo.InvariantCulture);
                return (int)Math.Round(d * 100.0);
            }
            return int.Parse(v);
        }

        private static void TryLoad(Dictionary<string,AppSettingDB> s,string key,Action<string> assign) {
            if(s.TryGetValue(key,out var entry) && !string.IsNullOrWhiteSpace(entry.ValueString)) {
                try { assign(entry.ValueString); } catch { /* keep default on bad data */ }
            }
        }
    }
}