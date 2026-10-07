using Infrastructure.Services;
using Infrastructure.Data;
using Infrastructure.UnitOfWork;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Web.Components;
using Web.Components.Account;
using Web.Mappings;

var builder = WebApplication.CreateBuilder(args);

// IIS can use a different native-DLL probing path than Visual Studio. Select the
// published LLamaSharp CPU backend before any LLamaSharp native API is initialized.

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllers();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString,
    options => options.EnableRetryOnFailure().UseCompatibilityLevel(120)));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
    options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    options.Password.RequiredLength = 1;
    options.Password.RequiredUniqueChars = 0;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireDigit = false;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Configure Authorization
builder.Services.AddAuthorization(options =>
{
    Policies.AddPolicies(options); // Call the centralized policy class
});

builder.Services.AddTransient(typeof(IEmailSender), typeof(EmailSender));
builder.Services.Configure<MailSettingsModel>(builder.Configuration.GetSection("MailSettings"));
builder.Services.Configure<SmsProviderOptions>(builder.Configuration.GetSection(SmsProviderOptions.SectionName));
builder.Services.AddHttpClient<ISmsSender, HttpSmsSender>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add AutoMapper

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile(new MappingProfile());
});

builder.Services.AddRadzenComponents();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<StudentService>();
builder.Services.AddScoped<StudentWelcomeMessageService>();
builder.Services.AddSingleton<StudentAccountReportExcelExporter>();
builder.Services.AddScoped<AppStateService>();
builder.Services.AddSingleton<StudentParentChangeNotificationService>();

builder.Services.AddSingleton<WorkflowUpdateNotifier>();
builder.Services.AddSingleton<AuthorizationUpdateNotifier>();
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    // Rebuild role and role-claim data on every subsequent HTTP request. Active
    // Blazor circuits are refreshed by AuthorizationUpdateNotifier.
    options.ValidationInterval = TimeSpan.Zero;
});

builder.Services.AddLocalization();
builder.Services.AddDistributedMemoryCache();

var app = builder.Build();

var supportedCultures = new[]
{
    CreateApplicationCulture("ar-EG"),
    CreateApplicationCulture("en-GB")
};
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(supportedCultures[0]),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

// Start in Arabic (Egypt) unless the user has explicitly selected another language.
localizationOptions.RequestCultureProviders =
[
    new CookieRequestCultureProvider()
];

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseRequestLocalization(localizationOptions);

app.MapStaticAssets();

app.UseAntiforgery();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

app.Run();

static CultureInfo CreateApplicationCulture(string name)
{
    var culture = (CultureInfo)CultureInfo.GetCultureInfo(name).Clone();
    var numberFormat = culture.NumberFormat;

    // Keep the application's numbers unambiguous in every UI language.
    numberFormat.NativeDigits = CultureInfo.InvariantCulture.NumberFormat.NativeDigits;
    numberFormat.DigitSubstitution = DigitShapes.None;
    numberFormat.NumberDecimalSeparator = ".";
    numberFormat.NumberGroupSeparator = ",";
    numberFormat.CurrencyDecimalSeparator = ".";
    numberFormat.CurrencyGroupSeparator = ",";
    numberFormat.PercentDecimalSeparator = ".";
    numberFormat.PercentGroupSeparator = ",";

    return culture;
}
