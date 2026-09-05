using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StudentAccommodation.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// 1. تسجيل قاعدة البيانات محلياً للسرعة الصاروخية (0ms Latency)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("StudentAccommodationDb"));

// 2. ضبط مهلة الـ HttpClient إلى 150ms كحد أقصى لتفادي أي تعليق
builder.Services.AddHttpClient("WebAPI", client =>
{
    client.BaseAddress = new Uri("http://localhost:57489/");
    client.Timeout = TimeSpan.FromMilliseconds(150);
})
.ConfigurePrimaryHttpMessageHandler(() => new System.Net.Http.HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// تهيئة البيانات الأولية تلقائياً
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    ApplicationDbContextSeed.SeedSampleDataAsync(db).GetAwaiter().GetResult();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
