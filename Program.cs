using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Nastavení JWT
var key = "tajny_klic_pro_jwt";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };
    });

// Nastavení databáze
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));

// Registrace služeb
builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Users API",
        Version = "v1",
        Description = "API pro registraci a přihlášení uživatelů s JWT",
        Contact = new OpenApiContact { Name = "Support", Email = "support@example.com" }
    });
});

var app = builder.Build();

// Inicializace databáze
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();

    if (!dbContext.Users.Any())
    {
        dbContext.Users.AddRange(new List<User>
        {
            new User { Name = "Alic26e Johnson", Email = "alice@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") },
            new User { Name = "Bob Smith", Email = "bob@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") }
        });

        dbContext.SaveChanges();
    }
}

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Mapování HTML stránky
app.MapGet("/", async context =>
{
    var htmlPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "index.html");
    if (File.Exists(htmlPath))
    {
        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(htmlPath);
    }
    else
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("Soubor nenalezen");
    }
});





app.Run();

// Databázový kontext
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public required DbSet<User> Users { get; set; } = default!;
}

// Model uživatele
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // 🔥 Oprava názvu
}

// Controller pro registraci a přihlášení
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly string _jwtKey = "SuperTajneHesloProJWTAutentizaci123";

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Registrace nového uživatele
    /// </summary>
    [HttpPost("register")]
    public IActionResult Register([FromBody] UserRegisterDto request)
    {
        if (_context.Users.Any(u => u.Email == request.Email))
        {
            return BadRequest("E-mail už existuje.");
        }

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = new User { Name = request.Name, Email = request.Email, PasswordHash = hashedPassword };

        _context.Users.Add(user);
        _context.SaveChanges();

        return Ok("Registrace úspěšná!");
    }

    /// <summary>
    /// Přihlášení uživatele
    /// </summary>
    [HttpPost("login")]
    public IActionResult Login([FromBody] UserLoginDto request)
    {
        var user = _context.Users.SingleOrDefault(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized("Špatné přihlašovací údaje.");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("id", user.Id.ToString()) }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return Ok(new { Token = tokenHandler.WriteToken(token) });
    }
}

// DTO modely pro přihlášení a registraci
public class UserRegisterDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserLoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Získá seznam všech registrovaných uživatelů
    /// </summary>
    [HttpGet]
    public IActionResult GetUsers()
    {
        var users = _context.Users.Select(u => new
        {
            u.Id,
            u.Name,
            u.Email
        }).ToList();

        return Ok(users);
    }
}
