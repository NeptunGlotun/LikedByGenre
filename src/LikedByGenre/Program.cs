using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using LikedByGenre.Data;
using LikedByGenre.Options;
using LikedByGenre.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SpotifyOptions>(builder.Configuration.GetSection(SpotifyOptions.SectionName));
var spotify = builder.Configuration.GetSection(SpotifyOptions.SectionName).Get<SpotifyOptions>() ?? new SpotifyOptions();

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<SpotifyApiClient>();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<LibraryImportService>();
builder.Services.AddScoped<GenreClassificationService>();
builder.Services.AddScoped<PlaylistExportService>();
builder.Services.AddSingleton<ISymmetricEncryptionService, AesGcmEncryptionService>();
builder.Services.AddSingleton<IIntegrityService, HmacIntegrityService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = "Spotify";
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "LikedByGenre.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    })
    .AddOAuth("Spotify", options =>
    {
        options.ClientId = spotify.ClientId;
        options.ClientSecret = spotify.ClientSecret;
        options.CallbackPath = "/signin-spotify";
        options.AuthorizationEndpoint = "https://accounts.spotify.com/authorize";
        options.TokenEndpoint = "https://accounts.spotify.com/api/token";
        options.UserInformationEndpoint = "https://api.spotify.com/v1/me";
        options.Scope.Add("user-library-read");
        options.Scope.Add("playlist-modify-private");
        options.Scope.Add("playlist-modify-public");
        options.Scope.Add("user-read-private");
        options.SaveTokens = true;
        options.UsePkce = true;
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "display_name");
        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                using var response = await context.Backchannel.SendAsync(request);
                response.EnsureSuccessStatusCode();
                using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                context.RunClaimActions(payload.RootElement);

                if (context.Identity != null && !context.Identity.HasClaim(c => c.Type == ClaimTypes.Role))
                {
                    context.Identity.AddClaim(new Claim(ClaimTypes.Role, "User"));
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireUser", policy => policy.RequireRole("User", "Admin"));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    GenreSeed.EnsureSeeded(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
