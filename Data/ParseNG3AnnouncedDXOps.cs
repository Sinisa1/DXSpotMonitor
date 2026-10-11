using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;
using System.Net;
using System.Threading;
namespace Data 
{
    public class DXAnnouncement {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string DXCCPrefix { get; set; }
        public string DXCCName { get; set; }
        public string Callsign { get; set; }
        public string QSLVia { get; set; }
        public string ReportedBy { get; set; }
        public DateTime? ReportedDate { get; set; }
        public string Info { get; set; }
        public bool IsActive { get; set; }

        // Overall QSO status (W = worked, N = not worked)
        public string Wkd { get; set; }
        // Overall confirmation (Y = confirmed on LoTW, N = not confirmed)
        public string Cfm { get; set; }

        // Per-band worked/confirmed codes: C=CW confirmed, c=CW worked, S=SSB confirmed, s=SSB worked
        public string b160m { get; set; }
        public string b80m { get; set; }
        public string b40m { get; set; }
        public string b30m { get; set; }
        public string b20m { get; set; }
        public string b17m { get; set; }
        public string b15m { get; set; }
        public string b12m { get; set; }
        public string b10m { get; set; }
        public string b6m { get; set; }

        // Formatted date strings for display
        public string StartDateStr => StartDate.ToString("yyyy-MM-dd");
        public string EndDateStr => EndDate.ToString("yyyy-MM-dd");
        public string ReportedDateStr => ReportedDate?.ToString("yyyy-MM-dd") ?? string.Empty;

        public bool IsCurrentlyActive =>
            DateTime.Now.Date >= StartDate.Date &&
            DateTime.Now.Date <= EndDate.Date;
    }
    public class DXAnnouncementParser 
    {
        private const string DEFAULT_URL = "http://www.ng3k.com/misc/adxo.html";
        private const int TIMEOUT_SECONDS = 10; // Reduced timeout

        private async Task<string> DownloadStringWithTimeoutAsync(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
        
            // Basic settings
            request.Method = "GET";
            request.Timeout = TIMEOUT_SECONDS * 1000;
            request.ReadWriteTimeout = TIMEOUT_SECONDS * 1000;
            request.AllowAutoRedirect = true;
            request.MaximumAutomaticRedirections = 3;
        
            // Headers
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.KeepAlive = false;
        
            // Use system proxy if available
            request.Proxy = WebRequest.GetSystemWebProxy();
        
            try
            {
                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    Debug.WriteLine($"Response status: {response.StatusCode}");
                
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new WebException($"Server returned {response.StatusCode}");
                    }

                    using (var stream = response.GetResponseStream())
                    using (var reader = new System.IO.StreamReader(stream))
                    {
                        return await reader.ReadToEndAsync();
                    }
                }
            }
            catch (WebException ex)
            {
                Debug.WriteLine($"WebException: {ex.Message}");
                Debug.WriteLine($"Status: {ex.Status}");
                if (ex.Response != null)
                {
                    using (var errorResponse = (HttpWebResponse)ex.Response)
                    {
                        Debug.WriteLine($"Error response: {errorResponse.StatusCode}");
                    }
                }
                throw;
            }
        }

        public async Task<List<DXAnnouncement>> DownloadAndParseAsync(string url = DEFAULT_URL) 
        {
            try 
            {
                // Configure TLS
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.DefaultConnectionLimit = 10;
                ServicePointManager.Expect100Continue = false;

                // Download with timeout and cancellation
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TIMEOUT_SECONDS)))
                {
                    var downloadTask = DownloadStringWithTimeoutAsync(url);
                    var completedTask = await Task.WhenAny(downloadTask, Task.Delay(TIMEOUT_SECONDS * 1000));
                    
                    if (completedTask != downloadTask)
                    {
                        throw new TimeoutException($"Download timed out after {TIMEOUT_SECONDS} seconds");
                    }

                    string html = await downloadTask;

                    if (string.IsNullOrEmpty(html))
                    {
                        throw new Exception("Downloaded HTML content is empty");
                    }

                    return ParseHtml(html);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DownloadAndParseAsync: {ex.GetType().Name} - {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public List<DXAnnouncement> ParseHtml(string html) {
            var announcements = new List<DXAnnouncement>();

            // Load HTML into HtmlAgilityPack
            HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            // Find the table with class="adxoitem"
            var rows = doc.DocumentNode.SelectNodes("//tr[@class='adxoitem']");
            if (rows==null) return announcements;

            foreach (var row in rows) {
                try {
                    var cells = row.SelectNodes(".//td");
                    if (cells?.Count<7) continue;

                    var reportInfo = ExtractReportedInfo(cells[5].InnerText.Trim());
                    var announcement = new DXAnnouncement {
                        StartDate = ParseDate(cells[0].InnerText.Trim()),
                        EndDate = ParseDate(cells[1].InnerText.Trim()),
                        DXCCName = cells[2].InnerText.Trim(),
                        Callsign = ExtractCallsign(cells[3].InnerText.Trim()),
                        QSLVia = cells[4].InnerText.Trim(),
                        ReportedBy = reportInfo.source,
                        ReportedDate = reportInfo.date,
                        Info = cells[6].InnerText.Trim(),
                        IsActive = row.GetAttributeValue("bgcolor", "") == "#FFDAB9"
                    };

                    announcements.Add(announcement);
                } catch (Exception ex) {
                    Debug.WriteLine($"Error parsing row: {ex.Message}");
                    continue;
                }
            }

            return announcements;
        }

        private DateTime ParseDate(string dateStr) {
            // Handle date format like "2025 Sep19"
            var match = Regex.Match(dateStr, @"(\d{4})\s+([A-Za-z]+)(\d{1,2})");
            if (match.Success) {
                var year = int.Parse(match.Groups[1].Value);
                var month = DateTime.ParseExact(match.Groups[2].Value, "MMM", CultureInfo.InvariantCulture).Month;
                var day = int.Parse(match.Groups[3].Value);
                return new DateTime(year, month, day);
            }
            throw new FormatException($"Unable to parse date: {dateStr}");
        }

        private string ExtractCallsign(string cell) {
            // Remove [spots] and other HTML elements, keep just the callsign
            return Regex.Replace(cell, @"<[^>]+>|\[spots\]", "").Trim();
        }

        private (string source, DateTime? date) ExtractReportedInfo(string cell)
        {
            try
            {
                // First remove HTML tags
                string cleanText = Regex.Replace(cell, "<[^>]+>", "").Trim();
                
                // Get last 8 characters for date if text is long enough
                if (cleanText.Length >= 8)
                {
                    string dateStr = cleanText.Substring(cleanText.Length - 8);
                    string source = cleanText.Substring(0, cleanText.Length - 8).Trim();

                    // Parse YYYYMMDD format
                    if (DateTime.TryParseExact(dateStr, 
                        "yyyyMMdd", 
                        CultureInfo.InvariantCulture, 
                        DateTimeStyles.None, 
                        out DateTime reportDate))
                    {
                        return (source, reportDate);
                    }
                }
                
                // If date parsing fails, return all text as source with null date
                return (cleanText, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error parsing reported info: {ex.Message} for cell: {cell}");
                return (cell.Trim(), null);
            }
        }

        private string ExtractReportedBy(string cell) {
            var (source, date) = ExtractReportedInfo(cell);
            return source;
        }
    }
}