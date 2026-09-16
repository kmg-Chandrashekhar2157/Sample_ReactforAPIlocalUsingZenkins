using System.ComponentModel.DataAnnotations;

namespace Poc.Api.Models;

/// <summary>Payload for creating a todo.</summary>
public record CreateTodoRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;
}

/// <summary>Payload for updating a todo. Both fields are replaced.</summary>
public record UpdateTodoRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    public bool IsComplete { get; init; }
}
