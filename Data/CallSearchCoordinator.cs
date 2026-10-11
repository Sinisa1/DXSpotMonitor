using Common;
using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;


namespace Data {

    /*
   Coordinates all search operations: pattern matching, optimization, merging, scoring.
   EXTRACTS ALL SEARCH LOGIC FROM LogProcessing.

  STARTUP
  1. App.xaml.cs: InitializeSharedInstances()
 ↓
2. LogProcessing: LoadAppSettingsDictionaryFromDB()
 → Loads from SQL: [GetAppSettings]
 → Stores in: AppSettingsDict
 → Auto-loads: PrioritySettings.LoadFromAppSettings(AppSettingsDict)
 ↓
3. MainWindow: SetControlValues()
 → ucSearchSettings.LoadFromAppSettings(AppSettingsDict)
 → UI controls populated from AppSettingsDict

      SETTING CHANGED
1. User changes weight textbox in ucSearchSettings
 ↓
2. ucSearchSettings: OnWeightChanged()
 → Calls: DatabaseSaveCallback(= MainWindow.CallbackFnSettingChanged)
 ↓
3. MainWindow: CallbackFnSettingChanged()
 → Calls: logProc.UpdateAppSetting(code, value)
 → Saves to SQL: [UpdateAppSetting]
 → Updates: AppSettingsDict[code] = new value
 ↓
4. ucSearchSettings: Raises SettingsChanged event
 ↓
5. MainWindow: ucSearchSettings_SettingsChanged()
 → Syncs: PrioritySettings ← ucSearchSettings values
 → Calls: RefreshSearch()
   Startup:
    LogProcessing.LoadAppSettingsDictionaryFromDB()
      → QSODataService.GetAppSettings()
        → Calls SQL: [GetAppSettings]
        → Returns: Dictionary<string, AppSettingDB>
      → SearchPrioritySettings.LoadFromAppSettings(dict)
      → ucSearchSettings.LoadFromAppSettings(dict)

  Save:
    ucSearchSettings change
      → CallbackFnSettingChanged
        → LogProcessing.UpdateAppSetting()
          → QSODataService.UpdateAppSetting()
            → Calls SQL: [UpdateAppSetting]

  /// </summary>
  public sealed class CallSearchCoordinator {
      private readonly CallSearchMatcher _matcher;
      private readonly CallMatchScorer _scorer;
      private readonly SearchPrioritySettings _settings;

      public CallSearchCoordinator(SearchPrioritySettings settings) {
          _settings = settings ?? throw new ArgumentNullException(nameof(settings));
          _matcher = new CallSearchMatcher(settings);
          _scorer = new CallMatchScorer(settings);
      }
*/

    public sealed class CallSearchCoordinator {
        private readonly CallSearchMatcher _matcher;
        private readonly CallMatchScorer _scorer;
        private readonly SearchPrioritySettings _settings;

        public CallSearchCoordinator(SearchPrioritySettings settings) {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _matcher = new CallSearchMatcher(settings);
            _scorer = new CallMatchScorer(settings);
        }



        // ========== ADD THIS NEW METHOD ==========
        /// <summary>
        /// Execute search using FrozenDictionary (2x faster than HashSet)
        /// </summary>
        public SearchExecutionResult ExecuteSearchWithFrozenDictionary(
            string searchText,
            MatchType matchType,
            SimilarityMatchType similarityType,
            FrozenDictionary<string,MatchedCalls> masterDictionary,
            bool useSuperCheck) {

            if(masterDictionary == null || masterDictionary.Count == 0) {
                return new SearchExecutionResult {
                    Matches = new List<MatchedCalls>(),
                    SuperCheckMatches = new List<MatchedCalls>(),
                    MyLogMatches = new List<MatchedCalls>()
                };
            }

            var allMatches = new List<MatchedCalls>(masterDictionary.Count / 100);
            double minThreshold = _settings.MinPercentThresholdSimilarity / 100.0;

            // ✅ Iterate frozen dictionary
            foreach(var kvp in masterDictionary) {
                var call = kvp.Value;

                // Skip if SuperCheck filtering disabled
                if(!useSuperCheck && call.InSuperCheck && call.WkdBeforeCount == 0)
                    continue;

                double similarity = 0;
                bool isMatch = false;

                switch(matchType) {
                case MatchType.QUICK:
                    if(call.Call.Equals(searchText,StringComparison.OrdinalIgnoreCase)) {
                        similarity = 1.0;
                        isMatch = true;
                    } else if(call.Call.StartsWith(searchText,StringComparison.OrdinalIgnoreCase)) {
                        similarity = 0.9 - (0.1 * (call.Call.Length - searchText.Length) / 10.0);
                        isMatch = true;
                    } else if(call.Call.Contains(searchText,StringComparison.OrdinalIgnoreCase)) {
                        similarity = 0.7 - (0.1 * (call.Call.Length - searchText.Length) / 10.0);
                        isMatch = true;
                    }
                    break;

                case MatchType.SIMILARITY:
                    if(similarityType == SimilarityMatchType.MORSE) {
                        similarity = SimilarityExtensions.CalculateSimilarity(
                            searchText,call.Call,"MORSECONVERTBOTH");
                    } else {
                        similarity = SimilarityExtensions.CalculateSimilarity(searchText,call.Call);
                    }
                    isMatch = similarity >= minThreshold;
                    break;
                }

                if(isMatch) {
                    var match = new MatchedCalls {
                        Call = call.Call,
                        CallMorse = call.CallMorse,
                        WkdBeforeCount = call.WkdBeforeCount,
                        InSuperCheck = call.InSuperCheck,
                        MatchPct = similarity * 100.0,
                        StartsWith = call.Call.StartsWith(searchText,StringComparison.OrdinalIgnoreCase),
                        Contains = call.Call.Contains(searchText,StringComparison.OrdinalIgnoreCase)
                    };

                    match.MatchScore = _scorer.CalculateFinalScore(match,searchText,matchType);
                    allMatches.Add(match);
                }
            }

            // Sort and take top N
            allMatches.Sort((a,b) => {
                int scoreCompare = b.MatchScore.CompareTo(a.MatchScore);
                return scoreCompare != 0 ? scoreCompare : string.Compare(a.Call,b.Call,StringComparison.Ordinal);
            });

            int takeCount = Math.Min(_settings.TakeTopMatchesNumber,allMatches.Count);
            var finalMatches = allMatches.GetRange(0,takeCount);

            // Split for compatibility
            var myLogMatches = finalMatches.Where(x => x.WkdBeforeCount > 0).ToList();
            var superCheckMatches = finalMatches.Where(x => x.InSuperCheck && x.WkdBeforeCount == 0).ToList();

            return new SearchExecutionResult {
                Matches = finalMatches,
                SuperCheckMatches = superCheckMatches,
                MyLogMatches = myLogMatches
            };
        }

        public SearchExecutionResult ExecuteSearchWithList(
           string searchText,
           MatchType matchType,
           SimilarityMatchType similarityType,
           IReadOnlyList<MatchedCalls> masterList,
           bool useSuperCheck) {

            if(masterList == null || masterList.Count == 0) {
                return new SearchExecutionResult {
                    Matches = new List<MatchedCalls>(),
                    SuperCheckMatches = new List<MatchedCalls>(),
                    MyLogMatches = new List<MatchedCalls>()
                };
            }

            List<MatchedCalls> resultList;

            if(matchType == MatchType.SIMILARITY) {
                resultList = ExecuteSearchSimilarity(searchText,similarityType,masterList,useSuperCheck);
            } else {
                resultList = ExecuteSearchQuick(searchText,masterList,useSuperCheck);
            }

            var myLogMatches = resultList.Where(x => x.WkdBeforeCount > 0).ToList();
            var superCheckMatches = resultList.Where(x => x.InSuperCheck && x.WkdBeforeCount == 0).ToList();

            return new SearchExecutionResult {
                Matches           = resultList,
                SuperCheckMatches = superCheckMatches,
                MyLogMatches      = myLogMatches
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // QUICK MODE — simple pattern match, no weighted scoring.
        // Groups: Exact (100) → StartsWith (90) → Contains (70), alpha within group.
        // Returns top-N immediately — weights do NOT apply in QUICK mode.
        // ──────────────────────────────────────────────────────────────────────
        private List<MatchedCalls> ExecuteSearchQuick(
            string searchText,
            IReadOnlyList<MatchedCalls> masterList,
            bool useSuperCheck) {

            var exact = new List<MatchedCalls>();
            var startsWith = new List<MatchedCalls>();
            var contains = new List<MatchedCalls>();

            foreach(var call in masterList) {
                if(!useSuperCheck && call.InSuperCheck && call.WkdBeforeCount == 0)
                    continue;

                if(call.Call.Equals(searchText,StringComparison.OrdinalIgnoreCase)) {
                    exact.Add(MakeQuickMatch(call,100.0,searchText));
                } else if(call.Call.StartsWith(searchText,StringComparison.OrdinalIgnoreCase)) {
                    startsWith.Add(MakeQuickMatch(call,90.0,searchText));
                } else if(call.Call.Contains(searchText,StringComparison.OrdinalIgnoreCase)) {
                    contains.Add(MakeQuickMatch(call,70.0,searchText));
                }
            }

            // Sort each group alphabetically
            exact.Sort((a,b) => string.Compare(a.Call,b.Call,StringComparison.Ordinal));
            startsWith.Sort((a,b) => string.Compare(a.Call,b.Call,StringComparison.Ordinal));
            contains.Sort((a,b) => string.Compare(a.Call,b.Call,StringComparison.Ordinal));

            // Concatenate: Exact first, then StartsWith, then Contains
            int maxTotal = _settings.TakeTopMatchesNumber;
            var result = new List<MatchedCalls>(maxTotal);

            result.AddRange(exact);
            if(result.Count < maxTotal) result.AddRange(startsWith.Take(maxTotal - result.Count));
            if(result.Count < maxTotal) result.AddRange(contains.Take(maxTotal - result.Count));

            // Assign simple rank-based scores for display only (not weight-dependent)
            for(int i = 0;i < result.Count;i++) {
                result[i].MatchScore = result[i].MatchPct;
            }

            return result;
        }

        private static MatchedCalls MakeQuickMatch(MatchedCalls call,double matchPct,string searchText) {
            return new MatchedCalls {
                Call           = call.Call,
                CallMorse      = call.CallMorse,
                WkdBeforeCount = call.WkdBeforeCount,
                InSuperCheck   = call.InSuperCheck,
                MatchPct       = matchPct,
                StartsWith     = call.Call.StartsWith(searchText,StringComparison.OrdinalIgnoreCase),
                Contains       = call.Call.Contains(searchText,StringComparison.OrdinalIgnoreCase),
                MatchScore     = 0
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // SIMILARITY MODE — full Levenshtein/Morse scoring.
        // Returns ALL candidates above threshold with MatchScore = 0.
        // Caller (RefreshSearch.ApplyScoring) does the weighted scoring so that
        // weight changes reshuffle the FULL pool, not a pre-truncated top-N.
        // ──────────────────────────────────────────────────────────────────────
        private List<MatchedCalls> ExecuteSearchSimilarity(
            string searchText,
            SimilarityMatchType similarityType,
            IReadOnlyList<MatchedCalls> masterList,
            bool useSuperCheck) {

            double minThreshold = _settings.MinPercentThresholdSimilarity / 100.0;
            int searchLen = searchText.Length;

            var allMatches = new ConcurrentBag<MatchedCalls>();

            Parallel.ForEach(masterList,new ParallelOptions {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            },call => {
                if(!useSuperCheck && call.InSuperCheck && call.WkdBeforeCount == 0)
                    return;

                // Length-based early exit
                int lengthDiff = Math.Abs(call.Call.Length - searchLen);
                double avgLength = (searchLen + call.Call.Length) / 2.0;
                if(avgLength > 0 && (1.0 - lengthDiff / avgLength) < minThreshold)
                    return;

                double similarity = similarityType == SimilarityMatchType.MORSE
                    ? SimilarityExtensions.CalculateSimilarity(searchText,call.Call,"MORSECONVERTBOTH")
                    : SimilarityExtensions.CalculateSimilarity(searchText,call.Call);

                if(similarity >= minThreshold) {
                    allMatches.Add(new MatchedCalls {
                        Call           = call.Call,
                        CallMorse      = call.CallMorse,
                        WkdBeforeCount = call.WkdBeforeCount,
                        InSuperCheck   = call.InSuperCheck,
                        MatchPct       = similarity * 100.0,
                        StartsWith     = call.Call.StartsWith(searchText,StringComparison.OrdinalIgnoreCase),
                        Contains       = call.Call.Contains(searchText,StringComparison.OrdinalIgnoreCase),
                        MatchScore     = 0   // ← scored by RefreshSearch with current weights
                    });
                }
            });

            return allMatches.ToList();
        }


        // /// <summary>
        // /// Execute search, merge results, and calculate scores
        // /// </summary>
        // public SearchExecutionResult ExecuteSearch(
        //     string searchText, MatchType matchType, SimilarityMatchType similarityType, HashSet<MatchedCalls> myLogCalls, HashSet<MatchedCalls> superCheckCalls, bool useSuperCheck,
        //     List<Common.SuperCheck.SuperCheckMatch> optimizedSuperCheckResults = null, List<Common.SuperCheck.SuperCheckMatch> optimizedMyLogResults = null) {
        //     // ✅ STEP 1: Execute pattern matching
        //     List<MatchedCalls> myLogMatches;
        //    List<MatchedCalls> superCheckMatches = new List<MatchedCalls>();

        //     // Use optimized results if provided, otherwise fall back to matcher
        //     if (optimizedMyLogResults != null) {
        //         myLogMatches = new List<MatchedCalls>(optimizedMyLogResults.Count);
        //         for (int i = 0; i < optimizedMyLogResults.Count; i++) {
        //             var m = optimizedMyLogResults[i];
        //             var existingCall = myLogCalls.FirstOrDefault(x =>
        //                 x.Call.Equals(m.Callsign, StringComparison.OrdinalIgnoreCase));

        //             myLogMatches.Add(new MatchedCalls {
        //                 Call = m.Callsign,
        //                 WkdBeforeCount = existingCall?.WkdBeforeCount ?? 1,
        //                 InSuperCheck = false,
        //                 MatchPct = 100.0
        //             });
        //         }
        //     }
        //     else {
        //         myLogMatches = ExecutePatternMatch(myLogCalls, searchText, matchType, similarityType);
        //     }

        //     if (useSuperCheck) {
        //         if (optimizedSuperCheckResults != null) {
        //             superCheckMatches = new List<MatchedCalls>(optimizedSuperCheckResults.Count);
        //             for (int i = 0; i < optimizedSuperCheckResults.Count; i++) {
        //                 var m = optimizedSuperCheckResults[i];
        //                 superCheckMatches.Add(new MatchedCalls {
        //                     Call = m.Callsign,
        //                     WkdBeforeCount = 0,
        //                     InSuperCheck = true,
        //                     MatchPct = 100.0
        //                 });
        //             }
        //         }
        //         else {
        //             superCheckMatches = ExecutePatternMatch(superCheckCalls, searchText, matchType, similarityType, true);
        //         }
        //     }

        //     // ✅ STEP 2: Merge results
        //     Dictionary<string, MatchedCalls> unifiedMatches = MergeResults(myLogMatches, superCheckMatches, useSuperCheck);

        //     // ✅ STEP 3: Calculate scores
        //     foreach (var match in unifiedMatches.Values) {
        //         match.MatchScore = _scorer.CalculateFinalScore(match, searchText, matchType);
        //     }

        //     // ✅ STEP 4: Sort and take top N
        //     var finalMatches = unifiedMatches.Values
        //         .GroupBy(x => x.Call, StringComparer.OrdinalIgnoreCase)
        //         .Select(g => g.OrderByDescending(m => m.MatchScore).First())
        //         .OrderByDescending(x => x.MatchScore)
        //         .ThenBy(x => x.Call)
        //         .Take(_settings.TakeTopMatchesNumber)
        //         .ToList();

        //     return new SearchExecutionResult {
        //         Matches = finalMatches,
        //         SuperCheckMatches = superCheckMatches,
        //         MyLogMatches = myLogMatches
        //     };
        //}

        /// <summary>
        /// Execute pattern matching (delegates to CallSearchMatcher)
        /// </summary>
        /// <summary>
        /// Execute pattern matching (delegates to CallSearchMatcher)
        /// </summary>
        //private List<MatchedCalls> ExecutePatternMatch(
        //    HashSet<MatchedCalls> inputList,
        //    string searchText,
        //    MatchType matchType,
        //    SimilarityMatchType similarityType,
        //    bool isFromSuperCheck = false) {  // ← ADD THIS PARAMETER

        //    List<MatchedCalls> results;

        //    switch (matchType) {
        //        case MatchType.QUICK:
        //            results = _matcher.RunMatchCallQuick(inputList, searchText, 10);
        //            break;

        //        case MatchType.SIMILARITY:
        //            results = similarityType == SimilarityMatchType.MORSE
        //                ? _matcher.RunMatchCallMorse(inputList, searchText, 10)
        //                : _matcher.RunMatchCallSimilarity(inputList, searchText, 10);
        //            break;

        //        default:
        //            results = new List<MatchedCalls>();
        //            break;
        //    }

        //    // ✅ FIX: Set InSuperCheck flag if results are from SuperCheck
        //    if (isFromSuperCheck) {
        //        for (int i = 0; i < results.Count; i++) {
        //            results[i].InSuperCheck = true;
        //        }
        //    }

        //    return results;
        //}

        /// <summary>
        /// Merge MyLog and SuperCheck results
        /// </summary>
        /// <summary>
        /// Merge MyLog and SuperCheck results
        /// OPTIMIZED: Reuses existing objects, no new allocations
        /// </summary>
        //private Dictionary<string, MatchedCalls> MergeResults(
        //    List<MatchedCalls> myLogMatches,
        //    List<MatchedCalls> superCheckMatches,
        //    bool useSuperCheck) {
        //    var unified = new Dictionary<string, MatchedCalls>(
        //        myLogMatches.Count + (useSuperCheck ? superCheckMatches.Count : 0),
        //        StringComparer.OrdinalIgnoreCase
        //    );

        //    // ✅ Add MyLog (direct reference, no copy)
        //    for (int i = 0; i < myLogMatches.Count; i++) {
        //        var match = myLogMatches[i];
        //        unified[match.Call] = match;
        //    }

        //    // ✅ Merge SuperCheck (mutate in place)
        //    if (useSuperCheck) {
        //        for (int i = 0; i < superCheckMatches.Count; i++) {
        //            var match = superCheckMatches[i];
        //            if (unified.TryGetValue(match.Call, out var existing)) {
        //                existing.InSuperCheck = true;
        //            }
        //            else {
        //                unified[match.Call] = match;
        //            }
        //        }
        //    }

        //    return unified;
        //}

        /// <summary>
        /// Format results for display
        /// OPTIMIZED: Direct StringBuilder, no LINQ, no string interpolation
        /// </summary>
        public string FormatResults(List<MatchedCalls> matches,MatchType matchType) {
            if(matches.Count == 0)
                return string.Empty;

            var sb = new System.Text.StringBuilder(matches.Count * 50);

            if(matchType == MatchType.SIMILARITY) {
                for(int i = 0;i < matches.Count;i++) {
                    var x = matches[i];
                    sb.Append(x.Call.PadRight(10));
                    sb.Append(' ');
                    sb.Append(x.GetSearchLabel());
                    sb.Append(" ");
                    sb.Append(x.MatchScore.ToString("F0").PadLeft(3));
                    sb.Append('(');
                    sb.Append(Math.Round(x.MatchPct,0).ToString("F0").PadLeft(3));
                    sb.Append(')');

                    if(i < matches.Count - 1)
                        sb.AppendLine();
                }
            } else {
                for(int i = 0;i < matches.Count;i++) {
                    var x = matches[i];
                    sb.Append(x.Call.PadRight(10));
                    sb.Append(' ');
                    sb.Append(x.GetSearchLabel());
                    sb.Append(" S:");
                    sb.Append(x.MatchScore.ToString("F0").PadLeft(3));

                    if(i < matches.Count - 1)
                        sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// PROCESS EXACT/PARTIAL MATCH CHECKS (extracted from LogProcessing)
        /// Returns contest match info for display
        /// </summary>

        /// <summary>
        /// PROCESS EXACT/PARTIAL MATCH CHECKS with improved algorithm.
        /// Returns contest match info for display in tbExactCallContestMatch.
        /// 
        /// IMPROVEMENTS (v2):
        /// - Configurable thresholds for search activation
        /// - Separate Contains threshold (only activates after MinContainsSearchLength)
        /// - Preserves match type priority (StartsWith before Contains)
        /// - Visual separator between match types
        /// - Handles "/" and special characters consistently
        /// </summary>
        public ContestMatchResult ProcessExactMatchChecks(
            string searchText,
            List<Common.QSODisplay> contestQSOs,
            ContestMatchSearchSettings settings,
            bool isContestMode) {

            // ═══════════════════════════════════════════════════════════════
            // EARLY EXIT: Empty or null input
            // ═══════════════════════════════════════════════════════════════
            if(contestQSOs == null || contestQSOs.Count == 0) {
                return new ContestMatchResult {
                    ExactMatch = false,
                    PartialMatch = false,
                    DisplayText = "",
                    BackgroundColor = System.Windows.Media.Brushes.LightGray
                };
            }

            // ═══════════════════════════════════════════════════════════════
            // EARLY EXIT: Search text too short
            // ═══════════════════════════════════════════════════════════════
            if(string.IsNullOrEmpty(searchText) || searchText.Length < settings.MinSearchLength) {
                return new ContestMatchResult {
                    ExactMatch = false,
                    PartialMatch = false,
                    DisplayText = "",
                    BackgroundColor = System.Windows.Media.Brushes.LightGray
                };
            }

            // ═══════════════════════════════════════════════════════════════
            // SEARCH PHASE: Exact, StartsWith, Contains (conditional)
            // ═══════════════════════════════════════════════════════════════

            // EXACT MATCH (highest priority)
            var exactQSO = contestQSOs.FirstOrDefault(x => x.Call == searchText);
            if(exactQSO != null) {
                return new ContestMatchResult {
                    ExactMatch = true,
                    PartialMatch = true,
                    DisplayText = exactQSO.Call,
                    BackgroundColor = System.Windows.Media.Brushes.Red
                };
            }

            // STARTS WITH (always search if MinSearchLength met)
            var startsWithQSOs = contestQSOs
                .Where(x => x.Call.StartsWith(searchText,StringComparison.OrdinalIgnoreCase))
                .Take(settings.MaxStartsWithResults)
                .ToList();

            // CONTAINS (only if threshold met)
            var containsQSOs = new List<Common.QSODisplay>();
            if(searchText.Length >= settings.MinContainsSearchLength) {
                containsQSOs = contestQSOs
                    .Where(x => x.Call.Contains(searchText) && !x.Call.StartsWith(searchText,StringComparison.OrdinalIgnoreCase))
                    .Take(settings.MaxContainsResults)
                    .ToList();
            }

            // ═══════════════════════════════════════════════════════════════
            // DISPLAY PHASE: Format results with priority preserved
            // ═══════════════════════════════════════════════════════════════

            int totalMatches = startsWithQSOs.Count + containsQSOs.Count;

            // NO MATCHES
            if(totalMatches == 0) {
                return new ContestMatchResult {
                    ExactMatch = false,
                    PartialMatch = false,
                    DisplayText = "",
                    BackgroundColor = System.Windows.Media.Brushes.LightGray
                };
            }

            // SINGLE MATCH (orange background, potential partial match)
            if(totalMatches == 1) {
                var singleMatch = startsWithQSOs.Count > 0 ? startsWithQSOs[0] : containsQSOs[0];
                bool isPartialMatch = searchText.Length >= settings.MinCallLengthForExactMatch && isContestMode;

                return new ContestMatchResult {
                    ExactMatch = false,
                    PartialMatch = isPartialMatch,
                    DisplayText = singleMatch.Call,
                    BackgroundColor = System.Windows.Media.Brushes.Orange
                };
            }

            // MULTIPLE MATCHES: Preserve priority order
            var displayCalls = new List<string>();

            // Add StartsWith matches first (already sorted by most recent from DB)
            displayCalls.AddRange(startsWithQSOs.Select(x => x.Call).Distinct());

            // Add separator if both types exist
            if(startsWithQSOs.Count > 0 && containsQSOs.Count > 0) {
                displayCalls.Add(settings.MatchTypeSeparator);
            }

            // Add Contains matches second
            displayCalls.AddRange(containsQSOs.Select(x => x.Call).Distinct());

            return new ContestMatchResult {
                ExactMatch = false,
                PartialMatch = totalMatches > 0,
                DisplayText = String.Join(", ",displayCalls),
                BackgroundColor = System.Windows.Media.Brushes.LightGray
            };
        }
        /// <summary>
        /// Result container for search execution
        /// </summary>
        public class SearchExecutionResult {
            public List<MatchedCalls> Matches { get; set; }
            public List<MatchedCalls> SuperCheckMatches { get; set; }
            public List<MatchedCalls> MyLogMatches { get; set; }
        }

        /// <summary>
        /// Result for contest exact match check
        /// </summary>
        public class ContestMatchResult {
            public bool ExactMatch { get; set; }
            public bool PartialMatch { get; set; }
            public string DisplayText { get; set; }
            public System.Windows.Media.Brush BackgroundColor { get; set; }
        }
    }
}