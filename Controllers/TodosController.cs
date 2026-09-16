using Microsoft.AspNetCore.Mvc;
using Poc.Api.Models;
using Poc.Api.Services;

namespace Poc.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TodosController(ITodoStore store, ILogger<TodosController> logger) : ControllerBase
{
    /// <summary>Lists every todo, incomplete ones first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TodoItem>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<TodoItem>> GetAll()
    {
        return Ok(store.GetAll());
    }

    /// <summary>Gets a single todo by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TodoItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TodoItem> GetById(int id)
    {
        var item = store.GetById(id);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Creates a todo.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TodoItem), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<TodoItem> Create(CreateTodoRequest request)
    {
        var created = store.Add(request.Title.Trim());
        logger.LogInformation("Created todo {TodoId}", created.Id);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Replaces a todo's title and completion state.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TodoItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TodoItem> Update(int id, UpdateTodoRequest request)
    {
        var updated = store.Update(id, request.Title.Trim(), request.IsComplete);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>Deletes a todo.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(int id)
    {
        return store.Delete(id) ? NoContent() : NotFound();
    }
}
