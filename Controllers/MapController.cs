using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication4.Models;

namespace WebApplication4.Controllers
{
    [Authorize]
    public class MapController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MapController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult GetMarkers()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole("Admin");

            var markers = _context.MapMarkers
                .Select(m => new
                {
                    m.Id,
                    m.Latitude,
                    m.Longitude,
                    m.Message,
                    m.CreatedAt,
                    Author = m.User.UserName,
                    AuthorPicture = m.User.ProfilePictureUrl,
                    Color = m.Color,
                    IsOwner = userId != null && m.UserId == userId,
                    IsAdmin = isAdmin
                })
                .OrderByDescending(m => m.CreatedAt)
                .ToList();

            return Json(markers);
        }

        [HttpGet]
        public IActionResult GetMarker(int id)
        {
            var marker = _context.MapMarkers.FirstOrDefault(m => m.Id == id);
            if (marker == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            bool isAdmin = User.IsInRole("Admin");
            if (marker.UserId != userId && !isAdmin) return Forbid();

            return Json(new { marker.Id, marker.Latitude, marker.Longitude, marker.Message, marker.Color });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMarker([FromBody] MarkerCreateModel model)
        {
            if (!ModelState.IsValid) return BadRequest("Неверные данные");
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var marker = new MapMarker
            {
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                Message = model.Message,
                Color = model.Color ?? "#3388ff",
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };
            _context.MapMarkers.Add(marker);
            await _context.SaveChangesAsync();
            return Json(new { success = true, marker.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMarker([FromBody] MarkerEditModel model)
        {
            if (!ModelState.IsValid) return BadRequest();

            var marker = await _context.MapMarkers.FindAsync(model.Id);
            if (marker == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            bool isAdmin = User.IsInRole("Admin");
            if (marker.UserId != userId && !isAdmin) return Forbid();

            marker.Message = model.Message;
            if (!string.IsNullOrWhiteSpace(model.Color))
                marker.Color = model.Color;

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMarker([FromBody] int id)
        {
            var marker = await _context.MapMarkers.FindAsync(id);
            if (marker == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            bool isAdmin = User.IsInRole("Admin");
            if (marker.UserId != userId && !isAdmin) return Forbid();

            _context.MapMarkers.Remove(marker);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [AllowAnonymous]
        public IActionResult GetComments(int markerId)
        {
            var comments = _context.Comments
                .Where(c => c.MapMarkerId == markerId)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.Text,
                    c.CreatedAt,
                    Author = c.User.UserName,
                    AuthorPicture = c.User.ProfilePictureUrl
                })
                .ToList();
            return Json(comments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment([FromBody] CommentCreateModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Неверные данные");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var marker = await _context.MapMarkers.FindAsync(model.MarkerId);
            if (marker == null) return NotFound("Метка не найдена");

            var comment = new Comment
            {
                Text = model.Text,
                MapMarkerId = model.MarkerId,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        public class CommentCreateModel
        {
            public int MarkerId { get; set; }
            public string Text { get; set; }
        }
    }

    public class MarkerCreateModel
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Message { get; set; }
        public string Color { get; set; }
    }

    public class MarkerEditModel
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public string Color { get; set; }
    }
}