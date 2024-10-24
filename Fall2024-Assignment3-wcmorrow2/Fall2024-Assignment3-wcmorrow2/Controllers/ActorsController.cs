﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Fall2024_Assignment3_wcmorrow2.Data;
using Fall2024_Assignment3_wcmorrow2.Models;
using System.Diagnostics;

namespace Fall2024_Assignment3_wcmorrow2.Controllers
{
    public class ActorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ActorsController(ApplicationDbContext context)
        {
            _context = context;
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
    }
}
