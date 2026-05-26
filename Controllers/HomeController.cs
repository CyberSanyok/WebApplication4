using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication4.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace WebApplication4.Controllers
{
    [Authorize] 
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index(int page = 1, string search = "")
        {
            ViewData["IsAdmin"] = User.IsInRole("Admin");
            int pageSize = 30;
            var query = _context.Greetings
                .Include(g => g.User)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(g => g.Name.Contains(search));
            }

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var greetings = query
                .OrderByDescending(g => g.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["Search"] = search;   

            return View(greetings);
        }

        [HttpGet]
        public IActionResult Hello()
        {
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hello(UserInputModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            var greeting = new Greeting
            {
                Name = model.Name,
                CreatedAt = DateTime.Now,
                UserId = userId
            };
            _context.Greetings.Add(greeting);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { page = 1, search = Request.Query["search"] });
        }

        public IActionResult Edit(int? id)
        {
            if (id == null) return NotFound();
            var userId = _userManager.GetUserId(User);
            var greeting = _context.Greetings.FirstOrDefault(g => g.Id == id && (g.UserId == userId || User.IsInRole("Admin")));
            if (greeting == null) return NotFound();
            return View(greeting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Greeting greeting)
        {
            var userId = _userManager.GetUserId(User);
            var existing = _context.Greetings.FirstOrDefault(g => g.Id == id && (g.UserId == userId || User.IsInRole("Admin")));
            if (existing == null) return NotFound();
            existing.Name = greeting.Name;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(int? id)
        {
            if (id == null) return NotFound();
            var userId = _userManager.GetUserId(User);
            var greeting = _context.Greetings.FirstOrDefault(g => g.Id == id && (g.UserId == userId || User.IsInRole("Admin")));
            if (greeting == null) return NotFound();
            return View(greeting);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var greeting = _context.Greetings.FirstOrDefault(g => g.Id == id && (g.UserId == userId || User.IsInRole("Admin")));
            if (greeting != null)
            {
                _context.Greetings.Remove(greeting);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}