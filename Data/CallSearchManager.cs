using ClubLogMatcher;
using Common;
using Common.SuperCheck;
using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Data {
    public class ContestPfxZone {
        public Band Band { get; set; }
        public QSOMode Mode { get; set; }
        public int CQZ { get; set; }
        public int ITUZ { get; set; }
        public string DXCCPrefix { get; set; }
    }

    public enum MatchType {
        QUICK,
        SIMILARITY
    }

    public enum SimilarityMatchType {
        MORSE,
        STANDARD
    }

    /// <summary>
    /// Fastest callsign search with unified list and cached structures.
    /// Single source of truth, no duplication, optimized for speed.
    /// </summary>
    public class CallSearchManager {
        private SettingsLog _settings;
        private SearchPrioritySettings _prioritySettings;
        private readonly ClubLogMatcherEngine _clubLogMatcherEngine;

        // ===== SINGLE SOURCE OF TRUTH =====
        private List<MatchedCalls> _masterCallList = new List<MatchedCalls>();
        private Dictionary<string, MatchedCalls> _fastLookup = new Dictionary<string, MatchedCalls>(StringComparer.OrdinalIgnoreCase);
        private FrozenDictionary<string, MatchedCalls> _frozenCache;
        private SuperCheckOptimized _trieSearch;
        private readonly object _dataLock = new object();

        // ===== QSO DISPLAY LISTS =====
        public List<QSODisplay> CallsLvl1All = new List<QSODisplay>();
        public List<QSODisplay> CallsLvl1FilterCallPattern { get; private set; }
        public List<QSODisplay> CallsLvl2Band { get; private set; }
        public List<QSODisplay> CallsLvl3BandMode { get; private set; }
        public List<QSODisplay> CallsLvl4BandModeContest { get; private set; }
        public List<QSODisplay> CallsLvl1ContestCurPeriod { get; private set; }
        public List<QSODisplay> CallsLvl1ContestPrevPeriods { get; private set; }

        List<QSODisplay> allContestQSOs { get;  set; }
        // ===== CONTEST-OPTIMIZED LISTS (only used in ContestMode) =====
        public List<QSODisplay> ContestQSOListCurrent { get; private set; } = new List<QSODisplay>();

     public   List<Common.QSODisplay> contestQSOsForMatch;

        public List<QSODisplay> ContestQSOListPrevious { get; private set; } = new List<QSODisplay>();
        // ===== PUBLIC ACCESS =====
        public IReadOnlyList<MatchedCalls> AllCalls => _masterCallList;
        public List<LotWCallsign> LotWCallsigns { get; set; } = new List<LotWCallsign>();
        public bool refreshSearchRequired = false;

        
        public CallSearchManager(SettingsLog sett) {
            _settings = sett;
            _clubLogMatcherEngine= new ClubLogMatcherEngine(_settings.SQLServerConnectionString);
            ResetLists();
        }

        public void SetSettings(SettingsLog sett) {
            _settings = sett;
        }

        public void SetPrioritySettings(SearchPrioritySettings prioritySettings) {
            _prioritySettings = prioritySettings;
        }

        public void LoadSuperCheckCalls(DataTable dt) {
            var superCheckCalls = new List<string>(dt?.Rows.Count ?? 0);

            if (dt != null && dt.Rows.Count > 0) {
                foreach (DataRow row in dt.Rows) {
                    string call = row["CALL"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(call)) {
                        superCheckCalls.Add(call);
                    }
                }
            }

            BuildUnifiedList(superCheckCalls);
        }

        public void LoadClubLogTablesToMatcher() => _clubLogMatcherEngine.LoadFromDatabase();
        public DxccMatchResult ClubLogResolveCall(string call,DateTime dtQSO) => _clubLogMatcherEngine.Resolve( call,  dtQSO);


        private void BuildUnifiedList(List<string> superCheckCalls) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var tempDict = new Dictionary<string, MatchedCalls>(100000, StringComparer.OrdinalIgnoreCase);

            // Phase 1: My log
            if (CallsLvl1All != null && CallsLvl1All.Count > 0) {
                foreach (var group in CallsLvl1All.Where(x => !string.IsNullOrWhiteSpace(x.Call)).GroupBy(x => x.Call.ToUpperInvariant())) {
                    tempDict[group.Key] = new MatchedCalls {
                        Call = group.Key,
                        CallMorse = SimilarityExtensions.ConvertCallToMorse(group.Key),
                        WkdBeforeCount = group.Count(),
                        InSuperCheck = false
                    };
                }
            }

            // Phase 2: SuperCheck
            foreach (var scCall in superCheckCalls) {
                string callUpper = scCall.ToUpperInvariant();
                if (tempDict.TryGetValue(callUpper, out var existing)) {
                    existing.InSuperCheck = true;
                }
                else {
                    tempDict[callUpper] = new MatchedCalls {
                        Call = callUpper,
                        CallMorse = SimilarityExtensions.ConvertCallToMorse(callUpper),
                        WkdBeforeCount = 0,
                        InSuperCheck = true
                    };
                }
            }

            lock (_dataLock) {
                _masterCallList = tempDict.Values.ToList();
                _fastLookup = tempDict;
                _frozenCache = null;
            }

            InitializeTrie();

            stopwatch.Stop();
          //  System.Diagnostics.Debug.WriteLine($"[CallSearchManager] Built {_masterCallList.Count} calls in {stopwatch.ElapsedMilliseconds}ms");
        }

        private void InitializeTrie() {
            try {
                if (_masterCallList == null || _masterCallList.Count == 0) return;

                _trieSearch = new SuperCheckOptimized();
                _trieSearch.LoadCallsigns(_masterCallList.Select(x => x.Call).ToList());

                System.Diagnostics.Debug.WriteLine($"[CallSearchManager] Trie ready with {_masterCallList.Count} calls");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"[CallSearchManager] Trie failed: {ex.Message}");
                _trieSearch = null;
            }
        }

        public void UpdateCallDictionary(string call) {
            if (string.IsNullOrWhiteSpace(call)) return;

            lock (_dataLock) {
                string callUpper = call.ToUpperInvariant();

                if (_fastLookup.TryGetValue(callUpper, out var existing)) {
                    existing.WkdBeforeCount++;
                }
                else {
                    var newCall = new MatchedCalls {
                        Call = callUpper,
                        CallMorse = SimilarityExtensions.ConvertCallToMorse(callUpper),
                        WkdBeforeCount = 1,
                        InSuperCheck = false
                    };
                    _masterCallList.Add(newCall);
                    _fastLookup[callUpper] = newCall;
                }

                _frozenCache = null;
            }
        }

        public async Task<List<MatchedCalls>> SearchCallsAsync(
            string searchText,
            MatchType matchType,
            SimilarityMatchType similarityType,
            bool includeSuperCheck,
            int maxResults = 50,
            CancellationToken cancellationToken = default) {

            if (string.IsNullOrWhiteSpace(searchText)) return new List<MatchedCalls>();

            searchText = searchText.ToUpperInvariant();

            // TRIE SEARCH (QUICK mode)
            if (matchType == MatchType.QUICK && _trieSearch != null) {
                try {
                    var trieMatches = await _trieSearch.FindMatchesAsync(searchText, maxResults * 2, cancellationToken);

                    if (trieMatches != null && trieMatches.Count > 0) {
                        var results = new List<MatchedCalls>(trieMatches.Count);

                        lock (_dataLock) {
                            foreach (var match in trieMatches) {
                                if (_fastLookup.TryGetValue(match.Callsign, out var fullData)) {
                                    if (!includeSuperCheck && fullData.InSuperCheck && fullData.WkdBeforeCount == 0)
                                        continue;

                                    results.Add(new MatchedCalls {
                                        Call = fullData.Call,
                                        CallMorse = fullData.CallMorse,
                                        WkdBeforeCount = fullData.WkdBeforeCount,
                                        InSuperCheck = fullData.InSuperCheck,
                                        MatchPct = 100.0,
                                        StartsWith = true,
                                        Contains = true
                                    });
                                }
                            }
                        }

                        return results.Take(maxResults).ToList();
                    }
                } catch { }
            }

            // LINEAR SCAN (SIMILARITY or fallback)
            return await Task.Run(() => {
                var matches = new ConcurrentBag<MatchedCalls>();
                double minThreshold = 0.3;
                List<MatchedCalls> snapshot;

                lock (_dataLock) {
                    snapshot = _masterCallList;
                }

                if (matchType == MatchType.SIMILARITY && snapshot.Count > 10000) {
                    Parallel.ForEach(snapshot, call => {
                        if (!includeSuperCheck && call.InSuperCheck && call.WkdBeforeCount == 0)
                            return;

                        double similarity = similarityType == SimilarityMatchType.MORSE
                            ? SimilarityExtensions.CalculateSimilarity(searchText, call.Call, "MORSECONVERTBOTH")
                            : SimilarityExtensions.CalculateSimilarity(searchText, call.Call);

                        if (similarity >= minThreshold) {
                            matches.Add(new MatchedCalls {
                                Call = call.Call,
                                CallMorse = call.CallMorse,
                                WkdBeforeCount = call.WkdBeforeCount,
                                InSuperCheck = call.InSuperCheck,
                                MatchPct = similarity * 100.0,
                                StartsWith = call.Call.StartsWith(searchText),
                                Contains = call.Call.Contains(searchText)
                            });
                        }
                    });
                }
                else {
                    foreach (var call in snapshot) {
                        if (!includeSuperCheck && call.InSuperCheck && call.WkdBeforeCount == 0)
                            continue;

                        double similarity;

                        if (matchType == MatchType.SIMILARITY) {
                            similarity = similarityType == SimilarityMatchType.MORSE
                                ? SimilarityExtensions.CalculateSimilarity(searchText, call.Call, "MORSECONVERTBOTH")
                                : SimilarityExtensions.CalculateSimilarity(searchText, call.Call);
                        }
                        else {
                            if (call.Call.Equals(searchText)) similarity = 1.0;
                            else if (call.Call.StartsWith(searchText)) similarity = 0.9;
                            else if (call.Call.Contains(searchText)) similarity = 0.7;
                            else continue;
                        }

                        if (similarity >= minThreshold) {
                            matches.Add(new MatchedCalls {
                                Call = call.Call,
                                CallMorse = call.CallMorse,
                                WkdBeforeCount = call.WkdBeforeCount,
                                InSuperCheck = call.InSuperCheck,
                                MatchPct = similarity * 100.0,
                                StartsWith = call.Call.StartsWith(searchText),
                                Contains = call.Call.Contains(searchText)
                            });
                        }
                    }
                }

                var results = matches.ToList();
                results.Sort((a, b) => b.MatchPct.CompareTo(a.MatchPct));
                return results.Take(maxResults).ToList();

            }, cancellationToken);
        }

        /// <summary>
        /// Rebuild contest-specific QSO lists when contest or year changes.
        /// Call this ONCE when contest/year changes, NOT on every keystroke.
        /// </summary>
        public void RebuildContestLists(string contestID) {
            if(string.IsNullOrEmpty(contestID)) {
                System.Diagnostics.Debug.WriteLine("[CallSearchManager] ❌ RebuildContestLists: contestID is empty");
                ContestQSOListCurrent = new List<QSODisplay>();
                ContestQSOListPrevious = new List<QSODisplay>();
                return;
            }

            if(_settings == null) {
                System.Diagnostics.Debug.WriteLine("[CallSearchManager] ❌ RebuildContestLists: _settings is NULL");
                ContestQSOListCurrent = new List<QSODisplay>();
                ContestQSOListPrevious = new List<QSODisplay>();
                return;
            }

             allContestQSOs = CallsLvl1All
                .Where(x => x.CONTEST_ID == contestID)
                .ToList();

            System.Diagnostics.Debug.WriteLine(
                $"[CallSearchManager] Found {allContestQSOs.Count} QSOs with CONTEST_ID='{contestID}' in CallsLvl1All ({CallsLvl1All.Count} total)");

            ContestQSOListCurrent = allContestQSOs
                .Where(x => _settings.IsCurrentContestPeriod(x.QSO_Begin))
                .ToList();

            ContestQSOListPrevious = allContestQSOs
                .Where(x => _settings.IsPreviousContestPeriod(x.QSO_Begin))
                .ToList();

            System.Diagnostics.Debug.WriteLine(
                $"[CallSearchManager] Contest lists rebuilt for {contestID}: Current={ContestQSOListCurrent.Count}, Previous={ContestQSOListPrevious.Count}");
        }

        public void RebuildContestQSOsForMatchList(SettingsLog settings_,QSOInfo qsoInfo) {
            bool matchBand = settings_.ContInfo.MatchBand;
            bool matchMode = settings_.ContInfo.MatchMode;
            int currentBand = qsoInfo.BAND;
            string currentMode = qsoInfo.MODE;

            if(settings_.ContestMode) {
                // Source directly from ContestQSOListCurrent (already filtered by contest and current period)
                var sourceList = ContestQSOListCurrent;
                if(sourceList == null || sourceList.Count == 0) {
                    contestQSOsForMatch = new List<QSODisplay>();
                    return;
                }

                // Fast path: if neither band nor mode matching is required
                if(!matchBand && !matchMode) {
                    contestQSOsForMatch = new List<QSODisplay>(sourceList);
                    return;
                }

                // Filter small current-contest list (typically 500 - 2,000 QSOs)
                var filtered = new List<QSODisplay>(sourceList.Count);
                for(int i = 0;i < sourceList.Count;i++) {
                    var q = sourceList[i];
                    if(matchBand && q.Band != currentBand) continue;
                    if(matchMode && q.Mode != currentMode) continue;
                    filtered.Add(q);
                }
                contestQSOsForMatch = filtered;
            } else {
                // Non-contest mode: filter general log by band/mode
                var sourceList = CallsLvl1All;
                if(sourceList == null || sourceList.Count == 0) {
                    contestQSOsForMatch = new List<QSODisplay>();
                    return;
                }

                if(!matchBand && !matchMode) {
                    contestQSOsForMatch = new List<QSODisplay>(sourceList);
                    return;
                }

                var filtered = new List<QSODisplay>(sourceList.Count);
                for(int i = 0;i < sourceList.Count;i++) {
                    var q = sourceList[i];
                    if(matchBand && q.Band != currentBand) continue;
                    if(matchMode && q.Mode != currentMode) continue;
                    filtered.Add(q);
                }
                contestQSOsForMatch = filtered;
            }
        }



        /// <summary>
        /// Fast contest search using pre-filtered contest lists.
        /// NO 50K scan — searches small pre-filtered lists only.
        /// </summary>
        public (List<QSODisplay> Current,List<QSODisplay> Previous) SearchContestQSOs(string searchText,int maxPrevious = 15) {

            if(string.IsNullOrEmpty(searchText) || searchText.Length < 1) {
                return (new List<QSODisplay>(),new List<QSODisplay>());
            }

            // Use pre-filtered lists (500-2K items, not 50K)
            var currentResult = SearchContestListAllMatches(ContestQSOListCurrent,searchText);
            var previousResult = SearchContestListAllMatches(ContestQSOListPrevious,searchText)
                .Take(maxPrevious)  // Limit previous year matches
                .ToList();

            return (currentResult,previousResult);
        }

        /// <summary>
        /// Search contest QSO list and return ALL matched QSOs (no grouping, no limit).
        /// Prioritizes exact matches first, then StartsWith, then Contains.
        /// Within each group, most recent QSOs first.
        /// </summary>
        private static List<QSODisplay> SearchContestListAllMatches(List<QSODisplay> qsos,string searchText) {
            if(qsos == null || qsos.Count == 0 || string.IsNullOrEmpty(searchText))
                return new List<QSODisplay>();

            var searchUpper = searchText.ToUpperInvariant();

            // Partition by match type
            var exactMatches = new List<QSODisplay>();
            var startsWithMatches = new List<QSODisplay>();
            var containsMatches = new List<QSODisplay>();

            foreach(var qso in qsos) {
                if(string.IsNullOrEmpty(qso.Call)) continue;

                var callUpper = qso.Call.ToUpperInvariant();

                if(callUpper == searchUpper) {
                    exactMatches.Add(qso);
                } else if(callUpper.StartsWith(searchUpper)) {
                    startsWithMatches.Add(qso);
                } else if(callUpper.Contains(searchUpper)) {
                    containsMatches.Add(qso);
                }
            }

            // Sort each group by most recent first
            var result = new List<QSODisplay>();
            result.AddRange(exactMatches.OrderByDescending(x => x.QSO_Begin));
            result.AddRange(startsWithMatches.OrderByDescending(x => x.QSO_Begin));
            result.AddRange(containsMatches.OrderByDescending(x => x.QSO_Begin));

            return result;
        }


        public RetCode SearchAndFilter(QSOInfo qsoInfo,MatchType matchType,List<MatchedCalls> searchResults) {
            RetCode ret = new RetCode();
            ResetLists();

            // In Contest Mode, ucMatchedCallsDetail uses SearchContestQSOs (ContestQSOListCurrent / Previous).
            // CallsLvl1FilterCallPattern and CallsLvl1ContestCurPeriod are not used. Skip scanning 50K QSOs.
            if(_settings.ContestMode) {
                return ret;
            }

            if(string.IsNullOrEmpty(qsoInfo.stnInfo.CALL) || searchResults == null || searchResults.Count == 0) {
                CallsLvl1FilterCallPattern = new List<QSODisplay>();
                return ret;
            }

            var selectedCalls = PrioritizeMatchedCalls(searchResults,qsoInfo.stnInfo.CALL);
            var matchedCallSet = new HashSet<string>(selectedCalls.Select(x => x.Call),StringComparer.OrdinalIgnoreCase);

            CallsLvl1FilterCallPattern = CallsLvl1All.Where(x => matchedCallSet.Contains(x.Call)).ToList();
            if(CallsLvl1FilterCallPattern.Count == 0) return ret;

            CallsLvl2Band = _settings.ContInfo.MatchBand  ? CallsLvl1FilterCallPattern.Where(x => x.Band == qsoInfo.BAND).ToList()  : CallsLvl1FilterCallPattern;

            CallsLvl3BandMode = _settings.ContInfo.MatchMode    ? CallsLvl2Band.Where(x => x.Mode == qsoInfo.MODE).ToList()   : CallsLvl2Band;

            CallsLvl1ContestCurPeriod = CallsLvl1FilterCallPattern;
            CallsLvl4BandModeContest = CallsLvl3BandMode;

            return ret;
        }

        private List<MatchedCalls> PrioritizeMatchedCalls(List<MatchedCalls> searchResults,string searchText) {
            // Partition results by position type — already scored, just apply count limits
            var exactMatches = searchResults.Where(x => x.Call.Equals(searchText,StringComparison.OrdinalIgnoreCase))
                                                 .OrderByDescending(x => x.MatchScore).ToList();
            var startsWithMatches = searchResults.Where(x => x.StartsWith && !x.Call.Equals(searchText,StringComparison.OrdinalIgnoreCase))
                                                 .OrderByDescending(x => x.MatchScore).ToList();
            var containsMatches = searchResults.Where(x => x.Contains && !x.StartsWith)
                                                 .OrderByDescending(x => x.MatchScore).ToList();
            var otherMatches = searchResults.Where(x => !x.StartsWith && !x.Contains)
                                                 .OrderByDescending(x => x.MatchScore).ToList();

            // Single set of limits — no contest/non-contest split
            int maxStartsWith = _prioritySettings?.MaxStartsWith ?? 15;
            int maxContains = _prioritySettings?.MaxContains   ?? 8;
            int maxOther = _prioritySettings?.MaxOther      ?? 4;
            int maxTotal = _prioritySettings?.MaxTotal      ?? 30;

            var selected = new List<MatchedCalls>(maxTotal);
            selected.AddRange(exactMatches);
            int remaining = maxTotal - selected.Count;

            if(remaining > 0) {
                selected.AddRange(startsWithMatches.Take(Math.Min(maxStartsWith,remaining)));
                remaining = maxTotal - selected.Count;
            }
            if(remaining > 0) {
                selected.AddRange(containsMatches.Take(Math.Min(maxContains,remaining)));
                remaining = maxTotal - selected.Count;
            }
            if(remaining > 0 && maxOther > 0) {
                selected.AddRange(otherMatches.Take(Math.Min(maxOther,remaining)));
            }

            return selected;
        }
        private void FilterByContest() {
            string contestID = _settings.ContInfo.CabrilloCode;

            CallsLvl1ContestCurPeriod = CallsLvl1FilterCallPattern
                .Where(x => x.CONTEST_ID == contestID && _settings.IsCurrentContestPeriod(x.QSO_Begin))
                .ToList();

            CallsLvl1ContestPrevPeriods = CallsLvl1FilterCallPattern
                .Where(x => x.CONTEST_ID == contestID && _settings.IsPreviousContestPeriod(x.QSO_Begin))
                .Take(15)
                .ToList();

            CallsLvl4BandModeContest = CallsLvl3BandMode
                .Where(x => x.CONTEST_ID == contestID && _settings.IsCurrentContestPeriod(x.QSO_Begin))
                .ToList();
        }

        public void RemoveQSO(QSODisplay toRemove) {
            if(toRemove != null) {
                CallsLvl1All.Remove(toRemove);
                ContestQSOListCurrent.Remove(toRemove);
                ContestQSOListPrevious?.Remove(toRemove); // ContestQSOListPrevious is static — remove defensively
            }
        }

        private void ResetLists() {
            CallsLvl1ContestCurPeriod = new List<QSODisplay>();
            CallsLvl1ContestPrevPeriods = new List<QSODisplay>();
            CallsLvl2Band = new List<QSODisplay>();
            CallsLvl3BandMode = new List<QSODisplay>();
            CallsLvl4BandModeContest = new List<QSODisplay>();
            CallsLvl1FilterCallPattern = new List<QSODisplay>();
        }

        public string[] QSODisplayToStringArrayConcatenateCallBandCount(List<QSODisplay> qsoDisp) {
            return qsoDisp.GroupBy(x => $"{x.Call}|{x.Band}").Select(x => $"{x.Key}{(x.Count() > 1 ? $"({x.Count()})" : "")}").OrderBy(x => x).ToArray();
        }

        public List<QSODisplay> FrequencySearch() {
            if (!_settings.ContestMode) return new List<QSODisplay>();
            double rangeKHZ = _settings.Mode == "CW" ? 0.5 : 2.5;
            return CallsLvl1All.Where(x => x.CONTEST_ID == _settings.ContInfo.CabrilloCode && Math.Abs((double)x.FREQ_RX_KHZ - _settings.RcvFreqkHz) < rangeKHZ && Util.DateDiffHours(x.QSO_Begin, DateTime.Now) < 12.0).OrderByDescending(x => x.QSO_Begin).Take(10).ToList();
        }

        public List<string> QSODisplayToListOfDistinctCalls() {
            return CallsLvl4BandModeContest.Select(x => x.Call).Distinct().OrderBy(x => x).ToList();
        }

        public IEnumerable<QSODisplay> GetDXCCQSOs(QSOInfo qsoInfo) {
            return CallsLvl1All.Where(x => x.DXCCPrefix == qsoInfo.stnInfo.DXCCPrefix).ToList();
        }

        public void BuildMasterCallDictionary() {
            BuildUnifiedList(new List<string>());
        }

        public void InitializeOptimizedSearch() { }

        //public async Task<(List<SuperCheckMatch>, List<SuperCheckMatch>)> FindMatchesOptimizedAsync(string partial, int maxResults = 50, CancellationToken cancellationToken = default) {
        //    var results = await SearchCallsAsync(partial, MatchType.QUICK, SimilarityMatchType.STANDARD, true, maxResults, cancellationToken);
        //    return (
        //        results.Where(x => x.InSuperCheck).Select(x => new SuperCheckMatch { Callsign = x.Call }).ToList(),
        //        results.Where(x => x.WkdBeforeCount > 0).Select(x => new SuperCheckMatch { Callsign = x.Call }).ToList()
        //    );
        //}

        public async Task<List<MatchedCalls>> FindMatchesOptimizedAsync(string partial,bool includeSuperCheck,int maxResults = 50,CancellationToken cancellationToken = default) {
            return await SearchCallsAsync(partial,MatchType.QUICK,SimilarityMatchType.STANDARD,includeSuperCheck,maxResults,cancellationToken);
        }

                public List<MatchedCalls> FindMatchesOptimized(string partial, bool includeSuperCheck, int maxResults = 50) {
            return SearchCalls(partial, MatchType.QUICK, SimilarityMatchType.STANDARD, includeSuperCheck, maxResults);
        }

        public List<MatchedCalls> SearchCalls(
            string searchText,
            MatchType matchType,
            SimilarityMatchType similarityType,
            bool includeSuperCheck,
            int maxResults = 50) {

            if (string.IsNullOrWhiteSpace(searchText)) return new List<MatchedCalls>();

            searchText = searchText.ToUpperInvariant();

            // FAST TRIE PATH
            if (matchType == MatchType.QUICK && _trieSearch != null) {
                try {
                    var trieMatches = _trieSearch.FindMatches(searchText, maxResults * 2);

                    if (trieMatches != null && trieMatches.Count > 0) {
                        var results = new List<MatchedCalls>(trieMatches.Count);

                        lock (_dataLock) {
                            foreach (var match in trieMatches) {
                                if (_fastLookup.TryGetValue(match.Callsign, out var fullData)) {
                                    if (!includeSuperCheck && fullData.InSuperCheck && fullData.WkdBeforeCount == 0)
                                        continue;

                                    results.Add(new MatchedCalls {
                                        Call = fullData.Call,
                                        CallMorse = fullData.CallMorse,
                                        WkdBeforeCount = fullData.WkdBeforeCount,
                                        InSuperCheck = fullData.InSuperCheck,
                                        MatchPct = 100.0,
                                        StartsWith = true,
                                        Contains = true
                                    });
                                }
                            }
                        }

                        return results.Take(maxResults).ToList();
                    }
                } catch { }
            }

            // Fallback: synchronous list scan
            var allCalls = _masterCallList;
            if (allCalls == null || allCalls.Count == 0) return new List<MatchedCalls>();

            var coordinator = new CallSearchCoordinator(_prioritySettings);
            var quickResult = coordinator.ExecuteSearchWithList(searchText, matchType, similarityType, allCalls, includeSuperCheck);
            return quickResult.Matches;
        }
    }
}