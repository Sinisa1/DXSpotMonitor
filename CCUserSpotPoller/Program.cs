using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CCUserSpotPoller {
    internal class Program {
        private const string Host = "127.0.0.1";
        private const int Port = 7300;
        private const string Callsign = "VE3ZR"; // Replace with your callsign

        static async Task Main(string[] args) {
            Console.Title = "Raw CC1/CC11 Spot Listener";
            Console.WriteLine($"[+] Connecting to CC User at {Host}:{Port}...");

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s,e) => {
                e.Cancel = true;
                cts.Cancel();
            };

            try {
                using var client = new TcpClient();
                await client.ConnectAsync(Host,Port,cts.Token);
                Console.WriteLine("[+] Connected! Initializing CC1 mode & RBN skimmers...");

                using var stream = client.GetStream();
                using var reader = new StreamReader(stream,Encoding.ASCII);
                using var writer = new StreamWriter(stream,Encoding.ASCII) { AutoFlush = true };

                // 1. Send login callsign
                await writer.WriteLineAsync(Callsign);
                await Task.Delay(200);

                // 2. Enable CC caret-delimited output
                await writer.WriteLineAsync("SET/VE7CC");
                await Task.Delay(200);

                // 3. Enable RBN / CW Skimmer spots on the node
                await writer.WriteLineAsync("SET/SKIMMER");
                await Task.Delay(200);
                await writer.WriteLineAsync("SET/RBN"); // Alternative command used by some DXSpider nodes

                Console.WriteLine("[+] Initialization sent. Listening for raw stream...\n");

                // 4. Continuously read and print raw line output
                await ReadStreamAsync(reader,cts.Token);
            } catch(OperationCanceledException) {
                Console.WriteLine("\n[-] Disconnected by user.");
            } catch(Exception ex) {
                Console.WriteLine($"\n[!] Error: {ex.Message}");
            }
        }

        private static async Task ReadStreamAsync(StreamReader reader,CancellationToken token) {
            while(!token.IsCancellationRequested) {
                string? line = await reader.ReadLineAsync(token);
                if(line == null) break; // Socket closed

                // Filter for raw CC caret-delimited lines
                if(line.StartsWith("CC1^",StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("CC11^",StringComparison.OrdinalIgnoreCase)) {
                    // Print the exact raw packet directly
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(line);
                    Console.ResetColor();
                } else {
                    // General node console responses / prompt text
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"[NODE] {line}");
                    Console.ResetColor();
                }
            }
        }
    }
}