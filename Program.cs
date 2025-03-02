using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Přidání služeb do kontejneru
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Konfigurace middlewares
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();

// DbContext
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
}

// Model
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// Controller pro API
[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;
    public UserController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult GetUsers()
    {
        return Ok(_context.Users.ToList());
    }

    [HttpPost]
    public IActionResult AddUser(User user)
    {
        _context.Users.Add(user);
        _context.SaveChanges();
        return Ok(user);
    }
}
