using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PeKeyMngr.Authorization;
using PeKeyMngr.Components;
using PeKeyMngr.Components.Account;
using PeKeyMngr.Data;

WebApplicationBuilder? builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ??
                      throw new InvalidOperationException("Connection string 'DefaultConnection' not found."));
});
builder.Services.AddQuickGridEntityFrameworkAdapter();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddIdentityCookies();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanAccessAdminPages", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new CanAccessAdminPages());
    });
    options.AddPolicy("CanAccessTeacherPages", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new CanAccessTeacherPages());
    });
    options.AddPolicy("CanAccessJanitorPages", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new CanAccessJanitorPages());
    });
    options.AddPolicy("CanAccessOperationsDirectorPages", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new CanAccessOperationsDirectorPages());
    });
});
builder.Services.AddScoped<IAuthorizationHandler, CanAccessAdminPagesHandler>();
builder.Services.AddScoped<IAuthorizationHandler, CanAccessTeacherPagesHandler>();
builder.Services.AddScoped<IAuthorizationHandler, CanAccessJanitorPagesHandler>();
builder.Services.AddScoped<IAuthorizationHandler, CanAccessOperationsDirectorPagesHandler>();

builder.Services.AddControllers();

builder.Services.AddScoped<KeyManagementService>();

WebApplication? app = builder.Build();

// Apply database migrations
using (IServiceScope? scope = app.Services.CreateScope())
{
    ApplicationDbContext? db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    await Seeder.SeedAsync(app.Services);
}
else
{
    await Seeder.SeedUsersAsync(app.Services);
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseMigrationsEndPoint();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapControllers();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
