using System.Data;
using Microsoft.Data.SqlClient;
using DXSpotMonitor.Models;

namespace DXSpotMonitor.Services;

/// <summary>
/// Service to query STG_DXSpider1 and manage dx schema tables
/// ✅ Updated for multi-user dx schema
/// </summary>
public class DxSpotDatabaseService {
    private readonly string _connectionString;

    public DxSpotDatabaseService(string connectionString) {
        _connectionString = connectionString;
    }

    #region User Authentication
    /// <summary>
    /// Get all registered users
    /// </summary>
    public async Task<List<DxSpotAlertUser>> GetAllUsersAsync() {
        var users = new List<DxSpotAlertUser>();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
        SELECT UserCall, PIN, UserName, Comment
        FROM dx.DXSpotAlertUser";

        using var command = new SqlCommand(sql,connection);
        using var reader = await command.ExecuteReaderAsync();

        while(await reader.ReadAsync()) {
            users.Add(new DxSpotAlertUser {
                UserCall = reader.GetString(0),
                PIN = reader.GetInt32(1),
                UserName = reader.IsDBNull(2) ? null : reader.GetString(2),
                Comment = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
        }

        return users;
    }


    /// <summary>
    /// Add new user
    /// </summary>
    public async Task AddUserAsync(DxSpotAlertUser user) {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
        INSERT INTO dx.DXSpotAlertUser (UserCall, PIN, UserName, Comment)
        VALUES (@UserCall, @PIN, @UserName, @Comment)";

        using var command = new SqlCommand(sql,connection);
        command.Parameters.AddWithValue("@UserCall",user.UserCall.ToUpper());
        command.Parameters.AddWithValue("@PIN",user.PIN);
        command.Parameters.AddWithValue("@UserName",(object?)user.UserName ?? DBNull.Value);
        command.Parameters.AddWithValue("@Comment",(object?)user.Comment ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
        System.Diagnostics.Debug.WriteLine($"✓ Added user: {user.UserCall}");
    }

    /// <summary>
    /// Update existing user
    /// </summary>
    public async Task UpdateUserAsync(DxSpotAlertUser user) {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
        UPDATE dx.DXSpotAlertUser
        SET PIN = @PIN,
            UserName = @UserName,
            Comment = @Comment
        WHERE UserCall = @UserCall";

        using var command = new SqlCommand(sql,connection);
        command.Parameters.AddWithValue("@UserCall",user.UserCall.ToUpper());
        command.Parameters.AddWithValue("@PIN",user.PIN);
        command.Parameters.AddWithValue("@UserName",(object?)user.UserName ?? DBNull.Value);
        command.Parameters.AddWithValue("@Comment",(object?)user.Comment ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
        System.Diagnostics.Debug.WriteLine($"✓ Updated user: {user.UserCall}");
    }

    /// <summary>
    /// Delete user (cascade will delete their filters)
    /// </summary>
    public async Task DeleteUserAsync(string userCall) {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
        DELETE FROM dx.DXSpotAlertUser
        WHERE UserCall = @UserCall";

        using var command = new SqlCommand(sql,connection);
        command.Parameters.AddWithValue("@UserCall",userCall.ToUpper());

        await command.ExecuteNonQueryAsync();
        System.Diagnostics.Debug.WriteLine($"✗ Deleted user: {userCall}");
    }


    /// <summary>
    /// Authenticate user with callsign and PIN
    /// </summary>
    public async Task<DxSpotAlertUser?> AuthenticateUserAsync(string userCall,int pin) {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT UserCall, PIN, UserName, Comment
            FROM dx.DXSpotAlertUser
            WHERE UserCall = @UserCall AND PIN = @PIN";

        using var command = new SqlCommand(sql,connection);
        command.Parameters.AddWithValue("@UserCall",userCall.ToUpper());
        command.Parameters.AddWithValue("@PIN",pin);

        using var reader = await command.ExecuteReaderAsync();
        if(await reader.ReadAsync()) {
            return new DxSpotAlertUser {
                UserCall = reader.GetString(0),
                PIN = reader.GetInt32(1),
                UserName = reader.IsDBNull(2) ? null : reader.GetString(2),
                Comment = reader.IsDBNull(3) ? null : reader.GetString(3)
            };
        }

        return null;
    }

    #endregion

    #region Spot Queries

    /// <summary>
    /// Get recent spots matching filter criteria for a specific user
    /// </summary>
    public async Task<List<DxSpotMessage>> GetMatchingSpotsAsync(
        string userCall,
        List<DxSpotFilter> filters,
        DateTime sinceTimestamp) {
        var results = new List<DxSpotMessage>();

        if(!filters.Any(f => f.IsEnabled && !string.IsNullOrWhiteSpace(f.Callsign)))
            return results;

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        foreach(var filter in filters.Where(f => f.IsEnabled && !string.IsNullOrWhiteSpace(f.Callsign))) {
            System.Diagnostics.Debug.WriteLine($"");
            System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine($"QUERYING FOR USER: {userCall}, DX CALL: {filter.Callsign}, MODE: {filter.Mode}");
            System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════════════════════════");

            var sql = BuildDynamicQuery(filter,sinceTimestamp);

            System.Diagnostics.Debug.WriteLine($"Generated SQL:");
            System.Diagnostics.Debug.WriteLine(sql);

            using var command = new SqlCommand(sql,connection);
            command.Parameters.AddWithValue("@Callsign",filter.Callsign.ToUpper());
            command.Parameters.AddWithValue("@SinceTime",sinceTimestamp);

            System.Diagnostics.Debug.WriteLine($"Parameters:");
            System.Diagnostics.Debug.WriteLine($"  @Callsign = '{filter.Callsign.ToUpper()}'");
            System.Diagnostics.Debug.WriteLine($"  @SinceTime = {sinceTimestamp:yyyy-MM-dd HH:mm:ss} UTC");

            try {
                int rowCount = 0;
                using var reader = await command.ExecuteReaderAsync();

                while(await reader.ReadAsync()) {
                    var spot = MapToSpotMessage(reader);
                    results.Add(spot);
                    rowCount++;

                    System.Diagnostics.Debug.WriteLine($"  Row {rowCount}: {spot.Call} on {spot.Band} {spot.Mode}");
                }

                System.Diagnostics.Debug.WriteLine($"  ✓ Returned {rowCount} row(s)");
            } catch(Exception ex) {
                System.Diagnostics.Debug.WriteLine($"❌ SQL Error: {ex.Message}");
            }
        }

        return results;
    }

    /// <summary>
    /// Get the most recent spot matching ANY enabled filter for testing
    /// ✅ FIXED: Loops through ALL enabled filters until a spot is found
    /// ✅ FIXED: Updates UTC to current time so viewer won't reject it
    /// ✅ FIXED: Falls back to ANY spot ever recorded for the selected recipient's filters
    /// ✅ FIXED: Falls back to a generic synthetic CW spot if no spot exists at all — always returns something
    /// </summary>
    public async Task<DxSpotMessage?> GetLatestTestSpotAsync(string userCall,List<DxSpotFilter> filters) {
        var enabledFilters = filters
            .Where(f => f.IsEnabled && !string.IsNullOrWhiteSpace(f.Callsign))
            .ToList();

        if(!enabledFilters.Any()) {
            System.Diagnostics.Debug.WriteLine("No enabled filters for test spot - using generic synthetic spot");
            return BuildGenericTestSpot(userCall);
        }

        System.Diagnostics.Debug.WriteLine($"🧪 Getting test spot for: {userCall} - checking {enabledFilters.Count} enabled filters");

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // ✅ Pass 1: recent spots (last 24 hours)
        var recentSpot = await TryFindSpotForFiltersAsync(connection,enabledFilters,DateTime.UtcNow.AddHours(-24));
        if(recentSpot != null)
            return recentSpot;

        System.Diagnostics.Debug.WriteLine($"⚠ No test spot found in last 24 hours for any of {enabledFilters.Count} enabled filters - trying ANY spot ever recorded");

        // ✅ Pass 2: ANY spot ever recorded for these filters, regardless of age
        var anySpot = await TryFindSpotForFiltersAsync(connection,enabledFilters,DateTime.MinValue);
        if(anySpot != null)
            return anySpot;

        System.Diagnostics.Debug.WriteLine("⚠ No spot found at all for selected recipient's filters - generating generic synthetic CW test spot");

        // ✅ Pass 3: nothing exists in the DB at all - build a generic CW spot so the test always returns something
        return BuildGenericTestSpot(userCall);
    }

    /// <summary>
    /// Searches all provided filters for a matching spot since the given timestamp.
    /// Returns the first match found (marked as a TEST spot), or null if none found.
    /// </summary>
    private async Task<DxSpotMessage?> TryFindSpotForFiltersAsync(SqlConnection connection,List<DxSpotFilter> enabledFilters,DateTime sinceTime) {
        foreach(var filter in enabledFilters) {
            System.Diagnostics.Debug.WriteLine($"  Trying filter: {filter.Callsign} {filter.Mode} (since {sinceTime:yyyy-MM-dd HH:mm:ss} UTC)");

            var sql = BuildDynamicQuery(filter,sinceTime);

            using var command = new SqlCommand(sql,connection);
            command.Parameters.AddWithValue("@Callsign",filter.Callsign.ToUpper());
            command.Parameters.AddWithValue("@SinceTime",sinceTime);

            try {
                var spots = new List<DxSpotMessage>();
                int rowCount = 0;

                using var reader = await command.ExecuteReaderAsync();

                while(await reader.ReadAsync()) {
                    rowCount++;
                    try {
                        var spot = MapToSpotMessage(reader);
                        spots.Add(spot);
                        System.Diagnostics.Debug.WriteLine($"    Row {rowCount}: {spot.Call} @ {spot.Frequency:F3} MHz ({spot.Band} {spot.Mode}) at {spot.UTC:HH:mm:ss}");
                    } catch(Exception mapEx) {
                        System.Diagnostics.Debug.WriteLine($"    ⚠ Error mapping row {rowCount}: {mapEx.Message}");
                    }
                }

                reader.Close();

                System.Diagnostics.Debug.WriteLine($"    Retrieved {rowCount} row(s) for filter {filter.Callsign}");

                if(spots.Any()) {
                    var latestSpot = spots.First();
                    System.Diagnostics.Debug.WriteLine($"✅ Test spot found: {latestSpot.Call} on {latestSpot.Band} {latestSpot.Mode}");

                    // ✅ FIX: Mark as test message
                    latestSpot.Spotter = $"{latestSpot.Spotter}=>TEST";

                    // ✅ FIX: Update UTC to current time so viewer won't reject it as "old"
                    latestSpot.UTC = DateTime.UtcNow;
                    latestSpot.RptByType = "SKM"; // Always SKM to trigger sound and vibration
                    return latestSpot;
                }

                System.Diagnostics.Debug.WriteLine($"    No spots found for {filter.Callsign}, trying next filter...");
            } catch(Exception ex) {
                System.Diagnostics.Debug.WriteLine($"    ❌ Query error for {filter.Callsign}: {ex.Message}");
            }
        }

        return null;
    }

    /// <summary>
    /// Builds a generic synthetic CW test spot used when there is no spot data available at all.
    /// Guarantees GetLatestTestSpotAsync always returns something.
    /// </summary>
    private DxSpotMessage BuildGenericTestSpot(string userCall) {
        return new DxSpotMessage {
            Type = "dx_spot",
            UTC = DateTime.UtcNow,
            Call = "YU7AOP",
            Frequency = 14.030,
            Mode = "CW",
            Band = "20m",
            DXCC = "",
            CQZone = 0,
            Spotter = $"{userCall}=>TEST",
            RptByType = "SKM", // Always SKM to trigger sound and vibration
            Comment = "Generic synthetic CW test spot - no matching spots found in database"
        };
    }

    /// <summary>
    /// Build dynamic SQL query
    /// ✅ Respects AnyBand setting and CQ Zone filtering
    /// </summary>
    private string BuildDynamicQuery(DxSpotFilter filter,DateTime sinceTime) {
        var modesSQL = filter.GetModesForSQL();
        var zonesSQL = filter.GetCQZonesForSQL();

        var modeFilter = !string.IsNullOrEmpty(modesSQL)
            ? $"AND Mode IN ({modesSQL})"
            : "";

        string bandFilter;
        if(filter.AnyBand) {
            bandFilter = "";
            System.Diagnostics.Debug.WriteLine($"  Band filter: ANY BAND");
        } else {
            var bandsSQL = filter.GetBandsForSQL();
            if(bandsSQL == "0" || string.IsNullOrEmpty(bandsSQL)) {
                bandFilter = "AND 1=0";
                System.Diagnostics.Debug.WriteLine($"  Band filter: NO BANDS SELECTED");
            } else {
                bandFilter = $"AND Band IN ({bandsSQL})";
                System.Diagnostics.Debug.WriteLine($"  Band filter: {bandsSQL}");
            }
        }

        string zoneFilter;
        if(string.IsNullOrEmpty(zonesSQL) || zonesSQL == "0") {
            zoneFilter = "";
            System.Diagnostics.Debug.WriteLine($"  Zone filter: ANY ZONE");
        } else {
    //        zoneFilter = $"AND RptCQZ IN ({zonesSQL})";
            zoneFilter = $"AND  ( dbo.IsItemFromCSVList1InCsvList2Integer('{zonesSQL}',CQZones)>0    OR   (RptCQZ IN ({zonesSQL})    and  RptByType='STN')) ";  // If  any CQZ from GUI selected CQZ(zonesSQL) list is in rbn.STG_DXSpider1.CQZones or if the STN reportCQZ is in zonesSQL list.
            System.Diagnostics.Debug.WriteLine($"  Zone filter: CQ Zones {zonesSQL}");
        }

        // ✅ Added RptByType to SELECT list
        return $@"
        SELECT TOP 100
            DXCall, Freq, Band, Mode, RptByCall, RptByType, UTC,
            Comment, RptCQZ, CountryName
        FROM (
            SELECT 
                DXCall, Freq, Band, Mode, RptByCall, RptByType, UTC,
                Comment, RptCQZ, CountryName,
                ROW_NUMBER() OVER (
                    PARTITION BY DXCall, Band, Mode 
                    ORDER BY UTC DESC
                ) AS RowNum
            FROM rbn.STG_DXSpider1
            WHERE DXCall = @Callsign
              AND UTC > @SinceTime
              {modeFilter}
              {bandFilter}
              {zoneFilter}
              AND   RptByCall not in ( select Call from dx.IgnoreSpotterCall)
        ) AS RankedSpots
        WHERE RowNum = 1
        ORDER BY UTC DESC";
    }
    private DxSpotMessage MapToSpotMessage(SqlDataReader reader) {
        decimal freqDec = reader.GetDecimal(reader.GetOrdinal("Freq"));
        double freqDbl = (double)freqDec;

        int band = reader.IsDBNull(reader.GetOrdinal("Band"))
            ? GetBandFromFrequency(freqDbl)
            : reader.GetInt32(reader.GetOrdinal("Band"));

        string bandStr = $"{band}m";

        return new DxSpotMessage {
            Type = "dx_spot",
            Call = reader.GetString(reader.GetOrdinal("DXCall")),
            Frequency = freqDbl,
            Mode = reader.IsDBNull(reader.GetOrdinal("Mode")) ? "SSB" : reader.GetString(reader.GetOrdinal("Mode")),
            Band = bandStr,

            // ✅ UTC from database column (not C# DateTime.UtcNow)
            UTC = reader.GetDateTime(reader.GetOrdinal("UTC")),

            // ✅ Spotter mapped from RptByCall
            Spotter = reader.GetString(reader.GetOrdinal("RptByCall")),

            // ✅ NEW: RptByType (3-letter code)
            RptByType = reader.IsDBNull(reader.GetOrdinal("RptByType")) ? "" : reader.GetString(reader.GetOrdinal("RptByType")),
            Comment = reader.IsDBNull(reader.GetOrdinal("Comment")) ? "" : reader.GetString(reader.GetOrdinal("Comment")),
            CQZone = reader.IsDBNull(reader.GetOrdinal("RptCQZ")) ? 0 : reader.GetInt32(reader.GetOrdinal("RptCQZ")),
            DXCC = reader.IsDBNull(reader.GetOrdinal("CountryName")) ? "" : reader.GetString(reader.GetOrdinal("CountryName"))

            // ✅ REMOVED: Continent, ITUZone, Needed
            // LocalTime is computed property (UTC.ToLocalTime())
        };
    }

    private int GetBandFromFrequency(double freqMhz) {
        if(freqMhz >= 1.8 && freqMhz <= 2.0) return 160;
        if(freqMhz >= 3.5 && freqMhz <= 4.0) return 80;
        if(freqMhz >= 7.0 && freqMhz <= 7.3) return 40;
        if(freqMhz >= 10.1 && freqMhz <= 10.15) return 30;
        if(freqMhz >= 14.0 && freqMhz <= 14.35) return 20;
        if(freqMhz >= 18.068 && freqMhz <= 18.168) return 17;
        if(freqMhz >= 21.0 && freqMhz <= 21.45) return 15;
        if(freqMhz >= 24.89 && freqMhz <= 24.99) return 12;
        if(freqMhz >= 28.0 && freqMhz <= 29.7) return 10;
        if(freqMhz >= 50.0 && freqMhz <= 54.0) return 6;
        return 0;
    }

    #endregion

    #region Alert History

    /// <summary>
    /// Check if spot was already sent to this user recently
    /// </summary>
    public async Task<bool> WasRecentlySentAsync(string userCall,string call,int band,string mode,int withinMinutes) {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT COUNT(*)
            FROM dx.DXSpotAlertHistory
            WHERE UserCall = @UserCall
              AND DXCall = @Call
              AND Band = @Band
              AND Mode = @Mode
              AND SentTimestampUTC > @SinceUTC";

        using var command = new SqlCommand(sql,connection);
        command.Parameters.AddWithValue("@UserCall",userCall.ToUpper());
        command.Parameters.AddWithValue("@Call",call.ToUpper());
        command.Parameters.AddWithValue("@Band",band);
        command.Parameters.AddWithValue("@Mode",mode);
        command.Parameters.AddWithValue("@SinceUTC",DateTime.UtcNow.AddMinutes(-withinMinutes));

        var count = (int)await command.ExecuteScalarAsync();
        return count > 0;
    }

    /// <summary>
    /// Record that spot was sent to user
    /// </summary>
    /// <summary>
    /// Record that spot was sent to user
    /// ✅ Updated to use UTC property instead of Timestamp
    /// </summary>
    public async Task RecordSentSpotAsync(string userCall,DxSpotMessage spot) {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            INSERT INTO dx.DXSpotAlertHistory 
            (UserCall, DXCall, Band, Mode, Frequency, SpotterCall, SentTimestampUTC, CQZones)
            VALUES (@UserCall, @Call, @Band, @Mode, @Frequency, @Spotter, GETUTCDATE(), @CQZones)";

        using var command = new SqlCommand(sql,connection);
        command.Parameters.AddWithValue("@UserCall",userCall.ToUpper());
        command.Parameters.AddWithValue("@Call",spot.Call);
        command.Parameters.AddWithValue("@Band",int.Parse(spot.Band.Replace("m","")));
        command.Parameters.AddWithValue("@Mode",spot.Mode);
        command.Parameters.AddWithValue("@Frequency",spot.Frequency);
        command.Parameters.AddWithValue("@Spotter",spot.Spotter);
        command.Parameters.AddWithValue("@CQZones",spot.CQZone > 0 ? spot.CQZone.ToString() : (object)DBNull.Value);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Clean up old alert history for all users
    /// </summary>
    public async Task CleanupOldAlertsAsync(int olderThanHours = 24) {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            DELETE FROM dx.DXSpotAlertHistory
            WHERE SentTimestampUTC < @CutoffUTC";

        using var command = new SqlCommand(sql,connection);
        command.Parameters.AddWithValue("@CutoffUTC",DateTime.UtcNow.AddHours(-olderThanHours));

        var deleted = await command.ExecuteNonQueryAsync();
        System.Diagnostics.Debug.WriteLine($"Cleaned up {deleted} old alert records");
    }

    #endregion

    #region Settings Management

    /// <summary>
    /// Load filter settings for a specific user
    /// </summary>
    public async Task<List<DxSpotFilter>> LoadSettingsAsync(string userCall) {
        var filters = new List<DxSpotFilter>();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                UserCall, DXCall, Mode, IsEnabled, NotifyPeriodMinutes, AnyBand,
                Band160m, Band80m, Band40m, Band30m, Band20m, 
                Band17m, Band15m, Band12m, Band10m, Band6m,
                CQZones, LastUpdated
            FROM dx.DXSpotAlertSettings
            WHERE UserCall = @UserCall
            ORDER BY LastUpdated DESC";

        using var command = new SqlCommand(sql,connection);
        command.Parameters.AddWithValue("@UserCall",userCall.ToUpper());

        using var reader = await command.ExecuteReaderAsync();

        while(await reader.ReadAsync()) {
            var filter = new DxSpotFilter {
                UserCall = reader.GetString(0),
                Callsign = reader.GetString(1),
                Mode = reader.GetString(2),
                IsEnabled = reader.GetBoolean(3),
                NotifyPeriodMinutes = reader.GetInt32(4),
                AnyBand = reader.GetBoolean(5),
                Band160m = reader.GetBoolean(6),
                Band80m = reader.GetBoolean(7),
                Band40m = reader.GetBoolean(8),
                Band30m = reader.GetBoolean(9),
                Band20m = reader.GetBoolean(10),
                Band17m = reader.GetBoolean(11),
                Band15m = reader.GetBoolean(12),
                Band12m = reader.GetBoolean(13),
                Band10m = reader.GetBoolean(14),
                Band6m = reader.GetBoolean(15),
                CqZones = reader.GetString(16)
            };

            filter.SetOriginalCallsign(filter.Callsign);
            filter.SetOriginalMode(filter.Mode);
            filters.Add(filter);
        }

        return filters;
    }

    /// <summary>
    /// Save or update a filter setting for a user
    /// </summary>
    /// <summary>
    /// Save or update a filter setting for a user
    /// ✅ Detects duplicate PK violations
    /// </summary>
    /// <summary>
    /// Save or update a filter setting for a user
    /// ✅ FIXED: Added logging and proper error handling
    /// </summary>
    public async Task SaveSettingAsync(DxSpotFilter filter) {
        // ✅ Log what we're trying to save
        System.Diagnostics.Debug.WriteLine($"SaveSettingAsync called: User={filter.UserCall}, Call={filter.Callsign}, Mode={filter.Mode}, Enabled={filter.IsEnabled}");

        if(string.IsNullOrWhiteSpace(filter.UserCall)) {
            System.Diagnostics.Debug.WriteLine("  ⚠ SKIPPED: UserCall is empty");
            return;
        }

        if(string.IsNullOrWhiteSpace(filter.Callsign)) {
            System.Diagnostics.Debug.WriteLine("  ⚠ SKIPPED: Callsign is empty");
            return;
        }

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
    MERGE dx.DXSpotAlertSettings AS target
    USING (SELECT @UserCall AS UserCall, @DXCall AS DXCall, @Mode AS Mode) AS source
    ON target.UserCall = source.UserCall 
       AND target.DXCall = source.DXCall 
       AND target.Mode = source.Mode
    WHEN MATCHED THEN
        UPDATE SET
            IsEnabled = @IsEnabled,
            NotifyPeriodMinutes = @NotifyPeriodMinutes,
            AnyBand = @AnyBand,
            Band160m = @Band160m,
            Band80m = @Band80m,
            Band40m = @Band40m,
            Band30m = @Band30m,
            Band20m = @Band20m,
            Band17m = @Band17m,
            Band15m = @Band15m,
            Band12m = @Band12m,
            Band10m = @Band10m,
            Band6m = @Band6m,
            CQZones = @CQZones,
            LastUpdated = GETDATE(),
            LastUpdatedBy = @UserCall
    WHEN NOT MATCHED THEN
        INSERT (UserCall, DXCall, Mode, IsEnabled, NotifyPeriodMinutes, AnyBand,
                Band160m, Band80m, Band40m, Band30m, Band20m,
                Band17m, Band15m, Band12m, Band10m, Band6m,
                CQZones, LastUpdated, LastUpdatedBy)
        VALUES (@UserCall, @DXCall, @Mode, @IsEnabled, @NotifyPeriodMinutes, @AnyBand,
                @Band160m, @Band80m, @Band40m, @Band30m, @Band20m,
                @Band17m, @Band15m, @Band12m, @Band10m, @Band6m,
                @CQZones, GETDATE(), @UserCall);";

        using var command = new SqlCommand(sql,connection);
        command.Parameters.AddWithValue("@UserCall",filter.UserCall.ToUpper());
        command.Parameters.AddWithValue("@DXCall",filter.Callsign.ToUpper());
        command.Parameters.AddWithValue("@Mode",filter.Mode);
        command.Parameters.AddWithValue("@IsEnabled",filter.IsEnabled);
        command.Parameters.AddWithValue("@NotifyPeriodMinutes",filter.NotifyPeriodMinutes);
        command.Parameters.AddWithValue("@AnyBand",filter.AnyBand);
        command.Parameters.AddWithValue("@Band160m",filter.Band160m);
        command.Parameters.AddWithValue("@Band80m",filter.Band80m);
        command.Parameters.AddWithValue("@Band40m",filter.Band40m);
        command.Parameters.AddWithValue("@Band30m",filter.Band30m);
        command.Parameters.AddWithValue("@Band20m",filter.Band20m);
        command.Parameters.AddWithValue("@Band17m",filter.Band17m);
        command.Parameters.AddWithValue("@Band15m",filter.Band15m);
        command.Parameters.AddWithValue("@Band12m",filter.Band12m);
        command.Parameters.AddWithValue("@Band10m",filter.Band10m);
        command.Parameters.AddWithValue("@Band6m",filter.Band6m);
        command.Parameters.AddWithValue("@CQZones",filter.CqZones ?? "");

        try {
            int rowsAffected = await command.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine($"  ✓ SAVED: {filter.UserCall}/{filter.Callsign}/{filter.Mode} (Enabled={filter.IsEnabled}) - {rowsAffected} rows affected");
        } catch(SqlException ex) when(ex.Number == 2627 || ex.Number == 2601) {
            throw new InvalidOperationException(
                $"Duplicate filter already exists:\n\nUser: {filter.UserCall}\nDX Call: {filter.Callsign}\nMode: {filter.Mode}");
        } catch(SqlException ex) {
            System.Diagnostics.Debug.WriteLine($"  ❌ SQL ERROR: {ex.Message}");
            throw new InvalidOperationException($"Database error: {ex.Message}");
        }
    }
    /// <summary>
    /// Delete a filter for a user
    /// </summary>
    public async Task DeleteSettingAsync(string userCall,string callsign,string mode) {
        if(string.IsNullOrWhiteSpace(userCall) || string.IsNullOrWhiteSpace(callsign))
            return;

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            DELETE FROM dx.DXSpotAlertSettings 
            WHERE UserCall = @UserCall 
              AND DXCall = @Call 
              AND Mode = @Mode";

        using var command = new SqlCommand(sql,connection);
        command.Parameters.AddWithValue("@UserCall",userCall.ToUpper());
        command.Parameters.AddWithValue("@Call",callsign.ToUpper());
        command.Parameters.AddWithValue("@Mode",mode);

        await command.ExecuteNonQueryAsync();
        System.Diagnostics.Debug.WriteLine($"✗ Deleted filter: {userCall}/{callsign}/{mode}");
    }

    /// <summary>
    /// Update callsign or mode for a filter
    /// </summary>
    /// <summary>
    /// Update callsign or mode for a filter (PK change)
    /// ✅ Handles duplicate key errors
    /// </summary>
    public async Task UpdateFilterKeyAsync(string userCall,string oldCallsign,string oldMode,
        string newCallsign,string newMode,DxSpotFilter filter) {
        if(oldCallsign == newCallsign && oldMode == newMode)
            return;

        try {
            // ✅ Check if new combination already exists
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var checkSql = @"
            SELECT COUNT(*)
            FROM dx.DXSpotAlertSettings
            WHERE UserCall = @UserCall
              AND DXCall = @NewCallsign
              AND Mode = @NewMode";

            using var checkCmd = new SqlCommand(checkSql,connection);
            checkCmd.Parameters.AddWithValue("@UserCall",userCall.ToUpper());
            checkCmd.Parameters.AddWithValue("@NewCallsign",newCallsign.ToUpper());
            checkCmd.Parameters.AddWithValue("@NewMode",newMode);

            var count = (int)await checkCmd.ExecuteScalarAsync();

            if(count > 0) {
                // ✅ Duplicate found - don't allow change
                throw new InvalidOperationException(
                    $"Cannot change to {newCallsign}/{newMode}:\n\nThis filter already exists for {userCall}.\n\nPlease choose a different DX Call or Mode."
                );
            }

            // No duplicate - proceed with update
            // Delete old
            await DeleteSettingAsync(userCall,oldCallsign,oldMode);

            // Insert new
            await SaveSettingAsync(filter);

            System.Diagnostics.Debug.WriteLine($"⟳ Updated filter key: {userCall}/{oldCallsign}/{oldMode} → {newCallsign}/{newMode}");
        } catch(InvalidOperationException) {
            // Re-throw our custom exceptions
            throw;
        } catch(Exception ex) {
            throw new InvalidOperationException($"Error updating filter: {ex.Message}");
        }
    }
    #endregion
}