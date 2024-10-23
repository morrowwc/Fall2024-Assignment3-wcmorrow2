using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Fall2024_Assignment3_wcmorrow2.Data;
using Fall2024_Assignment3_wcmorrow2.Models;
using System.Numerics;
using System.Diagnostics;
using OpenAI.Chat;
using Microsoft.AspNetCore.Routing;
using Azure.AI.OpenAI;
using VaderSharp2;
using Newtonsoft.Json;

namespace Fall2024_Assignment3_wcmorrow2.Controllers
{
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public MoviesController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // GET: Movies
        public async Task<IActionResult> Index()
        {
            return View(await _context.Movie.ToListAsync());
        }

        // GET: Movies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie
                .Include(m => m.Reviews) // Include related reviews
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }
            var movieDetailsViewModel = new MovieDetailsViewModel
            {
                Value = movie,
                Actors = _context.MovieActor
                    .Where(ma => ma.MovieId == id)
                    .Select(ma => ma.Actor)
                    .ToList()

            };

            return View(movieDetailsViewModel);
        }

        // GET: Movies/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Movies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Year,Genre,IMDBlink,Media")] Movie movie, IFormFile? Media)
        {

            if (ModelState.IsValid)
            {
                if (Media != null && Media.Length > 0)
                {
                    using var memoryStream = new MemoryStream(); // Dispose() for garbage collection 
                    await Media.CopyToAsync(memoryStream);
                    movie.Media = memoryStream.ToArray();
                    memoryStream.Dispose();
                }
                else
                {
                    var defaultImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "poster.jpg");
                    movie.Media = await System.IO.File.ReadAllBytesAsync(defaultImagePath);

                }
                _context.Add(movie);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        // GET: Movies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie.FindAsync(id);
            if (movie == null)
            {
                return NotFound();
            }
            return View(movie);
        }

        // POST: Movies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Year,Genre,IMDBlink,Media")] Movie movie, IFormFile? Media)
        {
            if (id != movie.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingMovie = await _context.Movie.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
                    if (existingMovie == null)
                    {
                        return NotFound();
                    }

                    // Retain existing media if no new file is uploaded
                    if (Media != null && Media.Length > 0)
                    {
                        using var memoryStream = new MemoryStream();
                        await Media.CopyToAsync(memoryStream);
                        movie.Media = memoryStream.ToArray();
                        memoryStream.Dispose();
                    }
                    else
                    {
                        movie.Media = existingMovie.Media;
                    }

                    _context.Update(movie);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MovieExists(movie.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }


        // GET: Movies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        // POST: Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movie.FindAsync(id);
            if (movie != null)
            {
                foreach (Review review in movie.Reviews)
                {
                    _context.Review.Remove(review);
                }
                _context.Movie.Remove(movie);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MovieExists(int id)
        {
            return _context.Movie.Any(e => e.Id == id);
        }

        public async Task<IActionResult> GetMovieMedia(int id)
        {
            var movie = await _context.Movie.FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }
            if (movie.Media == null)
            {
                return NotFound();
            }
            var imageData = movie.Media;

            return File(imageData, "image/jpg");
        }
        [HttpGet]
        public async Task<IActionResult> GenerateReview(int id)
        {
            var movie = await _context.Movie.FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }
            var content = await CallGenerateReviewApi(movie);
            // sentiment analysis
            SentimentIntensityAnalyzer analyzer = new SentimentIntensityAnalyzer();
            var score = analyzer.PolarityScores(content);
            Review review = new Review()
            {
                Content = content,
                SentimentScore = JsonConvert.SerializeObject(score),
                MovieId = id,
                Movie = movie
            };
            _context.Review.Add(review);
            movie.Reviews.Add(review);

            await UpdateSumSentiment(id);
            // Save changes to the database
            _context.Update(movie);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = movie.Id });
        }
        private async Task<string> CallGenerateReviewApi(Movie movie)
        {
            var api_key = new System.ClientModel.ApiKeyCredential(_config["AI_API_KEY"] ?? throw new Exception("AI_API_KEY does not exist in the current Configuration"));
            var api_endpoint = new Uri(_config["AI_API_ENDPOINT"] ?? throw new Exception("AI_API_ENDPOINT does not exist in the current Configuration"));
            AzureOpenAIClient client = new(api_endpoint, api_key);
            ChatClient chat = client.GetChatClient("gpt-35-turbo");

            List<string> sentimentList = ["harsh", "very literal", "easy to please", "critical of everything", "more into the books", "always referencing the Fast and the Furious franchise"];
            List<string> genreList = ["sci-fi", "fantasy", "documentaries", "sports", "old movies", "new movies"];
            Random random = new Random();

            // Select a random item from sentimentList
            string sentiment = sentimentList[random.Next(sentimentList.Count)];
            // Select a random item from genreList
            string favGenre = genreList[random.Next(genreList.Count)];

            ChatCompletion completion = await chat.CompleteChatAsync($"Give a twitter api response in json format {{\r\n  \"text\": \"\",\r\n  \"created_at\": \"Date Time Year\",\r\n  \"user\": {{\r\n    \"name\": \"\",\r\n    \"screen_name\": \"\",\r\n    \"followers_count\": int,\r\n    \"verified\": bool\r\n  }},\r\n  \"retweet_count\": int,\r\n  \"favorite_count\": int,\r\n  \"hashtags\": [],\r\n }}." +
                $"The tweet should be an honest review of {movie.Title} from {movie.Year.ToString()} by a movie reviewer who is {sentiment} and likes {favGenre}." +
                $" They do not have to directly mention their tastes, but you can let it affect their review. Keep the review less than four sentences.");

            Debug.WriteLine(completion.Content[0].Text);
            return completion.Content[0].Text;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Review.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie.FirstOrDefaultAsync(m => m.Id == review.MovieId); ;
            try
            {
                _context.Review.Remove(review);
                await UpdateSumSentiment(movie.Id);
                _context.Update(movie);
            }
            catch
            {
                return NotFound();
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = movie.Id });
        }

        public async Task<int> UpdateSumSentiment(int id) {

            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Id == id);
            SentimentIntensityAnalyzer analyzer = new SentimentIntensityAnalyzer();

            if (movie.Reviews.Count > 0)
            {
                var all_reviews = "";
                foreach (Review r in movie.Reviews)
                {
                    all_reviews += r.Content;
                }
                movie.SentimentSum = JsonConvert.SerializeObject(analyzer.PolarityScores(all_reviews));
            }
            else
            {
                movie.SentimentSum = JsonConvert.SerializeObject(new VaderSharp2.SentimentAnalysisResults());

            }
            return id;
        }
    }
}
