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

// 🔹 Tajný klíč pro JWT (nahraďte bezpečnější variantou)
var key = "tajny_klic_pro_jwt";

// 🔹 Nastavení databáze
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));

// 🔹 Přidání JWT autentizace
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

// 🔹 Aplikování migrací a inicializace testovacích dat
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();

    if (!dbContext.Users.Any())
    {
        dbContext.Users.AddRange(new List<User>
        {
            new User { Name = "Alice Johnson", Email = "alice@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") },
            new User { Name = "Bob Smith", Email = "bob@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") }
        });

        dbContext.SaveChanges();
    }
}

// 🔹 Middleware
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

// 🔹 Frontend – jednoduchý HTML soubor pro registraci/login
app.MapGet("/", async context =>
{
    await context.Response.WriteAsync(@"
    <!DOCTYPE html>
    <html lang='cs'>
    <head>
        <meta charset='UTF-8'>
        <title>Login & Registrace</title>
    </head>
    <body>
        <h2>Registrace</h2>
        <input id='regName' placeholder='Jméno'>
        <input id='regEmail' placeholder='Email'>
        <input id='regPassword' type='password' placeholder='Heslo'>
        <button onclick='register()'>Registrovat</button>

        <h2>Přihlášení</h2>
        <input id='logEmail' placeholder='Email'>
        <input id='logPassword' type='password' placeholder='Heslo'>
        <button onclick='login()'>Přihlásit</button>

        <script>
            async function register() {
                let response = await fetch('/api/auth/register', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        name: document.getElementById('regName').value,
                        email: document.getElementById('regEmail').value,
                        password: document.getElementById('regPassword').value
                    })
                });
                alert(await response.text());
            }

            async function login() {
                let response = await fetch('/api/auth/login', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        email: document.getElementById('logEmail').value,
                        password: document.getElementById('logPassword').value
                    })
                });
                let data = await response.json();
                alert('Token: ' + data.token);
            }
        </script>
    </body>
    </html>");
});

app.Run();

// 🔹 Databázový kontext
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public required DbSet<User> Users { get; set; } = default!;
}

// 🔹 Model uživatele
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // 🔥 Oprava názvu
}

// 🔹 Controller pro registraci a přihlášení
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
