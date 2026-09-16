using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

// ১. আগে আসবে Top-Level Web Application Builder setup
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();

app.Run(); 

// CreateTaskDto class  
public class CreateTaskDto
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 100 characters")]
    public string Title { get; set; }
    public string Description { get; set; }
}

public class TaskResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public bool IsCompleted { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class TaskController : ControllerBase
{
    private static List<TaskResponseDto> taskList = new List<TaskResponseDto>
    {
        new TaskResponseDto {Id = 1, Title = "Task 1", Description = "Description for Task 1", IsCompleted = false},
        new TaskResponseDto {Id = 2, Title = "Task 2", Description = "Description for Task 2", IsCompleted = true},
        new TaskResponseDto {Id = 3, Title = "Task 3", Description = "Description for Task 3", IsCompleted = false},
        new TaskResponseDto {Id = 4, Title = "Task 4", Description = "Description for Task 4", IsCompleted = true},
    };

    // get all tasks
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(taskList);
    }


    // get task by id
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var task = taskList.FirstOrDefault((t) => t.Id == id);
        if (task == null)
        {
            return NotFound($"Task with Id {id} not found.");
        }
        return Ok(task);
    }


    // create a new task
    [HttpPost]
    public IActionResult Create([FromBody] CreateTaskDto dto)
    {
        var newTask = new TaskResponseDto {
            Id = taskList.Count + 1,
            Title = dto.Title,
            Description = dto.Description,
            IsCompleted = false
        };

        taskList.Add(newTask);
        return CreatedAtAction(nameof(GetById), new {id = newTask.Id}, newTask);
    }



    // delete a task
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var task = taskList.FirstOrDefault((t) => t.Id == id);
        if (task == null)
        {
            return NotFound($"task with Id {id} not found.");
        }

        taskList.Remove(task);
        return NoContent();
    }














}
