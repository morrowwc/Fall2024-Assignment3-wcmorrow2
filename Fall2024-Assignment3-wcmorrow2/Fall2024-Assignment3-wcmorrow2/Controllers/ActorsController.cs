using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Fall2024_Assignment3_wcmorrow2.Data;
using Fall2024_Assignment3_wcmorrow2.Models;
using System.Diagnostics;
using Newtonsoft.Json;
using VaderSharp2;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace Fall2024_Assignment3_wcmorrow2.Controllers
{
    public class ActorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public ActorsController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;

        }

        // GET: Actors
        public async Task<IActionResult> Index()
        {
            return View(await _context.Actor.ToListAsync());
        }

        // GET: Actors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor
                .Include(m => m.Reviews) // Include related reviews
                .FirstOrDefaultAsync(a => a.Id == id);

            if (actor == null)
            {
                return NotFound();
            }

            var actorDetailsViewModel = new ActorDetailsViewModel
            {
                Value = actor,
                Movies = _context.MovieActor
                    .Where(ma => ma.ActorId == id)
                    .Select(ma => ma.Movie)
                    .ToList()
            };

            return View(actorDetailsViewModel);
        }



        // GET: Actors/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Actors/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Gender,DoB,DoD,IMDBlink,Media")] Actor actor, IFormFile? Media)
        {
            if (ModelState.IsValid)
            {
                if (Media != null && Media.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await Media.CopyToAsync(memoryStream);
                    actor.Media = memoryStream.ToArray();
                    memoryStream.Dispose();

                }
                else
                {
                    // Load the default image from a file or an embedded resource
                    var defaultImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "profile.jpg");
                    actor.Media = await System.IO.File.ReadAllBytesAsync(defaultImagePath);
                }
                _context.Add(actor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(actor);
        }
        
        // GET: Actors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor.FindAsync(id);
            if (actor == null)
            {
                return NotFound();
            }
            return View(actor);
        }

        // POST: Actors/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Gender,DoB,DoD,IMDBlink,Media")] Actor actor, IFormFile? Media)
        {
            if (id != actor.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingActor = await _context.Actor.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
                    if (existingActor == null)
                    {
                        return NotFound();
                    }
                    if (Media != null && Media.Length > 0)
                    {
                        using var memoryStream = new MemoryStream(); // Dispose() for garbage collection 
                        await Media.CopyToAsync(memoryStream);
                        actor.Media = memoryStream.ToArray();
                        memoryStream.Dispose();
                    }
                    else
                    {
                        actor.Media = existingActor.Media;
                    }
                    _context.Update(actor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ActorExists(actor.Id))
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
            return View(actor);
        }

        // GET: Actors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor
                .FirstOrDefaultAsync(m => m.Id == id);
            if (actor == null)
            {
                return NotFound();
            }

            return View(actor);
        }

        // POST: Actors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var actor = await _context.Actor.FindAsync(id);
            if (actor != null)
            {
                var mas = await _context.MovieActor
                    .Where(ma => ma.ActorId == id)
                    .ToListAsync();
                foreach (var ma in mas)
                {
                    _context.MovieActor.Remove(ma);
                }
                foreach (Review review in actor.Reviews)
                {
                    _context.Review.Remove(review);
                }
                _context.Actor.Remove(actor);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        private bool ActorExists(int id)
        {
            return _context.Actor.Any(e => e.Id == id);
        }
        public async Task<IActionResult> GetActorMedia(int id)
        {
            var actor = await _context.Actor.FirstOrDefaultAsync(m => m.Id == id);
            if (actor == null)
            {
                return NotFound();
            }
            if (actor.Media == null)
            {
                return NotFound();
            }
            var imageData = actor.Media;

            return File(imageData, "image/jpg");
        }

        [HttpGet]
        public async Task<IActionResult> GenerateReview(int id)
        {
            var actor = await _context.Actor.FirstOrDefaultAsync(a => a.Id == id);
            if (actor == null)
            {
                return NotFound();
            }
            var content = await CallGenerateReviewApi(actor);

            // Sentiment analysis
            SentimentIntensityAnalyzer analyzer = new SentimentIntensityAnalyzer();
            var score = analyzer.PolarityScores(content);

            Review review = new Review()
            {
                Content = content,
                SentimentScore = JsonConvert.SerializeObject(score),
                ContentId = id, // im too lazy to change the review id name right now
            };

            _context.Review.Add(review);
            actor.Reviews.Add(review);
            await UpdateSumSentiment(id);

            // Save changes to the database
            _context.Update(actor);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = actor.Id });
        }
        private async Task<string> CallGenerateReviewApi(Actor actor)
        {
            var api_key = new System.ClientModel.ApiKeyCredential(_config["AI_API_KEY"] ?? throw new Exception("AI_API_KEY does not exist in the current Configuration"));
            var api_endpoint = new Uri(_config["AI_API_ENDPOINT"] ?? throw new Exception("AI_API_ENDPOINT does not exist in the current Configuration"));
            AzureOpenAIClient client = new(api_endpoint, api_key);
            ChatClient chat = client.GetChatClient("gpt-35-turbo");

            List<string> sentimentList = ["harsh", "very literal", "easy to please", "critical of everything", "more into the books", "always referencing the Fast and the Furious franchise"];
            List<string> genreList = ["sci-fi", "fantasy", "documentaries", "sports", "old movies", "new movies"];
            Random random = new Random();
            string sentiment = sentimentList[random.Next(sentimentList.Count)];
            string favGenre = genreList[random.Next(genreList.Count)];

            ChatCompletion completion = await chat.CompleteChatAsync($"Give a twitter api response in json format Give a twitter api response in json format {{\r\n  \"text\": \"\",\r\n  \"created_at\": \"Date Time Year\",\r\n  \"user\": {{\r\n    \"name\": \"\",\r\n    \"screen_name\": \"\",\r\n    \"followers_count\": int,\r\n    \"verified\": bool\r\n  }},\r\n  \"retweet_count\": int,\r\n  \"favorite_count\": int,\r\n  \"hashtags\": [],\r\n }}." +
                $"The tweet should be an honest review of {actor.Name}, reflecting on their acting career, by a reviewer who is {sentiment} and likes {favGenre}." +
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
            var actor = await _context.Actor.FirstOrDefaultAsync(a => a.Id == review.ContentId);
            if (actor == null)
            {
                return NotFound();
            }
            try
            {
                _context.Review.Remove(review);
                await UpdateSumSentiment(actor.Id);
                _context.Update(actor);
            }
            catch
            {
                return NotFound();
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = actor.Id });
        }
        public async Task<int> UpdateSumSentiment(int id)
        {
            var actor = await _context.Actor
                .FirstOrDefaultAsync(a => a.Id == id);
            SentimentIntensityAnalyzer analyzer = new SentimentIntensityAnalyzer();
            if (actor.Reviews.Count > 0)
            {
                var all_reviews = "";
                foreach (Review r in actor.Reviews)
                {
                    all_reviews += r.Content;
                }
                actor.SentimentSum = JsonConvert.SerializeObject(analyzer.PolarityScores(all_reviews));
            }
            else
            {
                actor.SentimentSum = JsonConvert.SerializeObject(new VaderSharp2.SentimentAnalysisResults());
            }
            return id;
        }



    }
}
