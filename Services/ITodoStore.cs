using Poc.Api.Models;

namespace Poc.Api.Services;

public interface ITodoStore
{
    IReadOnlyList<TodoItem> GetAll();

    TodoItem? GetById(int id);

    TodoItem Add(string title);

    TodoItem? Update(int id, string title, bool isComplete);

    bool Delete(int id);
}
