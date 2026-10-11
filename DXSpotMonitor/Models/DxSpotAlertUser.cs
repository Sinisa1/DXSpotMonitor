using CommunityToolkit.Mvvm.ComponentModel;

namespace DXSpotMonitor.Models;

/// <summary>
/// Represents a user in the DX spot alert system
/// </summary>
public partial class DxSpotAlertUser:ObservableObject {
    [ObservableProperty]
    private string _userCall = "";

    // ✅ MANUAL PROPERTY: Use PIN (all caps) instead of source-generated Pin
    private int _pin;
    public int PIN {
        get => _pin;
        set => SetProperty(ref _pin,value);
    }

    [ObservableProperty]
    private string? _userName;

    [ObservableProperty]
    private string? _comment;

    /// <summary>
    /// Filter count (not in database, for display only)
    /// </summary>
    [ObservableProperty]
    private int _filterCount;
}