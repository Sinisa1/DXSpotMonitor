using Dapper;
using Data;
//using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Documents;

namespace ClubLogMatcher {
    public class ClubLogMatcherEngine:DataETLBase {
        private Dictionary<short,EntityRecord> _entities = new();
        private Dictionary<string,List<RuleRecord>> _exceptions = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string,List<RuleRecord>> _prefixes = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string,List<RuleRecord>> _invalidOps = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string,List<ZoneOverrideRecord>> _zoneExceptions = new(StringComparer.OrdinalIgnoreCase);

        public ClubLogMatcherEngine(string connString) : base(connString) {

        }
        object _dbLock= new object();
        /// <summary>
        /// Loads all tables into indexed in-memory dictionaries.
        /// </summary>
        /// <summary>
        /// Loads all tables into indexed in-memory dictionaries using the
        /// connection already managed by DataETLBase (dBManager).
        /// </summary>
        public void LoadFromDatabase() {
            lock(_dbLock) {
                EnsureConnectionOpen();
                IDbConnection db = dBManager.Connection;

                // 1. Entities
                _entities = db.Query<EntityRecord>(
                    "SELECT Adif, Name, Prefix, Deleted, ISNULL(Cqz, 0) AS Cqz, Cont, Longitude, Latitude FROM rbn.clEntity")
                    .ToDictionary(e => e.Adif);

                // 2. Exceptions (Keyed by Call)
                _exceptions = db.Query<RuleRecord>(
                    "SELECT RecordId, Call, Entity, Adif, Cqz, Cont, Longitude, Latitude,  [Start], [End] FROM rbn.clException")
                    .GroupBy(e => e.Call,StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key,g => g.ToList(),StringComparer.OrdinalIgnoreCase);

                // 3. Prefixes (Keyed by Prefix Pattern)
                _prefixes = db.Query<RuleRecord>(
                    "SELECT RecordId, Call, Entity, Adif, Cqz, Cont, Longitude, Latitude, [Start], [End] FROM rbn.clPrefix")
                    .GroupBy(p => p.Call,StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key,g => g.ToList(),StringComparer.OrdinalIgnoreCase);

                // 4. Invalid Operations
                _invalidOps = db.Query<RuleRecord>(
                    "SELECT RecordId, Call,  [Start], [End] FROM rbn.clInvalidOperation")
                    .GroupBy(i => i.Call,StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key,g => g.ToList(),StringComparer.OrdinalIgnoreCase);

                // 5. Zone Exceptions
                _zoneExceptions = db.Query<ZoneOverrideRecord>(
                    "SELECT Call, Zone,  [Start], [End] FROM rbn.clZoneException")
                    .GroupBy(z => z.Call,StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key,g => g.ToList(),StringComparer.OrdinalIgnoreCase);
            }
        }
        /// <summary>
        /// Matches a callsign against Club Log rules for a given timestamp.
        /// </summary>
        public DxccMatchResult Resolve(string rawCallsign,DateTime qsoTime) {
            string cleanCall = ExtractSearchToken(rawCallsign.ToUpper());
            var result = new DxccMatchResult { Callsign = rawCallsign };

            // -------------------------------------------------------------
            // STEP 1: Check Invalid Operations (Pirates / Unaccredited)
            // -------------------------------------------------------------
            if(_invalidOps.TryGetValue(cleanCall,out var invalidList)) {
                if(invalidList.Any(inv => inv.IsActiveAt(qsoTime))) {
                    result.IsInvalid = true;
                    result.MatchType = "InvalidOperation";
                    return result;
                }
            }

            // -------------------------------------------------------------
            // STEP 2: Check Exact Call Exceptions
            // -------------------------------------------------------------
            if(_exceptions.TryGetValue(cleanCall,out var exceptionList)) {
                var match = exceptionList.FirstOrDefault(ex => ex.IsActiveAt(qsoTime));
                if(match != null && _entities.TryGetValue(match.Adif,out var masterEntity)) {
                    PopulateResult(result,masterEntity,match,"ExactException");
                    ApplyZoneOverride(result,cleanCall,qsoTime);
                    return result;
                }
            }

            // -------------------------------------------------------------
            // STEP 3: Longest Prefix Matching (e.g., VE3ZR -> VE3Z -> VE3 -> VE -> V)
            // -------------------------------------------------------------
            for(int len = cleanCall.Length;len >= 1;len--) {
                string searchPrefix = cleanCall.Substring(0,len);

                if(_prefixes.TryGetValue(searchPrefix,out var prefixList)) {
                    var match = prefixList.FirstOrDefault(p => p.IsActiveAt(qsoTime));
                    if(match != null && _entities.TryGetValue(match.Adif,out var masterEntity)) {
                        PopulateResult(result,masterEntity,match,"PrefixMatch");
                        ApplyZoneOverride(result,cleanCall,qsoTime);
                        return result;
                    }
                }
            }

            result.MatchType = "Unknown";
            return result;
        }

        private void PopulateResult(DxccMatchResult res,EntityRecord entity,RuleRecord rule,string matchType) {
            res.Adif = entity.Adif;
            res.EntityName = entity.Name;
            res.PrimaryPrefix = entity.Prefix;
            res.Continent = rule.Cont ?? entity.Cont;
            res.CqZone = rule.Cqz ?? entity.Cqz;
            res.Longitude = rule.Longitude ?? entity.Longitude;
            res.Latitude = rule.Latitude ?? entity.Latitude;
            res.MatchType = matchType;
        }

        private void ApplyZoneOverride(DxccMatchResult res,string call,DateTime qsoTime) {
            if(_zoneExceptions.TryGetValue(call,out var zoneList)) {
                var zoneMatch = zoneList.FirstOrDefault(z => z.IsActiveAt(qsoTime));
                if(zoneMatch != null) {
                    res.CqZone = zoneMatch.Zone;
                }
            }
        }

        /// <summary>
        /// Extracts the active geographic callsign portion (handles slashes like VE3AA/I2).
        /// </summary>
        private string ExtractSearchToken(string rawCallsign) {
            string call = rawCallsign.Trim().ToUpper();
            if(!call.Contains('/')) return call;

            string[] parts = call.Split('/');
            var ignored = new HashSet<string> { "P","M","MM","AM","QRP","A","B" };

            if(parts.Length == 2) {
                if(ignored.Contains(parts[1])) return parts[0]; // e.g. VE3AA/P -> VE3AA
                if(parts[1].Length < parts[0].Length) return parts[1]; // e.g. VE3AA/I2 -> I2
                return parts[0]; // e.g. I2/VE3AA -> I2
            }

            return parts[0];
        }
    }
}