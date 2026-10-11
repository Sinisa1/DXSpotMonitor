using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;
using DXSpotViewer.Models;
using Microsoft.Maui.Networking;
namespace DXSpotViewer.Services;

public class MqttService:IDisposable {
    private IMqttClient? _mqttClient;
    private readonly MqttFactory _mqttFactory;
    private string _userCall = "";
    private string _userTopic = "";
    private string _clientId = "";
    private bool _isSubscribed;
    private bool _isDisposed;
    private int _reconnectAttempts = 0;
    private const int MaxReconnectAttempts = 20;
    private readonly object _lock = new();

    private readonly string _brokerAddress = "broker.hivemq.com";
    private readonly int _brokerPort = 1883;
    private readonly string _baseTopic = "hamradio/va3tnm";

    public event EventHandler<DxSpotMessage>? SpotReceived;
    public event EventHandler<string>? StatusChanged;
    public event EventHandler<bool>? ConnectionChanged;

    public bool IsConnected { get; private set; }
    public string UserCall => _userCall;

    public MqttService() {
        _mqttFactory = new MqttFactory();
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }
    private void OnConnectivityChanged(object? sender,ConnectivityChangedEventArgs e) {
        if(e.NetworkAccess == NetworkAccess.Internet && !IsConnected && !string.IsNullOrEmpty(_userCall)) {
            System.Diagnostics.Debug.WriteLine("🔄 Connectivity restored — forcing reconnect");
            _reconnectAttempts = 0; // reset backoff since this is a fresh network event
            _ = Task.Run(async () => {
                try { await ConnectAsync(_userCall); } catch(Exception ex) { System.Diagnostics.Debug.WriteLine($"❌ Connectivity-triggered reconnect failed: {ex.Message}"); }
            });
        }
    }
    public async Task ConnectAsync(string userCall) {
        if(string.IsNullOrEmpty(userCall))
            throw new ArgumentException("UserCall cannot be empty");

        lock(_lock) {
            if(IsConnected && _userCall == userCall.ToUpper()) {
                System.Diagnostics.Debug.WriteLine("✅ Already connected, skipping");
                return;
            }
        }

        _userCall = userCall.ToUpper();
        _userTopic = $"{_baseTopic}/viewer/{_userCall}";

        if(string.IsNullOrEmpty(_clientId)) {
            _clientId = $"DXV_{_userCall}_{Guid.NewGuid():N}"[..23];
        }

        try {
            StatusChanged?.Invoke(this,$"Connecting as {_userCall}...");
            System.Diagnostics.Debug.WriteLine($"🔌 Connecting as {_userCall} (ClientId: {_clientId})");

            // Cleanup existing client
            if(_mqttClient != null) {
                _mqttClient.DisconnectedAsync -= OnDisconnectedAsync;
                _mqttClient.ConnectedAsync -= OnConnectedAsync;
                _mqttClient.ApplicationMessageReceivedAsync -= OnMessageReceivedAsync;

                if(_mqttClient.IsConnected) {
                    try { await _mqttClient.DisconnectAsync(); } catch { }
                }
                _mqttClient.Dispose();
            }

            _isSubscribed = false;
            _mqttClient = _mqttFactory.CreateMqttClient();

            _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
            _mqttClient.ConnectedAsync += OnConnectedAsync;
            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(_brokerAddress,_brokerPort)
                .WithClientId(_clientId)
                .WithCleanSession(true)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(15))
                .WithTimeout(TimeSpan.FromSeconds(15))
                .Build();

            await _mqttClient.ConnectAsync(options);
            _reconnectAttempts = 0; // Reset on successful connection
        } catch(Exception ex) {
            StatusChanged?.Invoke(this,$"Connection failed");
            IsConnected = false;
            ConnectionChanged?.Invoke(this,false);
            System.Diagnostics.Debug.WriteLine($"❌ Connect failed: {ex.Message}");
            throw;
        }
    }

    private async Task OnConnectedAsync(MqttClientConnectedEventArgs e) {
        lock(_lock) { IsConnected = true; }

        ConnectionChanged?.Invoke(this,true);
        StatusChanged?.Invoke(this,$"Connected as {_userCall}");
        System.Diagnostics.Debug.WriteLine($"✅ Connected as {_userCall}");

        if(!_isSubscribed && _mqttClient != null) {
            try {
                await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic(_userTopic)
                    .WithAtMostOnceQoS()
                    .Build());

                _isSubscribed = true;
                System.Diagnostics.Debug.WriteLine($"✅ Subscribed to: {_userTopic}");
            } catch(Exception ex) {
                System.Diagnostics.Debug.WriteLine($"❌ Subscribe failed: {ex.Message}");
            }
        }
    }

    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e) {
        lock(_lock) {
            IsConnected = false;
            _isSubscribed = false;
        }

        ConnectionChanged?.Invoke(this,false);
        System.Diagnostics.Debug.WriteLine($"⚠ Disconnected: {e.Reason}");

        // ✅ Auto-reconnect with exponential backoff
        if(!_isDisposed && !string.IsNullOrEmpty(_userCall)) {
            _reconnectAttempts++;

            if(_reconnectAttempts <= MaxReconnectAttempts) {
                // Exponential backoff: 3s, 6s, 9s, 12s... max 30s
                int delaySeconds = Math.Min(3 * _reconnectAttempts,30);

                StatusChanged?.Invoke(this,$"Reconnecting in {delaySeconds}s...");
                System.Diagnostics.Debug.WriteLine($"🔄 Auto-reconnect in {delaySeconds}s (attempt {_reconnectAttempts}/{MaxReconnectAttempts})");

                _ = Task.Run(async () => {
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

                    if(!IsConnected && !_isDisposed && !string.IsNullOrEmpty(_userCall)) {
                        System.Diagnostics.Debug.WriteLine($"🔄 Attempting reconnect #{_reconnectAttempts}...");
                        try {
                            await ConnectAsync(_userCall);
                        } catch(Exception ex) {
                            System.Diagnostics.Debug.WriteLine($"❌ Reconnect #{_reconnectAttempts} failed: {ex.Message}");
                        }
                    }
                });
            } else {
                StatusChanged?.Invoke(this,"Connection lost - tap to retry");
                System.Diagnostics.Debug.WriteLine($"❌ Max reconnect attempts ({MaxReconnectAttempts}) reached");
            }
        }

        return Task.CompletedTask;
    }

    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e) {
        try {
            var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            System.Diagnostics.Debug.WriteLine($"📨 Message received ({payload.Length} chars)");

            var options = new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            };

            var spot = JsonSerializer.Deserialize<DxSpotMessage>(payload,options);

            if(spot != null) {
                System.Diagnostics.Debug.WriteLine($"✅ Parsed: {spot.Call} {spot.Band} {spot.Mode}");
                MainThread.BeginInvokeOnMainThread(() => SpotReceived?.Invoke(this,spot));
            }
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"❌ Parse error: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Disconnect for logout - clears userCall to prevent auto-reconnect
    /// </summary>
    public async Task DisconnectAsync() {
        System.Diagnostics.Debug.WriteLine("🔌 DisconnectAsync called (logout)");

        _isSubscribed = false;
        _userCall = ""; // ✅ Clear to prevent auto-reconnect
        _reconnectAttempts = 0;

        if(_mqttClient?.IsConnected == true) {
            try {
                await _mqttClient.DisconnectAsync();
                System.Diagnostics.Debug.WriteLine("✅ Disconnected cleanly");
            } catch(Exception ex) {
                System.Diagnostics.Debug.WriteLine($"⚠ Disconnect error: {ex.Message}");
            }
        }

        lock(_lock) { IsConnected = false; }
        ConnectionChanged?.Invoke(this,false);
        StatusChanged?.Invoke(this,"Logged out");
    }

    /// <summary>
    /// Reset reconnect counter (call when manually retrying)
    /// </summary>
    public void ResetReconnectCounter() {
        _reconnectAttempts = 0;
    }

    public void Dispose() {
        _isDisposed = true;
        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
        _mqttClient?.Dispose();
    }
}