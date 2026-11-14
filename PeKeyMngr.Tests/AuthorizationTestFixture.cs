using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PeKeyMngr.Data;

public sealed class AuthorizationTestFixture : IAsyncLifetime
{
    private SqliteConnection _connection = null!;

    public ApplicationDbContext DbContext { get; private set; } = null!;

    public UserManager<ApplicationUser> UserManager { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        DbContext = new ApplicationDbContext(options, null);

        await DbContext.Database.EnsureCreatedAsync();

        UserStore<ApplicationUser> store = new UserStore<ApplicationUser>(DbContext);

        UserManager = new UserManager<ApplicationUser>(
            store,
            null!,
            new PasswordHasher<ApplicationUser>(),
            new IUserValidator<ApplicationUser>[]
            {
                new UserValidator<ApplicationUser>()
            },
            new IPasswordValidator<ApplicationUser>[]
            {
                new PasswordValidator<ApplicationUser>()
            },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<UserManager<ApplicationUser>>.Instance
        );
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
