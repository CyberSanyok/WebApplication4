using System.ComponentModel.DataAnnotations;
namespace WebApplication4.Models
{
    public class UserInputModel
    {
        [Required(ErrorMessage ="Имя обязательно для заполенния!")]
        [Display(Name ="Твоё имя:")]
        public string Name { get; set; }
    }
}
