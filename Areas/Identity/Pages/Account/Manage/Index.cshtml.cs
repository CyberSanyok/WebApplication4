using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using WebApplication4.Models;

namespace WebApplication4.Areas.Identity.Pages.Account.Manage
{
    [RequestFormLimits(MultipartBodyLengthLimit = 2 * 1024 * 1024)]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _environment;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _environment = environment;
        }

        public string Username { get; set; }
        public string ProfilePictureUrl { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;
            ProfilePictureUrl = user.ProfilePictureUrl;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            System.Diagnostics.Debug.WriteLine(">>> OnPostAsync ВЫЗВАН");
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            System.Diagnostics.Debug.WriteLine($"Метод: {Request.Method}, ContentType: {Request.ContentType}");
            System.Diagnostics.Debug.WriteLine($"Файлов в форме: {Request.Form.Files.Count}");
            foreach (var f in Request.Form.Files)
            {
                System.Diagnostics.Debug.WriteLine($"  Файл: {f.FileName}, размер: {f.Length}");
            }
            IFormFile profilePicture = null;
            if (Request.Form.Files.Count > 0)
            {
                profilePicture = Request.Form.Files["ProfilePicture"];
                System.Diagnostics.Debug.WriteLine($"Файл получен вручную: {profilePicture?.FileName}, размер: {profilePicture?.Length}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Файлов в запросе нет.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // Обновление телефона
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            // Загрузка аватарки
            if (profilePicture != null)
            {
                try
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                    System.Diagnostics.Debug.WriteLine($"Папка сохранения: {uploadsFolder}");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(profilePicture.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    System.Diagnostics.Debug.WriteLine($"Сохраняем в: {filePath}");

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await profilePicture.CopyToAsync(fileStream);
                    }
                    System.Diagnostics.Debug.WriteLine("Файл успешно записан.");

                    // Удаление старого файла
                    if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                    {
                        var oldFilePath = Path.Combine(_environment.WebRootPath, user.ProfilePictureUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                            System.Diagnostics.Debug.WriteLine("Старый файл удалён.");
                        }
                    }

                    user.ProfilePictureUrl = "/uploads/avatars/" + uniqueFileName;
                    await _userManager.UpdateAsync(user);
                    System.Diagnostics.Debug.WriteLine("Аватарка обновлена в БД.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки: {ex}");
                    StatusMessage = "Ошибка при загрузке файла.";
                    return RedirectToPage();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostUploadAvatarAsync(IFormFile avatar)
        {
            if (avatar == null) return new JsonResult(new { success = false, error = "Файл не выбран." });

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return new JsonResult(new { success = false, error = "Пользователь не найден." });

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(avatar.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await avatar.CopyToAsync(fileStream);
                }

                // Удаляем старый файл (по желанию)
                if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                {
                    var oldFilePath = Path.Combine(_environment.WebRootPath, user.ProfilePictureUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                user.ProfilePictureUrl = "/uploads/avatars/" + uniqueFileName;
                await _userManager.UpdateAsync(user);

                return new JsonResult(new { success = true, url = user.ProfilePictureUrl });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }
    }
}