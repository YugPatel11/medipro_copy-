using MediPro.Core.Configuration;
using MediPro.Core.Interfaces;
using MediPro.Dashboard.Data;
using MediPro.Print.Configuration;
using MediPro.Print.Services;
using MediPro.Quota.Configuration;
using MediPro.Quota.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Configure EF Core SQLite
builder.Services.AddDbContext<MediProDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=medipro.db"));

// Bind configurations
builder.Services.Configure<PrinterPoolConfig>(builder.Configuration.GetSection(PrinterPoolConfig.SectionName));
builder.Services.Configure<FirebaseConfig>(builder.Configuration.GetSection(FirebaseConfig.SectionName));
builder.Services.Configure<EmailConfig>(builder.Configuration.GetSection(EmailConfig.SectionName));
builder.Services.Configure<WhatsAppConfig>(builder.Configuration.GetSection(WhatsAppConfig.SectionName));

// Register domain services for the API controllers
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IPrinterStatusProvider, WmiPrinterStatusProvider>();
builder.Services.AddSingleton<IQuotaService, FirebaseQuotaService>();
builder.Services.AddSingleton<IEmailService, MediPro.Dashboard.Services.SmtpEmailService>();
builder.Services.AddSingleton<IWhatsAppService, MediPro.Dashboard.Services.WhatsAppCloudService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();
app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

// Auto-migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MediProDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
