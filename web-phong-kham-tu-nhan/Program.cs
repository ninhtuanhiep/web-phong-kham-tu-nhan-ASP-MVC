using Microsoft.EntityFrameworkCore;
using web_phong_kham_tu_nhan.Data;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<web_phong_kham_tu_nhan.Services.Giao_diện.ISpecialtyService, web_phong_kham_tu_nhan.Services.Triển_khai.SpecialtyService>();
builder.Services.AddScoped<web_phong_kham_tu_nhan.Services.Giao_diện.IDoctorServices, web_phong_kham_tu_nhan.Services.Triển_khai.DoctorService>();
builder.Services.AddScoped<web_phong_kham_tu_nhan.Services.Giao_diện.IPatientService, web_phong_kham_tu_nhan.Services.Triển_khai.PatientService>();
builder.Services.AddScoped<web_phong_kham_tu_nhan.Services.Giao_diện.IAppointmentService, web_phong_kham_tu_nhan.Services.Triển_khai.AppointmentService>();
builder.Services.AddSession();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/DangNhap"; // Đường dẫn đến trang đăng nhập
        options.LogoutPath = "/Account/Dangxuat";// 
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.AccessDeniedPath = "/Account/AccessDenied"; // Đường dẫn đến trang từ chối truy cập
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // phải có dòng này
}

//app.MapStaticAssets();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();


app.UseAuthentication();
app.UseAuthorization();



app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
