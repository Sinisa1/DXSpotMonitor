using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Data {
    /// <summary>
    /// Contains ALL matching logic extracted from LogProcessing.
    /// Pure synchronous - no async/await.
    /// THIS REPLACES: RunMatchCallSimilarity, RunMatchCallQuick, RunMatchCallMorse
    /// </summary>
    public sealed class CallSearchMatcher {
        private readonly SearchPrioritySettings _settings;

        public CallSearchMatcher(SearchPrioritySettings settings) {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// REPLACES LogProcessing.RunMatchCallSimilarity()
        /// EXACT SAME LOGIC - just extracted
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<MatchedCalls> RunMatchCallSimilarity(HashSet<MatchedCalls> inputList, string searchText, int takeNum = 8) {
            List<MatchedCalls> allMatches = new List<MatchedCalls>(takeNum * 3);

            foreach (var item in inputList) {
                double rawSimilarity = SimilarityExtensions.CalculateSimilarity(searchText, item.Call);

                if (rawSimilarity <= _settings.MinPercentThresholdSimilarity / 100.0) {
                    continue;
                }

                var matchedCall = new MatchedCalls {
                    Call = item.Call,
                    WkdBeforeCount = item.WkdBeforeCount,
                    InSuperCheck = item.InSuperCheck,
                    MatchPct = 100.0 *rawSimilarity
                };

                allMatches.Add(matchedCall);
            }

            return allMatches;
        }

        /// <summary>
        /// REPLACES LogProcessing.RunMatchCallQuick()
        /// EXACT SAME LOGIC - just extracted
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<MatchedCalls> RunMatchCallQuick(HashSet<MatchedCalls> inputList, string searchText, int takeNum = 8) {
            var results = new List<MatchedCalls>();

            foreach (var item in inputList) {
                double matchQuality = 0.0;

                if (item.Call.Equals(searchText, StringComparison.OrdinalIgnoreCase)) {
                    matchQuality = 1.0;
                }
                else if (item.Call.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)) {
                    matchQuality = 0.9 - (0.1 * (item.Call.Length - searchText.Length) / 10.0);
                }
                else if (item.Call.Contains(searchText, StringComparison.OrdinalIgnoreCase)) {
                    matchQuality = 0.7 - (0.1 * (item.Call.Length - searchText.Length) / 10.0);
                }
                else {
                    continue;
                }

                matchQuality = Math.Max(0.4, Math.Min(1.0, matchQuality));

                results.Add(new MatchedCalls {
                    Call = item.Call,
                    WkdBeforeCount = item.WkdBeforeCount,
                    InSuperCheck = item.InSuperCheck,
                    MatchPct = 100.0 *matchQuality
                });
            }

            var sorted = results
                .OrderByDescending(x => x.MatchPct)
                .ThenBy(x => x.Call)
                .Take(takeNum)
                .ToList();

            return sorted;
        }

        /// <summary>
        /// REPLACES LogProcessing.RunMatchCallMorse()
        /// EXACT SAME LOGIC - just extracted
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<MatchedCalls> RunMatchCallMorse(HashSet<MatchedCalls> inputList, string searchText, int takeNum = 8) {
            int len = searchText.Length;
            List<MatchedCalls> matchedOutList = new List<MatchedCalls>();
            string searchTextMorse = SimilarityExtensions.ConvertCallToMorse(searchText);

            List<MatchedCalls> startsAll = inputList
                .Where(x => x.CallMorse.StartsWith(searchTextMorse))
                .Select(x => new MatchedCalls {
                    Call = x.Call,
                    WkdBeforeCount = x.WkdBeforeCount,
                    MatchPct = SimilarityExtensions.CalculateSimilarity(searchTextMorse, x.CallMorse)
                })
                .OrderByDescending(x => x.MatchPct)
                .ThenBy(x => x.Call)
                .ToList();

            List<MatchedCalls> containsAll = inputList
                .Where(x => x.CallMorse.Contains(searchTextMorse))
                .Select(x => new MatchedCalls {
                    Call = x.Call,
                    WkdBeforeCount = x.WkdBeforeCount,
                    MatchPct = SimilarityExtensions.CalculateSimilarity(searchTextMorse, x.CallMorse)
                })
                .OrderByDescending(x => x.MatchPct)
                .ThenBy(x => x.Call)
                .ToList();

            List<MatchedCalls> othersAll = new List<MatchedCalls>();
            if (len > 2) {
                othersAll = inputList
                    .Where(x => !x.CallMorse.StartsWith(searchTextMorse) &&
                                !x.CallMorse.Contains(searchTextMorse))
                    .Select(x => new MatchedCalls {
                        Call = x.Call,
                        WkdBeforeCount = x.WkdBeforeCount,
                        MatchPct = SimilarityExtensions.CalculateSimilarity(searchTextMorse, x.CallMorse)
                    })
                    .OrderByDescending(x => x.MatchPct)
                    .ThenBy(x => x.Call)
                    .ToList();
            }

            matchedOutList.AddRange(startsAll.Where(x => x.MatchPct > .4).Take(takeNum));
            matchedOutList.AddRange(containsAll.Where(x => x.MatchPct > .4 &&
                                    matchedOutList.Count(y => y.Call == x.Call) == 0).Take(takeNum));
            matchedOutList.AddRange(othersAll.Where(x => x.MatchPct > .4 &&
                                    matchedOutList.Count(y => y.Call == x.Call) == 0).Take(takeNum));

            return matchedOutList;
        }
    }
}