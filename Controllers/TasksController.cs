using CleanArchitecture.Application.Repositories;
using CleanArchitecture.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.Security.Claims;
using Task = CleanArchitecture.Domain.Models.Task;

[ApiController]
[Route("[controller]")]
[Authorize(AuthenticationSchemes = "Identity.Bearer")]
public class TasksController : ControllerBase
{
    private readonly ITasksRepository _tasksRepository;
    private readonly UserManager<User> _userManager;

    public TasksController(ITasksRepository tasksRepository, UserManager<User> userManager)
    {
        _tasksRepository = tasksRepository;
        _userManager = userManager;
    }

    // GET: api/tasks
    [HttpGet]
    public async Task<IActionResult> GetAllTasks()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var tasks = await _tasksRepository.GetAllTasks(userId);
        return Ok(tasks);
    }

    // GET: api/tasks/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTaskById(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var task = await _tasksRepository.GetTaskById(id);
        if (task == null || task.UserId != userId)
        {
            return NotFound();
        }

        return Ok(task);
    }

    // POST: api/tasks
    [HttpPost]
    public async Task<IActionResult> AddTask([FromBody] Task task)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        task.UserId = userId;

        await _tasksRepository.AddTask(task);
        return CreatedAtAction(nameof(GetTaskById), new { id = task.Id }, task);
    }

    // DELETE: api/tasks/{id}
    [HttpDelete("{id}")]
    public IActionResult RemoveTask(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var task = _tasksRepository.GetTaskById(id).Result;
        if (task == null || task.UserId != userId)
        {
            return NotFound();
        }

        _tasksRepository.RemoveTask(id);
        return NoContent();
    }
}
