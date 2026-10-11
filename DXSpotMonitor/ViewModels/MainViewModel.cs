using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DXSpotMonitor.Models;
using DXSpotMonitor.Services;


namespace DXSpotMonitor.ViewModels;

/// <summary>
/// ✅ Multi-User Monitor Service - FIXED VERSION
/// </summary>
public partial class MainViewModel:ObservableObject, IDisposable {
    private readonly DxSpotDatabaseService _dbService;
    private readonly MqttPublisherService _mqttService;
    private readonly MonitorLogService _logService;
    private readonly DispatcherTimer _monitorTimer;

    // ✅ Master list of ALL filters from DB (for UI display)
    private List<DxSpotFilter> _allFiltersFromDb = new();

    // ✅ Track last check time PER FILTER
    private readonly Dictionary<string,DateTime> _filterLastCheckTimes = new();

    // ✅ Only ENABLED filters for monitoring
    private readonly Dictionary<string,List<DxSpotFilter>> _userFilters = new();

    // ✅ Prevent concurrent reloads
    private bool _isReloading;
    private readonly object _reloadLock = new();

    // ✅ Track if we're disposed
    private bool _isDisposed;

    [ObservableProperty]
    private ObservableCollection<DxSpotFilter> _allFilters = new();

    [ObservableProperty]
    private int _timerIntervalSeconds = 30;

    [ObservableProperty]
    private bool _isMonitoring;

    [ObservableProperty]
    private bool _isMqttConnected;

    [ObservableProperty]
    private string _statusMessage = "Starting up...";

    [ObservableProperty]
    private int _spotsFoundToday;

    [ObservableProperty]
    private int _spotsSentToday;

    [ObservableProperty]
    private int _activeUsers;

    [ObservableProperty]
    private int _totalFilters;

    [ObservableProperty]
    private ObservableCollection<DxSpotAlertUser> _allUsers = new();

    [ObservableProperty]
    private DxSpotAlertUser? _selectedUser;

    [ObservableProperty]
    private ObservableCollection<string> _availableUsers = new();

    [ObservableProperty]
    private string? _selectedFilterUser;

    [ObservableProperty]
    private bool _showAllUsersFilters = true;

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    private readonly HashSet<DxSpotAlertUser> _modifiedUsers = new();
    private readonly HashSet<DxSpotAlertUser> _newUsers = new();

    [ObservableProperty]
    private DxSpotFilter? _selectedFilter;

    [ObservableProperty]
    private string _lastDecisionDisplay = "No checks performed yet";

    [ObservableProperty]
    private string _lastDecisionTimestamp = "";

    public ObservableCollection<LogEntry> ScreenLogEntries => _logService.ScreenLogEntries;

    [ObservableProperty]
    private bool _isFileLoggingEnabled = true;

    [ObservableProperty]
    private string _logFilePath = "";

    [ObservableProperty]
    private string _activityLogHeader = "📜 Sent Spots (Newest First)";

    public MainViewModel() {
        _logService = new MonitorLogService();
        _logService.Log("MainViewModel initializing...");
        _logService.LastDecisionChanged += OnLastDecisionChanged;
        _logService.ActivityHeaderChanged += OnActivityHeaderChanged;
        LogFilePath = _logService.LogFilePath;

        string connString = "Server=localhost;Database=QSOLOG;Integrated Security=True;TrustServerCertificate=True";
        _dbService = new DxSpotDatabaseService(connString);
        _mqttService = new MqttPublisherService();

        _mqttService.StatusChanged += (s,msg) => {
            if(_isDisposed) return;
            StatusMessage = msg;
            _logService.LogMqttStatus(_mqttService.IsConnected,msg);
        };
        _mqttService.ConnectionChanged += (s,connected) => {
            if(_isDisposed) return;
            IsMqttConnected = connected;
        };

        _monitorTimer = new DispatcherTimer();
        _monitorTimer.Tick += OnMonitorTimerTick;

        _logService.Log("MainViewModel initialized successfully");

        // ✅ Auto-startup - but track it so we can cancel if disposed
        _ = AutoStartupSequenceAsync();
    }

    private async Task AutoStartupSequenceAsync() {
        try {
            if(_isDisposed) return;

            _logService.LogStartupAutoConnect();

            await LoadAllUsersAndFiltersAsync();
            if(_isDisposed) return;

            StatusMessage = "Auto-connecting to MQTT broker...";
            await _mqttService.ConnectAsync();
            if(_isDisposed) return;

            await Task.Delay(2000);
            if(_isDisposed) return;

            if(!IsMqttConnected) {
                _logService.LogError("AutoStartup","Failed to connect to MQTT broker");
                StatusMessage = "❌ Auto-connect failed - please connect manually";
                return;
            }

            _logService.LogStartupAutoMonitor();
            StartMonitoringInternal();

            StatusMessage = $"✓ Auto-started: Monitoring {ActiveUsers} users with {TotalFilters} filters";
        } catch(Exception ex) {
            if(!_isDisposed) {
                _logService.LogError("AutoStartupSequence",ex);
                StatusMessage = $"❌ Startup error: {ex.Message}";
            }
        }
    }

    private void OnLastDecisionChanged(object? sender,string decision) {
        if(_isDisposed) return;
        System.Windows.Application.Current?.Dispatcher.Invoke(() => {
            LastDecisionTimestamp = $"[{_logService.LastDecisionTime:HH:mm:ss}]";
            LastDecisionDisplay = decision;
        });
    }

    private void OnActivityHeaderChanged(object? sender,string headerText) {
        if(_isDisposed) return;
        System.Windows.Application.Current?.Dispatcher.Invoke(() => {
            ActivityLogHeader = headerText;
        });
    }

    #region User & Filter Management


    private void SubscribeToFilterChanges(DxSpotFilter filter) {
        var originalCallsign = filter.Callsign;
        var originalMode = filter.Mode;

        filter.PropertyChanged += async (s,e) =>
        {
            if(_isDisposed) return;
            if(s is not DxSpotFilter modifiedFilter) return;

            // ✅ Log every property change for debugging
            _logService.Log($"📝 Filter property changed: {modifiedFilter.UserCall}/{modifiedFilter.Callsign} - {e.PropertyName}");

            try {
                bool needsDbSave = false;
                bool needsMonitoringReload = false;

                // ✅ Handle Callsign change (PK column)
                if(e.PropertyName == nameof(DxSpotFilter.Callsign)) {
                    if(!string.IsNullOrEmpty(originalCallsign) && originalCallsign != modifiedFilter.Callsign) {
                        _logService.LogFilterModified(modifiedFilter.UserCall,modifiedFilter.Callsign,
                            modifiedFilter.Mode,"Callsign",originalCallsign,modifiedFilter.Callsign);

                        await _dbService.UpdateFilterKeyAsync(
                            modifiedFilter.UserCall,
                            originalCallsign,
                            modifiedFilter.Mode,
                            modifiedFilter.Callsign,
                            modifiedFilter.Mode,
                            modifiedFilter
                        );

                        originalCallsign = modifiedFilter.Callsign;
                        needsMonitoringReload = true;
                    } else if(string.IsNullOrEmpty(originalCallsign) && !string.IsNullOrEmpty(modifiedFilter.Callsign)) {
                        // New filter getting its first callsign
                        _logService.Log($"  → New filter, saving to DB: {modifiedFilter.Callsign}");
                        await _dbService.SaveSettingAsync(modifiedFilter);
                        originalCallsign = modifiedFilter.Callsign;
                        needsMonitoringReload = true;
                    }
                }
                // ✅ Handle Mode change (PK column)
                else if(e.PropertyName == nameof(DxSpotFilter.Mode)) {
                    if(!string.IsNullOrEmpty(originalMode) && originalMode != modifiedFilter.Mode) {
                        _logService.LogFilterModified(modifiedFilter.UserCall,modifiedFilter.Callsign,
                            modifiedFilter.Mode,"Mode",originalMode,modifiedFilter.Mode);

                        await _dbService.UpdateFilterKeyAsync(
                            modifiedFilter.UserCall,
                            modifiedFilter.Callsign,
                            originalMode,
                            modifiedFilter.Callsign,
                            modifiedFilter.Mode,
                            modifiedFilter
                        );

                        originalMode = modifiedFilter.Mode;
                        needsMonitoringReload = true;
                    }
                }
                // ✅ Handle IsEnabled change - IMPORTANT for monitoring
                else if(e.PropertyName == nameof(DxSpotFilter.IsEnabled)) {
                    _logService.Log($"  → IsEnabled changed to: {modifiedFilter.IsEnabled}");

                    // Only save if filter has a callsign
                    if(!string.IsNullOrWhiteSpace(modifiedFilter.Callsign)) {
                        needsDbSave = true;
                        needsMonitoringReload = true; // Must reload to update _userFilters
                    } else {
                        _logService.Log($"  ⚠ Skipping save - filter has no callsign");
                    }
                }
                // ✅ Handle all other property changes
                else {
                    _logService.Log($"  → Other property changed: {e.PropertyName}");

                    // Only save if filter has a callsign
                    if(!string.IsNullOrWhiteSpace(modifiedFilter.Callsign)) {
                        needsDbSave = true;
                        // Only reload monitoring if it affects matching (bands, zones, etc.)
                        if(e.PropertyName?.StartsWith("Band") == true ||
                            e.PropertyName == nameof(DxSpotFilter.AnyBand) ||
                            e.PropertyName == nameof(DxSpotFilter.CqZones) ||
                            e.PropertyName == nameof(DxSpotFilter.NotifyPeriodMinutes)) {
                            needsMonitoringReload = true;
                        }
                    } else {
                        _logService.Log($"  ⚠ Skipping save - filter has no callsign");
                    }
                }

                // ✅ Save to database
                if(needsDbSave && !string.IsNullOrWhiteSpace(modifiedFilter.Callsign)) {
                    _logService.Log($"  → Saving to database...");
                    await _dbService.SaveSettingAsync(modifiedFilter);
                    _logService.Log($"  ✓ Saved to database: {modifiedFilter.UserCall}/{modifiedFilter.Callsign}/{modifiedFilter.Mode}");
                }

                // ✅ Reload monitoring lists (not full UI reload)
                if(needsMonitoringReload && !_isDisposed) {
                    _logService.Log($"  → Reloading monitoring filters...");
                    await ReloadMonitoringFiltersAsync();
                }
            } catch(Exception ex) {
                if(_isDisposed) return;
                StatusMessage = $"❌ Error saving filter: {ex.Message}";
                _logService.LogError("SubscribeToFilterChanges",ex);
            }
        };
    }

    /// <summary>
    /// ✅ NEW: Lightweight reload of ONLY monitoring filters (not UI)
    /// This updates _userFilters without touching AllFilters/UI
    /// </summary>
    private async Task ReloadMonitoringFiltersAsync() {
        lock(_reloadLock) {
            if(_isReloading) {
                _logService.Log("  ⚠ Reload already in progress, skipping...");
                return;
            }
            _isReloading = true;
        }

        try {
            _logService.Log("  Reloading monitoring filters...");

            // Clear monitoring collections
            _userFilters.Clear();

            // Rebuild from _allFiltersFromDb (which is already up-to-date from the binding)
            foreach(var filter in _allFiltersFromDb) {
                // Only add ENABLED filters with callsigns to monitoring
                if(filter.IsEnabled && !string.IsNullOrWhiteSpace(filter.Callsign)) {
                    if(!_userFilters.ContainsKey(filter.UserCall)) {
                        _userFilters[filter.UserCall] = new List<DxSpotFilter>();
                    }
                    _userFilters[filter.UserCall].Add(filter);

                    // Ensure filter has a tracking time
                    var filterKey = GetFilterKey(filter);
                    if(!_filterLastCheckTimes.ContainsKey(filterKey)) {
                        _filterLastCheckTimes[filterKey] = DateTime.UtcNow.AddMinutes(-filter.NotifyPeriodMinutes);
                    }
                }
            }

            // Remove tracking for disabled filters
            var enabledKeys = _userFilters.Values
                .SelectMany(filters => filters.Select(GetFilterKey))
                .ToHashSet();

            var keysToRemove = _filterLastCheckTimes.Keys
                .Where(k => !enabledKeys.Contains(k))
                .ToList();

            foreach(var key in keysToRemove) {
                _filterLastCheckTimes.Remove(key);
            }

            // Update counts
            ActiveUsers = _userFilters.Count;
            TotalFilters = _userFilters.Values.Sum(f => f.Count);

            _logService.Log($"  ✓ Monitoring filters updated: {ActiveUsers} users, {TotalFilters} enabled filters");
        } catch(Exception ex) {
            _logService.LogError("ReloadMonitoringFiltersAsync",ex);
        } finally {
            lock(_reloadLock) {
                _isReloading = false;
            }
        }
    }




    private void SubscribeToUserChanges(DxSpotAlertUser user) {
        user.PropertyChanged += (s,e) => {
            if(_isDisposed) return;
            if(e.PropertyName == nameof(DxSpotAlertUser.FilterCount)) return;

            if(s is DxSpotAlertUser modifiedUser) {
                if(!_newUsers.Contains(modifiedUser)) {
                    _modifiedUsers.Add(modifiedUser);
                }
                HasUnsavedChanges = true;
                StatusMessage = "⚠ Unsaved changes";
                _logService.Log($"⚠ User modified: {modifiedUser.UserCall}");
            }
        };
    }

    private async Task LoadAllUsersAndFiltersAsync() {
        try {
            StatusMessage = "Loading all users and filters...";
            _logService.LogSection("LOADING USERS AND FILTERS");

            var users = await _dbService.GetAllUsersAsync();

            if(!users.Any()) {
                StatusMessage = "No users found in database";
                _logService.Log("No users found in database");
                return;
            }

            _userFilters.Clear();
            AllFilters.Clear();
            AllUsers.Clear();
            AvailableUsers.Clear();
            _filterLastCheckTimes.Clear();
            _newUsers.Clear();
            _modifiedUsers.Clear();
            _allFiltersFromDb.Clear();
            HasUnsavedChanges = false;

            int totalEnabledFilters = 0;

            foreach(var user in users) {
                AllUsers.Add(user);
                AvailableUsers.Add(user.UserCall);

                SubscribeToUserChanges(user);

                var filters = await _dbService.LoadSettingsAsync(user.UserCall);

                if(filters.Any()) {
                    // ✅ Store ALL filters for UI display
                    foreach(var filter in filters) {
                        SubscribeToFilterChanges(filter);
                        _allFiltersFromDb.Add(filter);
                    }

                    // ✅ Store ONLY ENABLED filters for monitoring
                    var enabledFilters = filters
                        .Where(f => f.IsEnabled && !string.IsNullOrWhiteSpace(f.Callsign))
                        .ToList();

                    if(enabledFilters.Any()) {
                        _userFilters[user.UserCall] = enabledFilters;
                        totalEnabledFilters += enabledFilters.Count;

                        foreach(var filter in enabledFilters) {
                            var filterKey = GetFilterKey(filter);
                            _filterLastCheckTimes[filterKey] = DateTime.UtcNow.AddMinutes(-filter.NotifyPeriodMinutes);
                            _logService.Log($"  ✓ Enabled: {filter.UserCall}/{filter.Callsign}/{filter.Mode}");
                        }
                    }

                    var disabledCount = filters.Count - enabledFilters.Count;
                    if(disabledCount > 0) {
                        _logService.Log($"  ○ {disabledCount} disabled/empty for {user.UserCall}");
                    }
                }

                user.FilterCount = filters.Count;
            }

            if(AvailableUsers.Any() && string.IsNullOrEmpty(SelectedFilterUser)) {
                SelectedFilterUser = AvailableUsers.First();
            }

            ActiveUsers = _userFilters.Count;
            TotalFilters = totalEnabledFilters;

            RefreshFilterDisplay();

            StatusMessage = $"Loaded {AllUsers.Count} user(s) with {totalEnabledFilters} enabled ({_allFiltersFromDb.Count} total)";
            _logService.Log($"✓ Load complete: {totalEnabledFilters} enabled, {_allFiltersFromDb.Count} total");
        } catch(Exception ex) {
            StatusMessage = $"Error loading: {ex.Message}";
            _logService.LogError("LoadAllUsersAndFiltersAsync",ex);
        }
    }

    private void RefreshFilterDisplay() {
        AllFilters.Clear();

        IEnumerable<DxSpotFilter> filtersToShow;

        if(ShowAllUsersFilters) {
            filtersToShow = _allFiltersFromDb;
        } else if(!string.IsNullOrEmpty(SelectedFilterUser)) {
            filtersToShow = _allFiltersFromDb.Where(f => f.UserCall == SelectedFilterUser);
        } else {
            filtersToShow = Enumerable.Empty<DxSpotFilter>();
        }

        var sorted = filtersToShow
            .OrderBy(f => f.UserCall)
            .ThenBy(f => f.Callsign)
            .ThenBy(f => f.Mode)
            .ToList();

        foreach(var filter in sorted) {
            AllFilters.Add(filter);
        }

        int enabledCount = _userFilters.Values.Sum(f => f.Count);
        StatusMessage = ShowAllUsersFilters
            ? $"Showing {AllFilters.Count} filters ({enabledCount} enabled)"
            : $"Showing {AllFilters.Count} filters for {SelectedFilterUser}";
    }

    #endregion

    #region User Management Commands

    [RelayCommand]
    private async Task AddUserAsync() {
        try {
            var newUser = new DxSpotAlertUser {
                UserCall = "",
                PIN = 1234,
                UserName = "",
                Comment = "New user",
                FilterCount = 0
            };

            _newUsers.Add(newUser);
            SubscribeToUserChanges(newUser);
            AllUsers.Add(newUser);
            SelectedUser = newUser;

            HasUnsavedChanges = true;
            StatusMessage = "Enter callsign and click Save Changes";
            _logService.Log("New user added (pending save)");
        } catch(Exception ex) {
            StatusMessage = $"Error adding user: {ex.Message}";
            _logService.LogError("AddUserAsync",ex);
        }
    }

    [RelayCommand]
    private async Task SaveChangesAsync() {
        try {
            StatusMessage = "Saving changes...";
            _logService.Log("Saving user changes...");
            int savedCount = 0;

            foreach(var newUser in _newUsers.ToList()) {
                if(string.IsNullOrWhiteSpace(newUser.UserCall)) {
                    System.Windows.MessageBox.Show("Callsign cannot be empty!","Validation Error",
                        System.Windows.MessageBoxButton.OK,System.Windows.MessageBoxImage.Warning);
                    return;
                }

                if(AllUsers.Count(u => u.UserCall == newUser.UserCall.ToUpper()) > 1) {
                    System.Windows.MessageBox.Show($"User {newUser.UserCall} already exists!","Duplicate User",
                        System.Windows.MessageBoxButton.OK,System.Windows.MessageBoxImage.Warning);
                    return;
                }

                newUser.UserCall = newUser.UserCall.ToUpper();
                await _dbService.AddUserAsync(newUser);
                savedCount++;
                _logService.Log($"  Saved new user: {newUser.UserCall}");
            }

            foreach(var modifiedUser in _modifiedUsers.ToList()) {
                await _dbService.UpdateUserAsync(modifiedUser);
                savedCount++;
                _logService.Log($"  Updated user: {modifiedUser.UserCall}");
            }

            _newUsers.Clear();
            _modifiedUsers.Clear();
            HasUnsavedChanges = false;

            StatusMessage = $"✓ Saved {savedCount} user(s)";
            _logService.Log($"✓ Save complete: {savedCount} user(s)");

            await LoadAllUsersAndFiltersAsync();
        } catch(Exception ex) {
            StatusMessage = $"Error saving: {ex.Message}";
            _logService.LogError("SaveChangesAsync",ex);
        }
    }

    [RelayCommand]
    private async Task CancelChangesAsync() {
        if(HasUnsavedChanges) {
            var result = System.Windows.MessageBox.Show("Discard unsaved changes?","Confirm Cancel",
                System.Windows.MessageBoxButton.YesNo,System.Windows.MessageBoxImage.Question);

            if(result == System.Windows.MessageBoxResult.Yes) {
                await LoadAllUsersAndFiltersAsync();
                _newUsers.Clear();
                _modifiedUsers.Clear();
                HasUnsavedChanges = false;
                StatusMessage = "Changes discarded";
                _logService.Log("Changes discarded by user");
            }
        }
    }

    public bool IsNewUser(DxSpotAlertUser user) => _newUsers.Contains(user);

    [RelayCommand]
    private async Task RemoveUserAsync(DxSpotAlertUser? user) {
        if(user == null) {
            StatusMessage = "Select a user to remove";
            return;
        }

        try {
            var filterCount = _userFilters.ContainsKey(user.UserCall)
                ? _userFilters[user.UserCall].Count
                : 0;

            var message = filterCount > 0
                ? $"Delete user {user.UserCall} and {filterCount} filter(s)?\n\nThis cannot be undone."
                : $"Delete user {user.UserCall}?\n\nThis cannot be undone.";

            var result = System.Windows.MessageBox.Show(message,"Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,System.Windows.MessageBoxImage.Warning);

            if(result == System.Windows.MessageBoxResult.Yes) {
                StatusMessage = $"Deleting user {user.UserCall}...";
                _logService.Log($"Deleting user: {user.UserCall}");

                await _dbService.DeleteUserAsync(user.UserCall);
                await LoadAllUsersAndFiltersAsync();

                StatusMessage = $"Deleted user {user.UserCall}";
                _logService.Log($"✓ Deleted user: {user.UserCall}");
            }
        } catch(Exception ex) {
            StatusMessage = $"Error deleting user: {ex.Message}";
            _logService.LogError("RemoveUserAsync",ex);
        }
    }

    [RelayCommand]
    private async Task ReloadFiltersAsync() {
        _logService.Log("Manual reload triggered");
        await LoadAllUsersAndFiltersAsync();
    }

    private string GetFilterKey(DxSpotFilter filter) {
        return $"{filter.UserCall}|{filter.Callsign}|{filter.Mode}";
    }

    [RelayCommand]
    private void AddFilter() {
        if(ShowAllUsersFilters) {
            System.Windows.MessageBox.Show(
                "Please select a specific user first.\n\nUncheck 'Show All Users' and select a user.",
                "Select User",System.Windows.MessageBoxButton.OK,System.Windows.MessageBoxImage.Information);
            return;
        }

        if(string.IsNullOrEmpty(SelectedFilterUser)) {
            System.Windows.MessageBox.Show("Please select a user from the dropdown first.",
                "No User Selected",System.Windows.MessageBoxButton.OK,System.Windows.MessageBoxImage.Warning);
            return;
        }

        try {
            var newFilter = new DxSpotFilter {
                UserCall = SelectedFilterUser,
                Callsign = "",
                Mode = "CW",
                IsEnabled = true,
                AnyBand = true,
                CqZones = "4,5",
                NotifyPeriodMinutes = 30
            };

            SubscribeToFilterChanges(newFilter);

            // ✅ Add to BOTH collections
            _allFiltersFromDb.Add(newFilter);

            if(!_userFilters.ContainsKey(SelectedFilterUser)) {
                _userFilters[SelectedFilterUser] = new List<DxSpotFilter>();
            }
            _userFilters[SelectedFilterUser].Add(newFilter);

            // Refresh UI
            RefreshFilterDisplay();

            SelectedFilter = newFilter;

            StatusMessage = $"Added new filter for {SelectedFilterUser} - enter DX Call";
            _logService.LogFilterAdded(SelectedFilterUser,"(new)","CW");
        } catch(Exception ex) {
            StatusMessage = $"Error adding filter: {ex.Message}";
            _logService.LogError("AddFilter",ex);
        }
    }

    [RelayCommand]
    private async Task DeleteFilterAsync(DxSpotFilter? filter) {
        if(filter == null) {
            StatusMessage = "Select a filter to delete";
            return;
        }

        try {
            var result = System.Windows.MessageBox.Show(
                $"Delete filter for {filter.UserCall}/{filter.Callsign}/{filter.Mode}?",
                "Confirm Delete",System.Windows.MessageBoxButton.YesNo,System.Windows.MessageBoxImage.Question);

            if(result == System.Windows.MessageBoxResult.Yes) {
                StatusMessage = $"Deleting filter {filter.Callsign}...";
                _logService.LogFilterDeleted(filter.UserCall,filter.Callsign,filter.Mode);

                await _dbService.DeleteSettingAsync(filter.UserCall,filter.Callsign,filter.Mode);

                // Remove from ALL collections
                _allFiltersFromDb.Remove(filter);

                if(_userFilters.ContainsKey(filter.UserCall)) {
                    _userFilters[filter.UserCall].Remove(filter);
                }

                var filterKey = GetFilterKey(filter);
                _filterLastCheckTimes.Remove(filterKey);

                var user = AllUsers.FirstOrDefault(u => u.UserCall == filter.UserCall);
                if(user != null) {
                    user.FilterCount = _allFiltersFromDb.Count(f => f.UserCall == filter.UserCall);
                }

                RefreshFilterDisplay();
                StatusMessage = $"Deleted filter {filter.Callsign}";
            }
        } catch(Exception ex) {
            StatusMessage = $"Error deleting filter: {ex.Message}";
            _logService.LogError("DeleteFilterAsync",ex);
        }
    }

    #endregion

    #region MQTT Commands

    [RelayCommand]
    private async Task ConnectMqttAsync() {
        _logService.Log("Manual MQTT connection requested");
        await _mqttService.ConnectAsync();
    }

    [RelayCommand]
    private async Task DisconnectMqttAsync() {
        _logService.Log("Disconnecting from MQTT...");
        await _mqttService.DisconnectAsync();
    }

    #endregion

    #region Monitoring Commands

    [RelayCommand]
    private void StartMonitoring() => StartMonitoringInternal();

    private void StartMonitoringInternal() {
        if(!IsMqttConnected) {
            StatusMessage = "Please connect to MQTT first";
            _logService.Log("⚠ Cannot start - MQTT not connected");
            return;
        }

        if(!_userFilters.Any()) {
            StatusMessage = "No users with enabled filters";
            _logService.Log("⚠ Cannot start - No filters configured");
            return;
        }

        IsMonitoring = true;
        _monitorTimer.Interval = TimeSpan.FromSeconds(TimerIntervalSeconds);
        _monitorTimer.Start();

        StatusMessage = $"Monitoring {ActiveUsers} user(s) with {TotalFilters} filter(s)";
        _logService.LogMonitoringStarted(TimerIntervalSeconds,ActiveUsers,TotalFilters);
    }

    [RelayCommand]
    private void StopMonitoring() {
        IsMonitoring = false;
        _monitorTimer.Stop();
        StatusMessage = "Monitoring stopped";
        _logService.LogMonitoringStopped();
    }

    [RelayCommand]
    private async Task CheckNowAsync() {
        _logService.LogCheckNowTriggered();
        await CheckForNewSpotsAsync();
    }

    [RelayCommand]
    private void StopFileLogging() {
        _logService.StopFileLogging();
        IsFileLoggingEnabled = false;
        StatusMessage = "File logging stopped";
    }

    [RelayCommand]
    private void ClearScreenLog() {
        ScreenLogEntries.Clear();
        _logService.Log("Screen log cleared by user");
    }

    #endregion

    #region Spot Checking Logic

    private async void OnMonitorTimerTick(object? sender,EventArgs e) {
        if(_isDisposed) return;
        _logService.LogTimerTick(TimerIntervalSeconds,ActiveUsers,TotalFilters);
        await CheckForNewSpotsAsync();
    }

    private async Task CheckForNewSpotsAsync() {
        if(_isDisposed) return;

        try {
            StatusMessage = "Checking spots...";

            int enabledFilterCount = _userFilters.Values.Sum(f => f.Count);
            _logService.LogSection($"SPOT CHECK - {ActiveUsers} user(s), {enabledFilterCount} enabled");

            int totalSpotsFound = 0;
            int totalSpotsSent = 0;
            int filtersEvaluated = 0;

            foreach(var (userCall,filters) in _userFilters) {
                if(_isDisposed) return;

                var enabledFilters = filters
                    .Where(f => f.IsEnabled && !string.IsNullOrWhiteSpace(f.Callsign))
                    .ToList();

                if(!enabledFilters.Any()) continue;

                _logService.LogUserProcessingStart(userCall,enabledFilters.Count);

                var processedInThisBatch = new HashSet<string>();

                foreach(var filter in enabledFilters) {
                    if(_isDisposed) return;

                    filtersEvaluated++;

                    _logService.LogFilterDetails(filter.UserCall,filter.Callsign,filter.Mode,
                        filter.NotifyPeriodMinutes,filter.AnyBand,filter.GetBandsForSQL(),
                        filter.CqZones,filter.IsEnabled);

                    var filterKey = GetFilterKey(filter);
                    if(!_filterLastCheckTimes.ContainsKey(filterKey)) {
                        _filterLastCheckTimes[filterKey] = DateTime.UtcNow.AddMinutes(-filter.NotifyPeriodMinutes);
                    }

                    var filterLastCheck = _filterLastCheckTimes[filterKey];
                    var oldestAllowedTime = DateTime.UtcNow.AddMinutes(-filter.NotifyPeriodMinutes);
                    var sinceTime = filterLastCheck > oldestAllowedTime ? filterLastCheck : oldestAllowedTime;

                    _logService.LogQueryParameters(sinceTime,filter.NotifyPeriodMinutes,filterLastCheck);

                    var spots = await _dbService.GetMatchingSpotsAsync(userCall,
                        new List<DxSpotFilter> { filter },sinceTime);

                    var recentSpots = spots
                        .Where(s => s.UTC >= DateTime.UtcNow.AddMinutes(-filter.NotifyPeriodMinutes))
                        .ToList();

                    _logService.LogSpotsFound(recentSpots.Count,filter.Callsign);
                    totalSpotsFound += recentSpots.Count;

                    foreach(var spot in recentSpots) {
                        if(_isDisposed) return;

                        int bandNum = int.Parse(spot.Band.Replace("m",""));
                        var spotKey = $"{spot.Call}|{bandNum}|{spot.Mode}";

                        _logService.LogSpotEvaluation(userCall,spot.Call,spot.Band,
                            spot.Mode,spot.UTC,spot.Spotter);

                        if(processedInThisBatch.Contains(spotKey)) {
                            _logService.LogDuplicateInBatch(spot.Call,spotKey);
                            continue;
                        }

                        bool alreadySent = await _dbService.WasRecentlySentAsync(
                            userCall,spot.Call,bandNum,spot.Mode,filter.NotifyPeriodMinutes);

                        if(alreadySent) {
                            _logService.LogAlreadySentRecently(userCall,spot.Call,spot.Band,
                                spot.Mode,filter.NotifyPeriodMinutes);
                            continue;
                        }

                        processedInThisBatch.Add(spotKey);

                        await _mqttService.PublishSpotToUserAsync(userCall,spot);
                        await _dbService.RecordSentSpotAsync(userCall,spot);

                        totalSpotsSent++;
                        _logService.LogSpotSent(userCall,spot.Call,spot.Band,spot.Mode,
                            spot.Frequency,spot.Spotter, spot.RptByType,spot.UTC, spot.Comment);
                    }

                    _filterLastCheckTimes[filterKey] = DateTime.UtcNow;
                }
            }

            SpotsFoundToday += totalSpotsFound;
            SpotsSentToday += totalSpotsSent;

            _logService.LogCheckSummary(totalSpotsFound,totalSpotsSent,_userFilters.Count,filtersEvaluated);

            StatusMessage = totalSpotsFound > 0
                ? $"Found {totalSpotsFound}, sent {totalSpotsSent}"
                : $"No new spots ({filtersEvaluated} filters checked)";

            if(DateTime.Now.Minute == 0) {
                _logService.Log("Running periodic cleanup...");
                await _dbService.CleanupOldAlertsAsync(24*10);
            }
        } catch(Exception ex) {
            if(!_isDisposed) {
                StatusMessage = $"Error: {ex.Message}";
                _logService.LogError("CheckForNewSpotsAsync",ex);
            }
        }
    }

    [RelayCommand]
    private async Task SendTestMessageAsync() {
        if(!IsMqttConnected) {
            StatusMessage = "Cannot send test - MQTT not connected";
            _logService.Log("⚠ Test message failed - MQTT not connected");
            System.Windows.MessageBox.Show(
                "Please connect to MQTT broker first.",
                "Not Connected",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        if(!_userFilters.Any()) {
            StatusMessage = "Cannot send test - No enabled filters";
            _logService.Log("⚠ Test message failed - No enabled filters");
            System.Windows.MessageBox.Show(
                "No users have enabled filters.",
                "No Filters",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        // ✅ FIX: Always target the user selected in the UI, not just the first one in the list.
        // Priority: SelectedFilterUser (Filters tab combo) -> SelectedUser (Users tab grid) -> first user (fallback).
        var userCall = !string.IsNullOrWhiteSpace(SelectedFilterUser)
            ? SelectedFilterUser
            : SelectedUser?.UserCall;

        if(string.IsNullOrWhiteSpace(userCall)) {
            userCall = _userFilters.Keys.First();
            _logService.Log($"  ⚠ No user selected - defaulting to first user: {userCall}");
        }

        if(!_userFilters.ContainsKey(userCall)) {
            StatusMessage = $"Cannot send test - {userCall} has no enabled filters";
            _logService.Log($"⚠ Test message failed - Selected user {userCall} has no enabled filters");
            System.Windows.MessageBox.Show(
                $"Selected user {userCall} has no enabled filters.",
                "No Filters For User",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        try {
            StatusMessage = "Searching for test spot...";
            _logService.Log("🧪 Test message requested");

            _logService.Log($"  Searching test spot for selected user: {userCall}");

            // Get all filters for this user
            var allUserFilters = _allFiltersFromDb
                .Where(f => f.UserCall == userCall)
                .ToList();



            // ✅ FIX: Get latest test spot (now loops through ALL filters)
            var testSpot = await _dbService.GetLatestTestSpotAsync(userCall,allUserFilters);

            if(testSpot == null) {
                StatusMessage = "No test spot found in last 24 hours";
                _logService.Log($"  ⚠ No recent spots found for {userCall} in any of {allUserFilters.Count(f => f.IsEnabled)} enabled filters");

                System.Windows.MessageBox.Show(
                    $"No recent spots found for {userCall} in the last 24 hours.\n\n" +
                    $"Enabled filters checked: {allUserFilters.Count(f => f.IsEnabled)}\n\n" +
                    "Please ensure you have enabled filters with recent activity.",
                    "No Test Spot Found",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return;
            }

            // ✅ testSpot.Spotter already has "=>TEST" appended by GetLatestTestSpotAsync
            _logService.Log($"  ✓ Test spot: {testSpot.Call} @ {testSpot.Frequency:F3} MHz ({testSpot.Band} {testSpot.Mode})");
            _logService.Log($"  ✓ Spotter: {testSpot.Spotter}");
            _logService.Log($"  → Publishing to viewer for {userCall}");

            await _mqttService.PublishSpotToUserAsync(userCall,testSpot);

            // Log as sent spot on screen
            _logService.LogSpotSent(userCall,testSpot.Call,testSpot.Band,testSpot.Mode,
                testSpot.Frequency,testSpot.Spotter,testSpot.RptByType,testSpot.UTC,testSpot.Comment);

            StatusMessage = $"✓ Test sent: {testSpot.Call} on {testSpot.Band} → {userCall}";
            _logService.Log($"  ✓ Test message sent successfully");

            System.Windows.MessageBox.Show(
                $"Test message sent to {userCall}!\n\n" +
                $"DX Call: {testSpot.Call}\n" +
                $"Frequency: {testSpot.Frequency:F3} MHz\n" +
                $"Band: {testSpot.Band}\n" +
                $"Mode: {testSpot.Mode}\n" +
                $"Spotter: {testSpot.Spotter}\n" +
                $"Type: {testSpot.RptByType}\n" +
                $"UTC: {testSpot.UTC:HH:mm:ss}\n\n" +
                $"Check your viewer app!",
                "Test Message Sent",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        } catch(Exception ex) {
            StatusMessage = $"Test failed: {ex.Message}";
            _logService.LogError("SendTestMessage",ex);

            System.Windows.MessageBox.Show(
                $"Test message failed:\n\n{ex.Message}",
                "Test Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }


    partial void OnSelectedFilterUserChanged(string? value) {
        if(!ShowAllUsersFilters && value != null) {
            RefreshFilterDisplay();
        }
    }

    partial void OnShowAllUsersFiltersChanged(bool value) {
        RefreshFilterDisplay();
    }

    #endregion

    public void Dispose() {
        _isDisposed = true;  // ✅ Set FIRST to stop all async operations

        _monitorTimer.Stop();

        try {
            _logService.Dispose();
        } catch { }

        try {
            _mqttService.Dispose();
        } catch { }
    }
}