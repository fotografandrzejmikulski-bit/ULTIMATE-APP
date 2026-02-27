# ULTIMATE-APP — System Wieloagentowy Cross-Platform

Aplikacja klasy korporacyjnej łącząca natywne aplikacje desktopowe (Windows) i mobilne (Android/iOS) z zaawansowanym systemem wieloagentowym (Multi-Agent System — MAS).

## Architektura

```
[MAUI App: Windows/Android/iOS]
         │  SignalR WebSocket / REST
         ▼
[MAS.Api — ASP.NET Core]
         │
         ▼
[IOrchestrator → InProcessMessageBus]
    │            │            │
    ▼            ▼            ▼
[Orchestrator] [Research] [Execution] [Monitoring]
    Agent       Agent       Agent       Agent
```

Szczegółowa dokumentacja architektury: [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)

## Struktura Projektu

| Projekt                 | Opis                                              |
|-------------------------|---------------------------------------------------|
| `MAS.Core`              | Interfejsy, abstrakcje, modele domeny             |
| `MAS.Infrastructure`    | MessageBus, AgentRegistry, Orchestrator, Runner   |
| `MAS.Api`               | Backend ASP.NET Core + SignalR + REST             |
| `MAS.App`               | Frontend .NET MAUI (Windows/Android/iOS)          |
| `MAS.Core.Tests`        | Testy jednostkowe domeny                          |
| `MAS.Infrastructure.Tests` | Testy jednostkowe infrastruktury              |

## Uruchomienie

### Backend API
```bash
cd src/MAS.Api
dotnet run
```
- REST API: `https://localhost:5001/api/`
- SignalR Hub: `wss://localhost:5001/hubs/agents`
- OpenAPI UI: `https://localhost:5001/openapi/v1.json`

### Testy
```bash
dotnet test tests/MAS.Core.Tests/MAS.Core.Tests.csproj
dotnet test tests/MAS.Infrastructure.Tests/MAS.Infrastructure.Tests.csproj
```

### Aplikacja MAUI (wymaga workloadu)
```bash
dotnet workload install maui
dotnet run --project src/MAS.App -f net10.0-windows10.0.19041.0
```

## Wzorce Projektowe

- **Mediator** — `IMessageBus` dla komunikacji między agentami
- **Observer** — subskrypcje zdarzeń na magistrali
- **Template Method** — `BaseAgent.ProcessMessageAsync`
- **Strategy** — `IOrchestrator` z wymienną strategią wyboru agenta
- **MVVM** — `DashboardViewModel` + MAUI data-binding
