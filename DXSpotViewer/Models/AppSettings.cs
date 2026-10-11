namespace DXSpotViewer.Models;

public class AppSettings {
    public string BrokerAddress { get; set; } = "broker.hivemq.com";
    public int BrokerPort { get; set; } = 1883;
    public string BaseTopic { get; set; } = "hamradio/va3tnm";
    public string ClientId { get; set; } = $"VA3TNM_Phone_{Guid.NewGuid():N}";

    // Subscription filters
    public bool SubscribeAll { get; set; } = true;
    public List<string> BandFilters { get; set; } = new() { "20m","40m" };
    public List<string> ModeFilters { get; set; } = new() { "CW","SSB","FT8" };
    public bool OnlyNeeded { get; set; } = false;
    public bool EnableNotifications { get; set; } = true;
    public bool EnableVibration { get; set; } = true;

    // ✅ NEW: Sound notification
    public bool EnableSound { get; set; } = true;

    // Display settings
    public int MaxSpotsDisplayed { get; set; } = 100;
}