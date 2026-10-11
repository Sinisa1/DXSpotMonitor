using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using DXSpotMonitor.Models;

using Encoding = System.Text.Encoding;

namespace DXSpotMonitor.Services;

/// <summary>
/// MQTT service to publish spots to Android app
/// </summary>
public class MqttPublisherService:IDisposable {
    private IMqttClient? _mqttClient;
    private readonly MqttFactory _mqttFactory;
    private bool _isConnected;
    private bool _isDisposed;
    private readonly string _brokerAddress = "broker.hivemq.com";
    private readonly int _brokerPort = 1883;
    private readonly string _baseTopic = "hamradio/va3tnm";

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<bool>? ConnectionChanged;

    public bool IsConnected => _isConnected;

    public MqttPublisherService() {
        _mqttFactory = new MqttFactory();
    }

    public async Task ConnectAsync() {
        if(_isDisposed) return;

        try {
            StatusChanged?.Invoke(this,"Connecting to MQTT broker...");

            _mqttClient = _mqttFactory.CreateMqttClient();

            _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
            _mqttClient.ConnectedAsync += OnConnectedAsync;

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(_brokerAddress,_brokerPort)
                .WithClientId($"VA3TNM_SpotMonitor_{Guid.NewGuid():N}")
                .WithCleanSession(true)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
                .Build();

            await _mqttClient.ConnectAsync(options);
        } catch(Exception ex) {
            StatusChanged?.Invoke(this,$"Connection failed: {ex.Message}");
            _isConnected = false;
            ConnectionChanged?.Invoke(this,false);
        }
    }

    private Task OnConnectedAsync(MqttClientConnectedEventArgs e) {
        _isConnected = true;
        ConnectionChanged?.Invoke(this,true);
        StatusChanged?.Invoke(this,"Connected to MQTT broker");
        return Task.CompletedTask;
    }

    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e) {
        _isConnected = false;
        ConnectionChanged?.Invoke(this,false);
        StatusChanged?.Invoke(this,"Disconnected from MQTT broker");

        if(!_isDisposed) {
            Task.Run(async () => {
                await Task.Delay(5000);
                if(!_isConnected && !_isDisposed) {
                    await ConnectAsync();
                }
            });
        }

        return Task.CompletedTask;
    }

    public async Task PublishSpotAsync(DxSpotMessage spot) {
        if(_isDisposed) return;
        if(_mqttClient == null || !_mqttClient.IsConnected) {
            StatusChanged?.Invoke(this,"❌ Cannot publish - not connected to MQTT");
            System.Diagnostics.Debug.WriteLine($"❌ MQTT NOT CONNECTED - Cannot send {spot.Call}");
            return;
        }

        try {
            var topic = $"{_baseTopic}/spots/all";

            // ✅ FIX: Remove PropertyNamingPolicy - let attributes handle it
            var json = JsonSerializer.Serialize(spot,new JsonSerializerOptions {
                WriteIndented = false
                // ✅ REMOVED: PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                // Let [JsonPropertyName] attributes define the names
            });

            System.Diagnostics.Debug.WriteLine($"Publishing to {topic}: {json.Substring(0,Math.Min(100,json.Length))}");

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(json)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce)
                .WithRetainFlag(false)
                .Build();

            var result = await _mqttClient.PublishAsync(message);

            if(result.ReasonCode == MqttClientPublishReasonCode.Success) {
                StatusChanged?.Invoke(this,$"✓ Published: {spot.Call} on {spot.Band}");
            }
        } catch(Exception ex) {
            StatusChanged?.Invoke(this,$"❌ Publish failed: {ex.Message}");
        }
    }

    public async Task PublishSpotToUserAsync(string userCall,DxSpotMessage spot) {
        if(_isDisposed) return;
        if(_mqttClient == null || !_mqttClient.IsConnected) {
            System.Diagnostics.Debug.WriteLine($"❌ MQTT NOT CONNECTED - Cannot send to {userCall}");
            return;
        }

        try {
            var topic = $"{_baseTopic}/viewer/{userCall.ToUpper()}";

            // ✅ FIX: Remove PropertyNamingPolicy - let attributes handle it
            var options = new JsonSerializerOptions {
                WriteIndented = false
                // ✅ REMOVED: PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                // Let [JsonPropertyName] attributes define the names
            };

            var json = JsonSerializer.Serialize(spot,options);

            System.Diagnostics.Debug.WriteLine($"📤 TEST MESSAGE to {userCall}:");
            System.Diagnostics.Debug.WriteLine($"   Topic: {topic}");
            System.Diagnostics.Debug.WriteLine($"   JSON: {json}");

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(json)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce)
                .WithRetainFlag(false)
                .Build();

            var result = await _mqttClient.PublishAsync(message);

            if(result.ReasonCode == MqttClientPublishReasonCode.Success) {
                System.Diagnostics.Debug.WriteLine($"  ✅ Test message published successfully to topic: {topic}");
            } else {
                System.Diagnostics.Debug.WriteLine($"  ❌ Test message publish failed: {result.ReasonCode}");
            }
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"❌ Publish error for {userCall}: {ex.Message}");
        }
    }

    public async Task DisconnectAsync() {
        if(_mqttClient?.IsConnected == true) {
            await _mqttClient.DisconnectAsync();
        }
        _isConnected = false;
        ConnectionChanged?.Invoke(this,false);
    }

    public void Dispose() {
        _isDisposed = true;
        _isConnected = false;

        try {
            _mqttClient?.DisconnectAsync().Wait(TimeSpan.FromSeconds(2));
        } catch { }

        _mqttClient?.Dispose();
        _mqttClient = null;
    }
}