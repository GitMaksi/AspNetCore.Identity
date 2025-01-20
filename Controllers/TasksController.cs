using CleanArchitecture.Application.Repositories;
using CleanArchitecture.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Task = CleanArchitecture.Domain.Models.Task;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Newtonsoft.Json;

[ApiController]
[Route("[controller]")]
[Authorize(AuthenticationSchemes = "Identity.Bearer")]
public class TasksController : ControllerBase
{
    private readonly ITasksRepository _tasksRepository;
    private readonly IDistributedCache _distributedCache;
    private readonly UserManager<User> _userManager;

    public TasksController(ITasksRepository tasksRepository, UserManager<User> userManager, IDistributedCache distributedCache)
    {
        _tasksRepository = tasksRepository;
        _userManager = userManager;
        _distributedCache = distributedCache;
    }

    // GET: api/tasks
    [HttpGet]
    public async Task<IActionResult> GetAllTasks()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        string cacheKey = "task";

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var tasks = await _tasksRepository.GetAllTasks(userId);

        var task = new Task()
        {
            Id = "69",
            Name = "Task",
            Description = "Cached Task"
        };

        memoryCache.Set(cacheKey, task, TimeSpan.FromMinutes(5));

        var taskCached = memoryCache.Get(cacheKey);
        tasks.Add((Task)taskCached);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };

        var task2 = new Task()
        {
            Id = "670",
            Name = "Task",
            Description = "Cached Task 2"
        };

        var serializedTask2 = JsonConvert.SerializeObject(task2);

        var cacheKey2 = "Task2";
        await _distributedCache.SetStringAsync(cacheKey2, serializedTask2, options);
        var cachedValue = await _distributedCache.GetAsync(cacheKey2);

        var task2Cached = await _distributedCache.GetAsync(cacheKey2);
        var taskString = System.Text.Encoding.UTF8.GetString(task2Cached);

        var deserialized = JsonConvert.DeserializeObject<Task>(taskString);

        tasks.Add(deserialized);

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
