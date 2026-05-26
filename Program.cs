using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication4.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 МБ
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=greetings.db"));
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 3;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();    
}

app.UseStaticFiles();
app.UseRouting();              

app.UseAuthentication();       
app.UseAuthorization();        

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();        


using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string adminRole = "Admin";
    if (!roleManager.RoleExistsAsync(adminRole).Result)
    {
        roleManager.CreateAsync(new IdentityRole(adminRole)).Wait();
    }

    string adminEmail = "admin@admin.com";
    var adminUser = userManager.FindByEmailAsync(adminEmail).Result;
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            ProfilePictureUrl = "/uploads/avatars/default.png"
        };
        var result = userManager.CreateAsync(adminUser, "Admin123!").Result;
        if (result.Succeeded)
        {
            userManager.AddToRoleAsync(adminUser, adminRole).Wait();
        }
    }
    else
    {
        if (!userManager.IsInRoleAsync(adminUser, adminRole).Result)
        {
            userManager.AddToRoleAsync(adminUser, adminRole).Wait();
        }
    }
}

app.Use(async (context, next) =>
{
    try { await next(); }
    catch (Microsoft.AspNetCore.Connections.ConnectionResetException) { }
});

app.Run();

