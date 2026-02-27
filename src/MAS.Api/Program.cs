using MAS.Api.Agents;
using MAS.Api.Hubs;
using MAS.Core.Abstractions;
using MAS.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// ─── Kontrolery REST API ───────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ─── SignalR (WebSocket) dla real-time komunikacji z klientami ─────────────────
builder.Services.AddSignalR();

// ─── CORS dla aplikacji mobilnych i SPA ───────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ─── Infrastruktura MAS: MessageBus, AgentRegistry, Orchestrator, AgentRunner ─
builder.Services.AddMasInfrastructure();

// ─── Rejestracja konkretnych agentów ──────────────────────────────────────────
builder.Services.AddSingleton<OrchestratorAgent>();
builder.Services.AddSingleton<ResearchAgent>();
builder.Services.AddSingleton<ExecutionAgent>();
builder.Services.AddSingleton<MonitoringAgent>();

// Rejestracja agentów w IAgentRegistry podczas rozwiązywania kontenera
builder.Services.AddSingleton<IAgent>(sp =>
{
    var agent = sp.GetRequiredService<OrchestratorAgent>();
    sp.GetRequiredService<IAgentRegistry>().Register(agent);
    return agent;
});
builder.Services.AddSingleton<IAgent>(sp =>
{
    var agent = sp.GetRequiredService<ResearchAgent>();
    sp.GetRequiredService<IAgentRegistry>().Register(agent);
    return agent;
});
builder.Services.AddSingleton<IAgent>(sp =>
{
    var agent = sp.GetRequiredService<ExecutionAgent>();
    sp.GetRequiredService<IAgentRegistry>().Register(agent);
    return agent;
});
builder.Services.AddSingleton<IAgent>(sp =>
{
    var agent = sp.GetRequiredService<MonitoringAgent>();
    sp.GetRequiredService<IAgentRegistry>().Register(agent);
    return agent;
});

var app = builder.Build();

// Wymuś inicjalizację agentów (rejestracja w registry) przed uruchomieniem
_ = app.Services.GetServices<IAgent>();

// ─── Middleware pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();

// ─── SignalR Hub: /hubs/agents ─────────────────────────────────────────────────
app.MapHub<AgentHub>("/hubs/agents");

app.Run();
