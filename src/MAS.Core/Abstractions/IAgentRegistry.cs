using MAS.Core.Enums;

namespace MAS.Core.Abstractions;

/// <summary>
/// Rejestr wszystkich agentów dostępnych w systemie.
/// Umożliwia odkrywanie i zarządzanie agentami w środowisku rozproszonym.
/// </summary>
public interface IAgentRegistry
{
    /// <summary>Rejestruje agenta w systemie.</summary>
    void Register(IAgent agent);

    /// <summary>Wyrejestrowuje agenta z systemu.</summary>
    void Unregister(string agentId);

    /// <summary>Zwraca agenta o podanym ID lub null.</summary>
    IAgent? Find(string agentId);

    /// <summary>Zwraca wszystkich agentów w danym statusie.</summary>
    IReadOnlyList<IAgent> GetByStatus(AgentStatus status);

    /// <summary>Zwraca wszystkich zarejestrowanych agentów.</summary>
    IReadOnlyList<IAgent> GetAll();
}
