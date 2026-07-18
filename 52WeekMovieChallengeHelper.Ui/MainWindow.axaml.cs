using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Media;

namespace MovieChallengeHelper.Ui;

public partial class MainWindow : Window
{
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

        Opened += (_, _) => TitleBox.Focus();
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
        catch (FileNotFoundException)
        {
            ShowStatus("Couldn't read /usr/local/bin/appsettings.json — run publish.sh to install it.", isError: true);
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
