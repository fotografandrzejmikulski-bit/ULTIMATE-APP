using MAS.Core.Abstractions;
using MAS.Core.Messages;
using MAS.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace MAS.Api.Controllers;

/// <summary>
/// REST API kontroler do zarządzania zadaniami agentów.
/// Używany przez aplikacje mobilne i desktopowe jako alternatywa dla SignalR.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class TasksController : ControllerBase
{
    private readonly IOrchestrator _orchestrator;

    public TasksController(IOrchestrator orchestrator)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    /// <summary>
    /// Deleguje zadanie do wolnego agenta i zwraca wynik.
    /// POST /api/tasks
    /// </summary>
    [HttpPost]
    [ProducesResponseType<TaskResultResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DispatchTask(
        [FromBody] DispatchTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var task = new TaskMessage
        {
            SenderId = "api-client",
            Title = request.Title,
            Payload = request.Payload,
            Timeout = TimeSpan.FromSeconds(request.TimeoutSeconds),
            Context = new Dictionary<string, string>(request.Context, StringComparer.OrdinalIgnoreCase)
        };

        var result = await _orchestrator.DispatchAsync(task, cancellationToken).ConfigureAwait(false);

        return Ok(new TaskResultResponse
        {
            TaskId = result.TaskId,
            Status = result.Status.ToString(),
            Result = result.Result,
            DurationMs = result.Duration?.TotalMilliseconds
        });
    }
}
