using System.Diagnostics;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace MovieChallengeHelper.Ui;

public partial class MainWindow : Window
{
    /// <summary>Where the window was last left, so it reopens on the same display.</summary>
    private sealed record Placement(int X, int Y, double Width, double Height);

    private static string PlacementPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library", "Application Support", "52WeekMovieChallengeHelper", "window.json");

    private OmdbClient? _omdb;
    private TmdbClient? _tmdb;

    private string? _googleUrl;
    private string? _quotesUrl;

    public MainWindow()
    {
        InitializeComponent();

        SearchButton.Click += async (_, _) => await SearchAsync();
        GoogleButton.Click += (_, _) => OpenUrl(_googleUrl);
        QuotesButton.Click += (_, _) => OpenUrl(_quotesUrl);

        Opened += (_, _) =>
        {
            RestorePlacement();
            TitleBox.Focus();
        };

        Closing += (_, _) => SavePlacement();
    }

    /// <summary>
    /// Puts the window back where it was last closed. Falls back to the XAML's
    /// CenterScreen if nothing is saved or that display is no longer connected.
    /// </summary>
    private void RestorePlacement()
    {
        try
        {
            if (!File.Exists(PlacementPath)) return;

            var saved = JsonSerializer.Deserialize<Placement>(File.ReadAllText(PlacementPath));
            if (saved is null) return;

            var point = new PixelPoint(saved.X, saved.Y);
            if (Screens.ScreenFromPoint(point) is null) return;

            Position = point;
            if (saved.Width > 0) Width = saved.Width;
            if (saved.Height > 0) Height = saved.Height;
        }
        catch
        {
            // A corrupt or unreadable state file just means default placement.
        }
    }

    private void SavePlacement()
    {
        try
        {
            var placement = new Placement(Position.X, Position.Y, ClientSize.Width, ClientSize.Height);
            Directory.CreateDirectory(Path.GetDirectoryName(PlacementPath)!);
            File.WriteAllText(PlacementPath, JsonSerializer.Serialize(placement));
        }
        catch
        {
            // Not worth interrupting shutdown over.
        }
    }

    private async Task SearchAsync()
    {
        var title = TitleBox.Text?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            ShowStatus("Enter a movie title to search.", isError: true);
            return;
        }

        SearchButton.IsEnabled = false;
        TitleBox.IsEnabled = false;
        ShowStatus("Searching…", isError: false);

        try
        {
            // The API clients read their keys in the constructor, so a missing or
            // malformed appsettings.json surfaces here rather than at startup.
            _omdb ??= new OmdbClient();

            var movie = await _omdb.GetMovieByTitleAsync(title);
            if (movie == null)
            {
                ResultsCard.IsVisible = false;
                LinksPanel.IsVisible = false;
                ShowStatus($"No match for “{title}”. Check the spelling and try again.", isError: true);
                return;
            }

            // TMDB only fills in two extra credits — if it fails, still show the OMDB result.
            try
            {
                _tmdb ??= new TmdbClient();
                var (producedBy, musicBy) = await _tmdb.GetProducerAndMusicByAsync(title);
                movie.ProducedBy = producedBy ?? movie.ProducedBy;
                movie.MusicBy = musicBy ?? movie.MusicBy;
            }
            catch
            {
                // Ignored: producer/composer stay "N/A".
            }

            Render(movie);
            HideStatus();
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException)
        {
            ShowStatus(ex.Message, isError: true);
        }
        catch (Exception ex)
        {
            ShowStatus($"Lookup failed: {ex.Message}", isError: true);
        }
        finally
        {
            SearchButton.IsEnabled = true;
            TitleBox.IsEnabled = true;
            TitleBox.Focus();
        }
    }

    private void Render(Movie movie)
    {
        TitleValue.Text = Or(movie.Title);
        YearValue.Text = Or(movie.YearReleased);
        RunTimeValue.Text = Or(movie.RunTime);
        GenreValue.Text = Or(movie.Genre);
        RatedValue.Text = Or(movie.Rated);
        StarringValue.Text = Or(movie.Staring);
        DirectedByValue.Text = Or(movie.DirectedBy);
        ProducedByValue.Text = Or(movie.ProducedBy);
        WrittenByValue.Text = Or(movie.WrittenBy);
        MusicByValue.Text = Or(movie.MusicBy);
        AwardsValue.Text = Or(movie.AwardsReceived);

        _googleUrl = Usable(movie.GoogleSearchURL);
        _quotesUrl = Usable(movie.ImdbQuotesURL);

        GoogleButton.IsVisible = _googleUrl != null;
        QuotesButton.IsVisible = _quotesUrl != null;

        ResultsCard.IsVisible = true;
        LinksPanel.IsVisible = _googleUrl != null || _quotesUrl != null;
    }

    private static string Or(string? value) => string.IsNullOrWhiteSpace(value) ? "N/A" : value;

    private static string? Usable(string? url) =>
        string.IsNullOrWhiteSpace(url) || url == "N/A" ? null : url;

    private static void OpenUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return;
        Process.Start("open", url);
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError ? Brushes.IndianRed : Brushes.Gray;
        StatusText.IsVisible = true;
    }

    private void HideStatus()
    {
        StatusText.Text = "";
        StatusText.IsVisible = false;
    }
}
