
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;


// ======================================================
// 1. Top-Level Web Application Builder Setup
// ======================================================

var builder = WebApplication.CreateBuilder(args);

// DbContext এবং PostgreSQL Register
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository এবং Service Register
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ITaskService, TaskService>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthorization();

app.MapControllers();

app.Run();



// ======================================================
// 2. CreateTaskDto
// ======================================================
public class CreateTaskDto
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 100 characters")]
    public string Title { get; set; }
    public string Description { get; set; }
}

// ======================================================
// 2. CreateTaskDto
// ======================================================
public class UpdateTaskDto
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 100 characters")]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
}

// ======================================================
// 4. TaskResponseDto
// ======================================================
public class TaskResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public bool IsCompleted { get; set; }
}



// ======================================================
// 5. TaskItem Entity
// ======================================================
public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}



// ======================================================
// 6. AppDbContext
// ======================================================
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TaskItem> Tasks { get; set; }
}



// ======================================================
// 7. ITaskRepository Interface
// ======================================================

public interface ITaskRepository
{
    Task<IEnumerable<TaskItem>> GetAllAsync();

    Task<TaskItem?> GetByIdAsync(int id);

    Task AddAsync(TaskItem task);

    Task UpdateAsync(TaskItem task);

    Task DeleteAsync(TaskItem task);
}



[ApiController]
[Route("api/[controller]")]
public class TaskController : ControllerBase
{
    private readonly ITaskService _service;

    public TaskController(ITaskService service)
    {
        _service = service;
    }


    [HttpGet]
    public async Task<IActionResult> GerAll()
    {
        var tasks = await _service.GetAllTaskAsync();
        return Ok(tasks);
    }


    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var task = await _service.GetTaskByIdAsync(id);
        if(task == null)
        {
            return NotFound($"Task with Id {id} not found");
        }

        return Ok(task);
    }



}



public interface ITaskService
{
    Task<IEnumerable<TaskResponseDto>> GetAllTaskAsync();
    Task<TaskResponseDto?> GetTaskByIdAsync(int id);
    // Task<TaskResponseDto> CreateTaskAsync(CreateTaskDto dto);
    // Task<TaskResponseDto?> UpdateTaskAsync(int id, UpdateTaskDto dto);
    // Task<TaskResponseDto?> ToggleTaskStatusAsync(int it);
    // Task<bool> DeleteTaskAsync(int id);
}


// Task service
public class TaskService : ITaskService
{
    private readonly ITaskRepository _repository;

    public TaskService(ITaskRepository repository)
    {
        _repository = repository;
    }


    public async Task<IEnumerable<TaskResponseDto>> GetAllTaskAsync()
    {   
        var tasks = await _repository.GetAllAsync();
        return tasks.Select(t => new TaskResponseDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description ?? string.Empty,
            IsCompleted = t.IsCompleted
        });
    }


    public async Task<TaskResponseDto?> GetTaskByIdAsync(int id)
    {
        var task = await _repository.GetByIdAsync(id);
        if(task == null) return null;

        return new TaskResponseDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description ?? string.Empty,
            IsCompleted = task.IsCompleted
        };
    } 


}



public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TaskItem>> GetAllAsync()
    {
        return await _context.Tasks.ToListAsync();
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