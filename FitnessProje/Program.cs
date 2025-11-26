using FitnessProje.Data;
using FitnessProje.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Veritabaný Baðlantýsýný Ekle
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Identity (Üyelik) Ayarlarýný Ekle
// (Admin ve Üye rolleri olacaðý için AddRoles ekledik)
builder.Services.AddIdentity<UygulamaKullanýcý, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 3; // "sau" 3 karakter
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
})

    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 3. MVC (Controller ve View) Servislerini Ekle
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Hata Yönetimi ve HTTPS
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 4. Yetkilendirme Middleware'leri (Sýrasý Önemli!)
app.UseAuthentication(); // Kimlik Doðrulama (Kimsin?)
app.UseAuthorization();  // Yetkilendirme (Neye iznin var?)

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
// Veritabanýný Baþlangýç Verileriyle Doldur (Seed)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DbSeeder.SeedRolesAndAdminAsync(services);
}
app.Run();