using System.Diagnostics;

// run with: 52wmovie

Console.Write("Enter a movie title: ");
var title = Console.ReadLine();

var omdbClient = new OmdbClient();
Movie? movie = await omdbClient.GetMovieByTitleAsync(title ?? "");

while (movie == null)
{
    Console.WriteLine("Movie not found. Please try again.");
    Console.Write("Enter a movie title: ");
    title = Console.ReadLine();
    movie = await omdbClient.GetMovieByTitleAsync(title ?? "");
}

var (producedBy, musicBy) = await new TmdbClient().GetProducerAndMusicByAsync(title ?? "");
movie.ProducedBy = producedBy ?? movie.ProducedBy;
movie.MusicBy = musicBy ?? movie.MusicBy;

Console.WriteLine("\n================ Movie Info ================");
Console.WriteLine($"Title:           {movie.Title}");
Console.WriteLine($"Year Released:   {movie.YearReleased}");
Console.WriteLine($"Run Time:        {movie.RunTime ?? "N/A"}");
Console.WriteLine($"Genre:           {movie.Genre ?? "N/A"}");
Console.WriteLine($"Rated:           {movie.Rated ?? "N/A"}");
Console.WriteLine($"Starring:        {movie.Staring ?? "N/A"}");
Console.WriteLine($"Directed By:     {movie.DirectedBy ?? "N/A"}");
Console.WriteLine($"Produced By:     {movie.ProducedBy ?? "N/A"}");
Console.WriteLine($"Written By:      {movie.WrittenBy ?? "N/A"}");
Console.WriteLine($"Music By:        {movie.MusicBy ?? "N/A"}");
Console.WriteLine($"Awards Received: {movie.AwardsReceived ?? "N/A"}");
Console.WriteLine();
Console.WriteLine($"Google search for Oscars & aspect ratio:\n{movie.GoogleSearchURL}");
Console.WriteLine($"IMDb Quotes page:\n{movie.ImdbQuotesURL}");
Console.WriteLine("============================================\n");

if (!string.IsNullOrEmpty(movie.GoogleSearchURL) && movie.GoogleSearchURL != "N/A")
    Process.Start("open", movie.GoogleSearchURL);

if (!string.IsNullOrEmpty(movie.ImdbQuotesURL) && movie.ImdbQuotesURL != "N/A")
    Process.Start("open", movie.ImdbQuotesURL);
