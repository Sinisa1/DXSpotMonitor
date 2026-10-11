using Common;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Data {

    public struct SearchResult {
        public List<MatchedCalls> Matches { get; set; }
        public static SearchResult Empty => new SearchResult { Matches = new List<MatchedCalls>() };
    }

    /// <summary>
    /// ADDITIVE scoring — each attribute contributes independent points:
    ///   Score = (MatchPct × SimWeight)
    ///         + (StartsWithBonus)              — if call starts with search text
    ///         + (ContainsBonus)                — if call contains (not starts with)
    ///         + (SuperCheckBonus)              — if call is in SuperCheck DB
    ///         + (WorkedBeforeBonus)            — FLAT bonus if worked at least once
    ///         + (PerQSO × √capped × 10)       — INDEPENDENT √-scaled per QSO count
    ///
    /// WorkedBefore and PerQSO are independent attributes per copilot-instructions.md.
    /// √ curve gives meaningful differentiation without overtaking Base or Position.
    /// All weights stored as prorated integers (÷100) in SearchPrioritySettings.
    /// </summary>
    public sealed class CallMatchScorer {
        private readonly SearchPrioritySettings _settings;

        public CallMatchScorer(SearchPrioritySettings settings) {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }
        private double perQSOScaling = 50.0;
        // ─────────────────────────────────────────────────────────────────────
        // HOT PATH — called for every candidate match
        // ─────────────────────────────────────────────────────────────────────
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double CalculateFinalScore(MatchedCalls match,string searchText,MatchType matchType) {
            double score = match.MatchPct * _settings.WeightSimilarityCalc;

            if(match.StartsWith)
                score += _settings.WeightStartsWithCalc * 100.0;
            else if(match.Contains)
                score += _settings.WeightContainsCalc * 100.0;

            if(match.InSuperCheck)
                score += _settings.WeightSuperCheckCalc * 100.0;

            if(match.WkdBeforeCount > 0)
                score += _settings.WeightWorkedCalc * 100.0;

            if(match.WkdBeforeCount > 0) {
                int capped = Math.Min(match.WkdBeforeCount,50);
                score += _settings.WorkedCountFactorCalc * Math.Sqrt(capped * perQSOScaling)*(1+Math.Log(capped));
            }

            return score;
        }

        // ─────────────────────────────────────────────────────────────────────
        // DEBUG — aligned fixed-width breakdown, all attributes always shown
        // ─────────────────────────────────────────────────────────────────────
        public (double Score,string Breakdown) CalculateWithBreakdown(
            MatchedCalls match,string searchText,MatchType matchType) {

            var sb = new StringBuilder();

            // 1. Base
            double basePts = match.MatchPct * _settings.WeightSimilarityCalc;
            sb.AppendLine(
                $"    1. Base   MatchPct              => {match.MatchPct,6:F2} × {_settings.WeightSimilarityCalc,-5:F2}           = {basePts,8:F2}");

            // 2. Position
            double posPts = 0;
            if(match.StartsWith) {
                posPts = _settings.WeightStartsWithCalc * 100.0;
                sb.AppendLine(
                    $"    2. Pos    StartsWith            =>  100 × {_settings.WeightStartsWithCalc,-5:F2}              = {posPts,8:F2}");
            } else if(match.Contains) {
                posPts = _settings.WeightContainsCalc * 100.0;
                sb.AppendLine(
                    $"    2. Pos    Contains              =>  100 × {_settings.WeightContainsCalc,-5:F2}              = {posPts,8:F2}");
            } else {
                sb.AppendLine(
                    $"    2. Pos    No position match     =>                                = {0,8:F2}");
            }

            // 3. SuperCheck
            double scPts = 0;
            if(match.InSuperCheck) {
                scPts = _settings.WeightSuperCheckCalc * 100.0;
                sb.AppendLine(
                    $"    3. SC     In SuperCheck DB      =>  100 × {_settings.WeightSuperCheckCalc,-5:F2}              = {scPts,8:F2}");
            } else {
                sb.AppendLine(
                    $"    3. SC     Not in SuperCheck     =>                                = {0,8:F2}");
            }

            // 4. WorkedBefore — flat bonus
            double wkdPts = 0;
            if(match.WkdBeforeCount > 0) {
                wkdPts = _settings.WeightWorkedCalc * 100.0;
                sb.AppendLine(
                    $"    4. Wkd    Worked before         =>  100 × {_settings.WeightWorkedCalc,-5:F2}              = {wkdPts,8:F2}");
            } else {
                sb.AppendLine(
                    $"    4. Wkd    Not worked            =>                                = {0,8:F2}");
            }

            // 5. PerQSO — √-scaled
            double qsoPts = 0;
            if(match.WkdBeforeCount > 0) {
                int capped = Math.Min(match.WkdBeforeCount,50);
                //double sqrtVal = Math.Sqrt(capped);
                qsoPts =_settings.WorkedCountFactorCalc * Math.Sqrt(capped * perQSOScaling)*(1+Math.Log(capped));
                sb.AppendLine(
                    $"    5. QSO    {match.WkdBeforeCount,3}×QSO (cap={capped,-2})       =>  {qsoPts,8:F2}");
            } else {
                sb.AppendLine(
                    $"    5. QSO    0×QSO                 =>                                = {0,8:F2}");
            }

            double total = basePts + posPts + scPts + wkdPts + qsoPts;
            sb.Append(
                $"    ► FINAL = {total,7:F2}  (Base={basePts:F1} + Pos={posPts:F1} + SC={scPts:F1} + Wkd={wkdPts:F1} + QSO={qsoPts:F1})");

            return (total,sb.ToString());
        }
    }
}