using MAS.Core.Abstractions;
using MAS.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace MAS.Api.Controllers;

/// <summary>
/// REST API kontroler do monitorowania stanu agentów.
/// GET /api/agents — zwraca listę agentów i ich statusy dla dashboardu.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AgentsController : ControllerBase
{
    private readonly IAgentRegistry _registry;

    public AgentsController(IAgentRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>
    /// Zwraca listę wszystkich agentów i ich aktualnych statusów.
    /// GET /api/agents
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IEnumerable<AgentInfoResponse>>(StatusCodes.Status200OK)]
    public IActionResult GetAgents()
    {
        var agents = _registry.GetAll()
            .Select(a => new AgentInfoResponse
            {
                Id = a.Id,
                Name = a.Name,
                Status = a.Status.ToString()
            });

        return Ok(agents);
    }

    /// <summary>
    /// Zwraca informacje o konkretnym agencie.
    /// GET /api/agents/{id}
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType<AgentInfoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetAgent(string id)
    {
        var agent = _registry.Find(id);
        if (agent is null) return NotFound();

        return Ok(new AgentInfoResponse
        {
            Id = agent.Id,
            Name = agent.Name,
            Status = agent.Status.ToString()
        });
    }
}
