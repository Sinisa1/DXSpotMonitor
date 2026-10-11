

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Common.LookupFile {

    public class StationContestInfo {
        public string Call { get; set; } = "";
        public string Name { get; set; } = "";
        public string State { get; set; } = "";
        public string UserText { get; set; } = "";
        public string DataSource { get; set; } = "";  // e.g., "NAQP", "ARRL"
    }

    /// <summary>
    /// Fast O(1) lookup for contest station info using FrozenDictionary
    /// Thread-safe for reads, immutable after loading
    /// </summary>
    public class StationContestLookup {
        private FrozenDictionary<string, StationContestInfo> _lookup;
        private int _stationCount = 0;

        /// <summary>
        /// Load stations from database DataTable
        /// </summary>
        public void LoadFromDataTable(DataTable dt, string dataSource = "NAQP") {
            if (dt == null || dt.Rows.Count == 0) {
                System.Diagnostics.Debug.WriteLine($"[StationContestLookup] No data to load for {dataSource}");
                _lookup = FrozenDictionary<string, StationContestInfo>.Empty;
                return;
            }

            var stations = dt.AsEnumerable()
                .Select(row => new StationContestInfo {
                    Call = row["Call"]?.ToString()?.Trim().ToUpperInvariant() ?? "",
                    Name = row["Name"]?.ToString()?.Trim() ?? "",
                    State = row["State"]?.ToString()?.Trim() ?? "",
                    UserText = row.Table.Columns.Contains("UserText")
                        ? row["UserText"]?.ToString()?.Trim() ?? ""
                        : "",
                    DataSource = dataSource
                })
                .Where(x => !string.IsNullOrEmpty(x.Call))
                .ToList();

            // ✅ Build FrozenDictionary (case-insensitive, fastest lookup)
            _lookup = stations.ToFrozenDictionary(
                x => x.Call,
                StringComparer.OrdinalIgnoreCase
            );

            _stationCount = _lookup.Count;
            System.Diagnostics.Debug.WriteLine(
                $"[StationContestLookup] Loaded {_stationCount} stations from {dataSource}");
        }

        /// <summary>
        /// O(1) lookup - returns station info or null
        /// </summary>
        public StationContestInfo GetByCall(string call) {
            if (_lookup == null || string.IsNullOrWhiteSpace(call))
                return null;

            return _lookup.TryGetValue(call.ToUpperInvariant(), out var station)
                ? station
                : null;
        }

        /// <summary>
        /// O(1) check if call exists in contest
        /// </summary>
        public bool Exists(string call) {
            return _lookup != null &&
                   !string.IsNullOrWhiteSpace(call) &&
                   _lookup.ContainsKey(call.ToUpperInvariant());
        }

        /// <summary>
        /// Get all stations (for debugging/display)
        /// </summary>
        public IEnumerable<StationContestInfo> GetAll() {
            return _lookup?.Values ?? Enumerable.Empty<StationContestInfo>();
        }

        /// <summary>
        /// Get total count
        /// </summary>
        public int Count => _stationCount;

        /// <summary>
        /// Clear all data
        /// </summary>
        public void Clear() {
            _lookup = FrozenDictionary<string, StationContestInfo>.Empty;
            _stationCount = 0;
        }
    }
}