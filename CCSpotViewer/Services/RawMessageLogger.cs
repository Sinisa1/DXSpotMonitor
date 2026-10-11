using System.IO;
using System.Text;

namespace CCSpotViewer.Services;

/// <summary>
/// Writes raw CC11 lines to  C:\temp\CC11_Messages_YYYY-MM-DD_HHMMSS.log
/// Thread-safe: all writes are serialised through a SemaphoreSlim.
/// </summary>
public sealed class RawMessageLogger:IDisposable {
    private const string LogDirectory = @"C:\temp";

    private readonly StreamWriter _writer;
    private readonly SemaphoreSlim _lock = new(1,1);
    private bool _disposed;

    public string FilePath { get; }

    public RawMessageLogger() {
        Directory.CreateDirectory(LogDirectory);

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
        FilePath = Path.Combine(LogDirectory,$"CC11_Messages_{timestamp}.log");

        _writer = new StreamWriter(FilePath,append: false,Encoding.UTF8) {
            AutoFlush = true
        };
    }

    /// <summary>Appends a single raw line to the log file asynchronously.</summary>
    public async Task WriteLineAsync(string line) {
        if(_disposed) return;

        await _lock.WaitAsync();
        try {
            await _writer.WriteLineAsync(line);
        } finally {
            _lock.Release();
        }
    }

    public void Dispose() {
        if(_disposed) return;
        _disposed = true;
        _writer.Dispose();
        _lock.Dispose();
    }
}