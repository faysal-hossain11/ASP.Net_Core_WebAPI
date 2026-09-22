using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

// ======================================================
// 1. Top-Level Web Application Builder Setup
// ======================================================

var builder = WebApplication.CreateBuilder(args);

// DbContext en PostgreSQL Registraasje
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository en Services Registraasje
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// JWT Authentication Konfiguraasje
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"]
        };
    });

builder.Services.AddControllers();

// Swagger Konfiguraasje mei JWT Nimmen
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Task Management API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token in the format: Bearer {your token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// App Build
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Global Exception Handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Middlewares
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();


// ======================================================
// 2. DTOs
// ======================================================

public class CreateTaskDto
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 100 characters")]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateTaskDto
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 100 characters")]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
}

public class TaskResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}

public class RegisterDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}


// ======================================================
// 3. Entities & DbContext
// ======================================================

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public User? User { get; set; }
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<User> Users { get; set; }
}

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var response = new
        {
            StatusCode = context.Response.StatusCode,
            Message = "An internal server error occurred. Please try again later.",
            Detailed = exception.Message
        };

        return context.Response.WriteAsJsonAsync(response);
    }
}


// ======================================================
// 4. Controllers
// ======================================================

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TaskController : ControllerBase
{
    private readonly ITaskService _service;

    public TaskController(ITaskService service)
    {
        _service = service;
    }
    
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(userIdClaim!);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        int userId = GetCurrentUserId();
        var tasks = await _service.GetAllTaskAsync(userId);
        return Ok(tasks);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        int userId = GetCurrentUserId();
        var task = await _service.GetTaskByIdAsync(id, userId);
        if (task == null)
        {
            return NotFound($"Task with Id {id} not found or unauthorized");
        }

        return Ok(task);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
    {
        int userId = GetCurrentUserId();
        var createdTask = await _service.CreateTaskAsync(dto, userId);
        return CreatedAtAction(nameof(GetById), new { id = createdTask.Id }, createdTask);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskDto dto)
    {
        int userId = GetCurrentUserId();
        var updatedTask = await _service.UpdateTaskAsync(id, dto, userId);
        if (updatedTask == null)
        {
            return NotFound($"Task with id {id} not found or unauthorized");
        }

        return Ok(updatedTask);
    }

    [HttpPatch("{id}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        int userId = GetCurrentUserId();
        var updatedTask = await _service.ToggleTaskStatusAsync(id, userId);
        if (updatedTask == null)
        {
            return NotFound($"Task with id {id} not found or unauthorized");
        }

        return Ok(updatedTask);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        int userId = GetCurrentUserId();
        var isDelete = await _service.DeleteTaskAsync(id, userId);
        if (!isDelete)
        {
            return NotFound($"Task with id {id} not found or unauthorized");
        }

        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var user = await _authService.RegisterAsync(dto);
        if (user == null)
        {
            return BadRequest("Username is already taken.");
        }

        return Ok(new { Message = "User registered successfully" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var token = await _authService.LoginAsync(dto);
        if (token == null)
        {
            return Unauthorized("Invalid username or password");
        }

        return Ok(new { Token = token });
    }
}


// ======================================================
// 5. Services Layer
// ======================================================

public interface ITaskService
{
    Task<IEnumerable<TaskResponseDto>> GetAllTaskAsync(int userId);
    Task<TaskResponseDto?> GetTaskByIdAsync(int id, int userId);
    Task<TaskResponseDto> CreateTaskAsync(CreateTaskDto dto, int userId);
    Task<TaskResponseDto?> UpdateTaskAsync(int id, UpdateTaskDto dto, int userId);
    Task<TaskResponseDto?> ToggleTaskStatusAsync(int id, int userId);
    Task<bool> DeleteTaskAsync(int id, int userId);
}

public class TaskService : ITaskService
{
    private readonly ITaskRepository _repository;

    public TaskService(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TaskResponseDto>> GetAllTaskAsync(int userId)
    {
        var tasks = await _repository.GetAllByUserIdAsync(userId);
        return tasks.Select(t => new TaskResponseDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description ?? string.Empty,
            IsCompleted = t.IsCompleted
        });
    }

    public async Task<TaskResponseDto?> GetTaskByIdAsync(int id, int userId)
    {
        var task = await _repository.GetByIdAsync(id);
        if (task == null || task.UserId != userId) return null;

        return new TaskResponseDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description ?? string.Empty,
            IsCompleted = task.IsCompleted
        };
    }

    public async Task<TaskResponseDto> CreateTaskAsync(CreateTaskDto dto, int userId)
    {
        var taskItem = new TaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };

        await _repository.AddAsync(taskItem);

        return new TaskResponseDto
        {
            Id = taskItem.Id,
            Title = taskItem.Title,
            Description = taskItem.Description ?? string.Empty,
            IsCompleted = taskItem.IsCompleted,
        };
    }

    public async Task<TaskResponseDto?> UpdateTaskAsync(int id, UpdateTaskDto dto, int userId)
    {
        var task = await _repository.GetByIdAsync(id);
        if (task == null || task.UserId != userId) return null;

        task.Title = dto.Title;
        task.Description = dto.Description;
        task.IsCompleted = dto.IsCompleted;

        await _repository.UpdateAsync(task);

        return new TaskResponseDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description ?? string.Empty,
            IsCompleted = task.IsCompleted
        };
    }

    public async Task<TaskResponseDto?> ToggleTaskStatusAsync(int id, int userId)
    {
        var task = await _repository.GetByIdAsync(id);
        if (task == null || task.UserId != userId) return null;

        task.IsCompleted = !task.IsCompleted;
        await _repository.UpdateAsync(task);

        return new TaskResponseDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description ?? string.Empty,
            IsCompleted = task.IsCompleted
        };
    }

    public async Task<bool> DeleteTaskAsync(int id, int userId)
    {
        var task = await _repository.GetByIdAsync(id);
        if (task == null || task.UserId != userId) return false;

        await _repository.DeleteAsync(task);
        return true;
    }
}

public interface IAuthService
{
    Task<User?> RegisterAsync(RegisterDto dto);
    Task<string?> LoginAsync(LoginDto dto);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<User?> RegisterAsync(RegisterDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Username == dto.Username.ToLower()))
            return null;

        using var hmac = new HMACSHA512();

        var user = new User
        {
            Username = dto.Username.ToLower(),
            PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password)),
            PasswordSalt = hmac.Key
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<string?> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username.ToLower());
        if (user == null) return null;

        using var hmac = new HMACSHA512(user.PasswordSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password));

        for (int i = 0; i < computedHash.Length; i++)
        {
            if (computedHash[i] != user.PasswordHash[i]) return null;
        }

        return CreateToken(user);
    }

    private string CreateToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };

        var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is missing in appsettings.json");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = creds,
            Issuer = _config["Jwt:Issuer"],
            Audience = _config["Jwt:Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}


// ======================================================
// 6. Repository Layer
// ======================================================

public interface ITaskRepository
{
    Task<IEnumerable<TaskItem>> GetAllByUserIdAsync(int userId);
    Task<TaskItem?> GetByIdAsync(int id);
    Task AddAsync(TaskItem task);
    Task UpdateAsync(TaskItem task);
    Task DeleteAsync(TaskItem task);
}

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TaskItem>> GetAllByUserIdAsync(int userId)
    {
        return await _context.Tasks
            .Where(t => t.UserId == userId)
            .ToListAsync();
    }

    public async Task<TaskItem?> GetByIdAsync(int id)
    {
        return await _context.Tasks.FindAsync(id);
    }

    public async Task AddAsync(TaskItem task)
    {
        await _context.Tasks.AddAsync(task);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TaskItem task)
    {
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(TaskItem task)
    {
        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
    }
}