namespace DXSpotViewer;

/// <summary>
/// Build information
/// </summary>
public static class BuildInfo {
    /// <summary>
    /// App start time (for debugging)
    /// </summary>
    public static string CompilationTime { get; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
}