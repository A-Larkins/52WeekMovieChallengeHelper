using RestSharp;
using System.Text.Json;

public class OmdbClient
{
    private readonly string _apiKey;
    private readonly RestClient _client;

    public OmdbClient()
    {
        _apiKey = AppConfiguration.RequireKey("OMDB:ApiKey");
        _client = new RestClient("https://www.omdbapi.com/");
    }

    public async Task<Movie?> GetMovieByTitleAsync(string title, string? year = null)
    {
        var request = new RestRequest();
        request.AddParameter("t", title);
        if (!string.IsNullOrEmpty(year))
            request.AddParameter("y", year);
        request.AddParameter("apikey", _apiKey);

        var response = await _client.GetAsync(request);
        if (response.Content == null) return null;

        using var doc = JsonDocument.Parse(response.Content);
        var root = doc.RootElement;
        if (root.TryGetProperty("Response", out var resp) && resp.GetString() == "False")
            return null;

        string? rated = root.TryGetProperty("Rated", out var ratedProp) ? ratedProp.GetString() : null;
        string? awardsRaw = root.TryGetProperty("Awards", out var awardsProp) ? awardsProp.GetString() : null;
        string? runTime = root.TryGetProperty("Runtime", out var rt) ? rt.GetString() : null;
        string? genre = root.TryGetProperty("Genre", out var g) ? g.GetString() : null;
        string? starring = root.TryGetProperty("Actors", out var actors) ? actors.GetString() : null;
        string? directedBy = root.TryGetProperty("Director", out var director) ? director.GetString() : null;
        string? writtenBy = root.TryGetProperty("Writer", out var writer) ? writer.GetString() : null;
        string? awards = !string.IsNullOrEmpty(awardsRaw) && awardsRaw.Contains("Oscar") ? $"{awardsRaw.Split(' ')[1]} Oscars" : "No Oscars";
        string? imdbID = root.TryGetProperty("imdbID", out var idProp) ? idProp.GetString() : null;
        string? imdbQuotesUrl = imdbID != null ? $"https://www.imdb.com/title/{imdbID}/quotes/" : "N/A";
    
        return new Movie
        {
            Title = root.TryGetProperty("Title", out var t) ? t.GetString() : null,
            YearReleased = root.TryGetProperty("Year", out var y) ? y.GetString() : null,
            AspectRatio = null, // OMDB does not provide aspect ratio
            Rated = rated,
            AwardsReceived = awards,
            RunTime = runTime,
            Genre = genre,
            Staring = starring,
            DirectedBy = directedBy,
            ProducedBy = null, // to be filled from TMDB
            WrittenBy = writtenBy,
            MusicBy = null, // to be filled from TMDB,
            GoogleSearchURL = $"https://www.google.com/search?q={Uri.EscapeDataString(title + " oscars won and aspect ratio")}",
            ImdbQuotesURL = imdbQuotesUrl
        };
    }

}
