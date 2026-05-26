using Microsoft.AspNetCore.Identity;

namespace WebApplication4.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string ProfilePictureUrl { get; set; }
    }
}