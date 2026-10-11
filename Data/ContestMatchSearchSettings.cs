using System;

namespace Data {
    /// <summary>
    /// Centralized configuration for tbExactCallContestMatch search algorithm.
    /// Controls minimum character thresholds, result limits, and match type behavior.
    /// All settings are configurable and persisted to database via SettingsLog.
    /// </summary>
    public class ContestMatchSearchSettings {
        /// <summary>
        /// Minimum characters before any search starts.
        /// Default: 2 (searching on 1 character is too broad)
        /// </summary>
        public int MinSearchLength { get; set; } = 2;

        /// <summary>
        /// Minimum characters before Contains search activates (in addition to StartsWith).
        /// Default: 3 (Contains on short strings matches too many irrelevant calls)
        /// </summary>
        public int MinContainsSearchLength { get; set; } = 3;

        /// <summary>
        /// Maximum StartsWith results to display.
        /// Default: 10
        /// </summary>
        public int MaxStartsWithResults { get; set; } = 10;

        /// <summary>
        /// Maximum Contains results to display (shown after StartsWith with separator).
        /// Default: 5
        /// </summary>
        public int MaxContainsResults { get; set; } = 5;

        /// <summary>
        /// Minimum call length for exact match flag (affects color coding).
        /// Default: 3 (prevents premature exact match flagging on very short inputs)
        /// </summary>
        public int MinCallLengthForExactMatch { get; set; } = 3;

        /// <summary>
        /// Visual separator between StartsWith and Contains results.
        /// Default: "│" (Unicode box drawing character)
        /// </summary>
        public string MatchTypeSeparator { get; set; } = "│";

        /// <summary>
        /// Load settings from SettingsLog instance.
        /// </summary>
        public void LoadFromSettings(SettingsLog settings) {
            if(settings == null) return;

            MinSearchLength = settings.ContestMatchMinSearchLength;
            MinContainsSearchLength = settings.ContestMatchMinContainsLength;
            MaxStartsWithResults = settings.ContestMatchMaxStartsWith;
            MaxContainsResults = settings.ContestMatchMaxContains;
            MinCallLengthForExactMatch = settings.contestCallLenMin;
        }

        /// <summary>
        /// Create default instance with standard contest search parameters.
        /// </summary>
        public static ContestMatchSearchSettings CreateDefault() {
            return new ContestMatchSearchSettings {
                MinSearchLength = 2,
                MinContainsSearchLength = 3,
                MaxStartsWithResults = 10,
                MaxContainsResults = 5,
                MinCallLengthForExactMatch = 3,
                MatchTypeSeparator = "│"
            };
        }
    }
}