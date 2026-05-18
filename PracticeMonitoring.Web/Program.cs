using PracticeMonitoring.Web.Services;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]
                 ?? throw new InvalidOperationException("ApiSettings:BaseUrl is not configured.");

builder.Services.AddHttpClient<AuthApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddHttpClient<AdminApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddHttpClient<CatalogApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddHttpClient<DepartmentStaffApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddHttpClient<ChatApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddHttpClient<StudentApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddHttpClient<SupervisorApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddHttpClient<NotificationApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddScoped<AttestationSheetService>();
builder.Services.AddScoped<PracticeDiaryDocumentService>();
builder.Services.AddScoped<PracticeReportDocumentService>();
builder.Services.AddScoped<DocxPdfConversionService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.Use(async (context, next) =>
{
    var mustChangePassword = bool.TryParse(context.Session.GetString("MustChangePassword"), out var value) && value;
    var path = context.Request.Path.Value ?? string.Empty;

    if (mustChangePassword &&
        !path.StartsWith("/Account/ChangePassword", StringComparison.OrdinalIgnoreCase) &&
        !path.StartsWith("/Account/Logout", StringComparison.OrdinalIgnoreCase) &&
        !path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) &&
        !path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) &&
        !path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/Account/ChangePassword");
        return;
    }

    await next();
});

app.MapControllerRoute(
    name: "controller-index",
    pattern: "{controller:regex(^(Admin|Student|Supervisor|DepartmentStaff)$)}",
    defaults: new { action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
