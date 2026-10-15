using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebApplication4.Models;

namespace WebApplication4.ViewComponents
{
    public class ProfilePictureViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfilePictureViewComponent(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            string avatarUrl = "/uploads/avatars/default.png"; 
            string userName = "Гость";

            if (user != null)
            {
                userName = user.UserName;
                if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                    avatarUrl = user.ProfilePictureUrl;
            }

            return View(new { AvatarUrl = avatarUrl, UserName = userName });
        }
    }
}
