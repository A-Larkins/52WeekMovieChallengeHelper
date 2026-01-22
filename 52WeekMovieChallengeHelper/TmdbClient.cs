using Microsoft.Extensions.Configuration;
using RestSharp;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

public class TmdbClient
{
    private readonly string? _apiKey;
    private readonly RestClient _client;

    public TmdbClient()
    {
        var exePath = Assembly.GetExecutingAssembly().Location;
        var exeDir = Path.GetDirectoryName(exePath) ?? Directory.GetCurrentDirectory();
        
        var config = new ConfigurationBuilder()
            .AddJsonFile("/usr/local/bin/appsettings.json")
            .Build();
                            
        _apiKey = config["TMDB:ApiKey"];
        _client = new RestClient(new RestClientOptions("https://api.themoviedb.org/3/"));
    }

    public async Task<(string? ProducedBy, string? MusicBy)> GetProducerAndMusicByAsync(string title)
    {
        var searchRequest = new RestRequest($"search/movie?query={Uri.EscapeDataString(title)}&language=en-US");
        searchRequest.AddHeader("accept", "application/json");
        searchRequest.AddHeader("Authorization", $"Bearer {_apiKey}");
        var searchResponse = await _client.GetAsync(searchRequest);
        if (searchResponse.Content == null) return (null, null);

        using var doc = JsonDocument.Parse(searchResponse.Content);
        var results = doc.RootElement.GetProperty("results");
        if (results.GetArrayLength() == 0) return (null, null);

        var firstResult = results[0];
        int id = firstResult.GetProperty("id").GetInt32();

        var detailsRequest = new RestRequest($"movie/{id}?language=en-US&append_to_response=credits");
        detailsRequest.AddHeader("accept", "application/json");
        detailsRequest.AddHeader("Authorization", $"Bearer {_apiKey}");
        var detailsResponse = await _client.GetAsync(detailsRequest);
        if (detailsResponse.Content == null) return (null, null);

        var details = JsonDocument.Parse(detailsResponse.Content).RootElement;
        string? producedBy = null, musicBy = null;
        if (details.TryGetProperty("credits", out var credits))
        {
            if (credits.TryGetProperty("crew", out var crew) && crew.GetArrayLength() > 0)
            {
                producedBy = string.Join(", ", crew.EnumerateArray().Where(c => c.GetProperty("job").GetString() == "Producer").Select(c => c.GetProperty("name").GetString()));
                musicBy = string.Join(", ", crew.EnumerateArray().Where(c => c.GetProperty("job").GetString() == "Original Music Composer").Select(c => c.GetProperty("name").GetString()));
            }
        }
        return (producedBy, musicBy);
    }
}
