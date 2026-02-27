using MAS.Core.Abstractions;
using MAS.Infrastructure.Agents;
using MAS.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace MAS.Infrastructure.DependencyInjection;

/// <summary>
/// Rozszerzenia kontenera DI rejestrujące wszystkie usługi infrastrukturalne MAS.
/// </summary>
public static class MasInfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Rejestruje podstawowe usługi systemu wieloagentowego:
    /// InProcessMessageBus, AgentRegistry, DefaultOrchestrator, AgentRunner.
    /// </summary>
    public static IServiceCollection AddMasInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IMessageBus, InProcessMessageBus>();
        services.AddSingleton<IAgentRegistry, AgentRegistry>();
        services.AddSingleton<IOrchestrator, DefaultOrchestrator>();
        services.AddHostedService<AgentRunner>();

        return services;
    }

    /// <summary>
    /// Rejestruje konkretnego agenta w kontenerze DI oraz rejestrze agentów.
    /// Agenty muszą być zarejestrowane PRZED wywołaniem AddMasInfrastructure().
    /// </summary>
    public static IServiceCollection AddAgent<TAgent>(this IServiceCollection services)
        where TAgent : class, IAgent
    {
        services.AddSingleton<TAgent>();
        services.AddSingleton<IAgent>(sp =>
        {
            var agent = sp.GetRequiredService<TAgent>();
            sp.GetRequiredService<IAgentRegistry>().Register(agent);
            return agent;
        });

        return services;
    }
}
