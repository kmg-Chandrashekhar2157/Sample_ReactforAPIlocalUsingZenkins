using Poc.Api.Models;

namespace Poc.Api.Services;

/// <summary>
/// Keeps todos in a plain List in memory — no database, no connection string, no EF Core.
/// Registered as a singleton so the list is shared by every request; it starts over from
/// the seeded items each time the app restarts.
///
/// Note: a plain List is not thread-safe. That is fine for a POC with one person clicking
/// around, but if two requests ever write at the same time you would need a lock or a
/// ConcurrentDictionary here.
/// </summary>
public class InMemoryTodoStore : ITodoStore
{
    private readonly List<TodoItem> _items = [];
    private int _nextId = 1;

    public InMemoryTodoStore()
    {
        // Seed data so the list isn't empty the first time you run it.
        Add("Wire the React frontend to the .NET API");
        Add("Read through Controllers/TodosController.cs");

        var done = Add("Install the .NET SDK12");
        done.IsComplete = true;
    }

    public IReadOnlyList<TodoItem> GetAll()
    {
        // Incomplete items first, newest first within each group.
        return _items
            .OrderBy(t => t.IsComplete)
            .ThenByDescending(t => t.CreatedAt)
            .ToList();
    }

    public TodoItem? GetById(int id)
    {
        return _items.FirstOrDefault(t => t.Id == id);
    }

    public TodoItem Add(string title)
    {
        var item = new TodoItem
        {
            Id = _nextId++,
            Title = title,
            IsComplete = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _items.Add(item);
        return item;
    }

    public TodoItem? Update(int id, string title, bool isComplete)
    {
        var item = GetById(id);
        if (item is null)
        {
            return null;
        }

        item.Title = title;
        item.IsComplete = isComplete;
        return item;
    }

    public bool Delete(int id)
    {
        var item = GetById(id);
        return item is not null && _items.Remove(item);
    }
}
