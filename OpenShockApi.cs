using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Logging;
using ExitGames.Client.Photon.StructWrapping;
using Newtonsoft.Json;
using ReOpenShock;
using Sirenix.Utilities;

namespace ReOpenShock;
public enum ControlType
{
    Stop = 0,
    Shock = 1,
    Vibrate = 2,
    Sound = 3
}
public class ControlRequest
{
    public IEnumerable<Control> Shocks { get; set; } = null!;
    public string CustomName { get; set; }
}

public class Control
{
    public string Id { get; set; }
    public ControlType Type { get; set; }
    public int Intensity { get; set; }
    public int Duration { get; set; }
}

public class DevicesRes
{
    public string message { get; set; }
    public List<Hub> data { get; set; }
}

public class Hub
{
    public string id { get; set; }
    public string name { get; set; }
    public DateTime createdOn { get; set; }
    public List<Shocker> shockers { get; set; }
}

public class Shocker
{
    public string name { get; set; }
    public bool isPaused { get; set; }
    public DateTime createdOn { get; set; }
    public string id { get; set; }
    public int rfId { get; set; }
    public string model { get; set; }
}



public class OpenShockApi
{
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("OpenShockApi");
    private readonly HttpClient _httpClient;
    
    public OpenShockApi(string apiToken, Uri server)
    {
        var handler = new HttpClientHandler
        {
            Proxy = new WebProxy("http://localhost:9000"),
            UseProxy = true
        };
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = server
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", $"ReOpenShock/{ReOpenShock.PluginVersion}");
        _httpClient.DefaultRequestHeaders.Add("OpenShockToken", apiToken);
    }

    public async Task Control(IEnumerable<Control> shocks)
    {
        Logger.LogInfo("Sending control request to OpenShock API");
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/2/shockers/control")
        {
            Content = new StringContent(JsonConvert.SerializeObject(new ControlRequest
            {
                Shocks = shocks,
                CustomName = "Integrations.ReOpenShock"
            }), Encoding.UTF8, "application/json")
        };
        var response = await _httpClient.SendAsync(requestMessage);
        
        if (!response.IsSuccessStatusCode) Logger.LogError($"Failed to send control request to OpenShock API [{response.StatusCode}]");
        else Logger.LogInfo("Successfully sent control request");
    }

    public ImmutableList<string> devices = new([]);
    
    public async Task GetDevices()
    {
        Logger.LogInfo("Sending get devices request to OpenShock API");
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/1/shockers/own");
        var response = await _httpClient.SendAsync(requestMessage);
        if (!response.IsSuccessStatusCode)
        {
            Logger.LogError($"Failed to send get devices request to OpenShock API [{response.StatusCode}]");
            return;
        }

        var resCl = JsonConvert.DeserializeObject<DevicesRes>(await response.Content.ReadAsStringAsync());
        List<string> shockerIds = [];
        foreach (var hub in resCl.data)
        {
            foreach (var shocker in hub.shockers)
            {
                shockerIds.Add(shocker.id);
            }
        }
        

        devices = shockerIds.ToImmutableList();

        Logger.LogInfo($"Got {devices.Count} shockers");
    }
}