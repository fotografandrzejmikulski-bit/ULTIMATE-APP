# Architektura Systemu ULTIMATE-APP
## Cross-Platform & Multi-Agent System (MAS)

---

## 1. Przegląd Architektury

ULTIMATE-APP to system klasy korporacyjnej łączący:

- **Aplikację cross-platform** (.NET MAUI — Windows, Android, iOS) jako warstwę prezentacji
- **Rój autonomicznych agentów AI** jako backend przetwarzania
- **Asynchroniczną komunikację** (SignalR WebSocket + REST API) między warstwami

```
┌─────────────────────────────────────────────────────────────────────┐
│                       WARSTWA PREZENTACJI                           │
│                                                                     │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────────────┐   │
│  │  Windows 11  │   │   Android    │   │         iOS          │   │
│  │  (Desktop)   │   │  (Mobile)    │   │       (Mobile)       │   │
│  └──────┬───────┘   └──────┬───────┘   └──────────┬───────────┘   │
│         └──────────────────┴──────────────────────┘               │
│                          .NET MAUI                                  │
│                     DashboardPage + MVVM                            │
└────────────────────────────┬────────────────────────────────────────┘
                             │ SignalR WebSocket / REST HTTP
┌────────────────────────────▼────────────────────────────────────────┐
│                     MAS.Api (ASP.NET Core)                          │
│                                                                     │
│  ┌─────────────────┐  ┌──────────────────┐  ┌───────────────────┐  │
│  │  AgentHub       │  │  TasksController │  │ AgentsController  │  │
│  │  (SignalR)      │  │  (REST POST)     │  │ (REST GET)        │  │
│  └────────┬────────┘  └────────┬─────────┘  └────────┬──────────┘  │
│           └───────────────────┴────────────────────┘              │
│                          IOrchestrator                              │
└────────────────────────────┬────────────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────────────┐
│                   MAS.Infrastructure                                 │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │                    InProcessMessageBus                        │  │
│  │             (System.Threading.Channels + Mediator)           │  │
│  └──┬───────────────┬────────────────┬───────────────┬──────────┘  │
│     │               │                │               │             │
│  ┌──▼──────────┐ ┌──▼──────────┐ ┌──▼──────────┐ ┌─▼──────────┐  │
│  │Orchestrator │ │  Research   │ │  Execution  │ │ Monitoring  │  │
│  │   Agent     │ │   Agent     │ │   Agent     │ │   Agent     │  │
│  │ (Mediator)  │ │  (LLM/AI)   │ │ (I/O ops)   │ │ (Metrics)   │  │
│  └─────────────┘ └─────────────┘ └─────────────┘ └────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 2. Struktura Projektów

```
ULTIMATE-APP/
├── src/
│   ├── MAS.Core/               # Domena — interfejsy i abstrakcje
│   │   ├── Abstractions/       # IAgent, IMessageBus, IAgentRegistry, IOrchestrator
│   │   ├── Agents/             # BaseAgent (abstract)
│   │   ├── Enums/              # AgentStatus, MessagePriority, TaskExecutionStatus
│   │   ├── Messages/           # AgentMessage, TaskMessage, TaskResultMessage
│   │   └── Orchestration/      # TaskAnalysis
│   │
│   ├── MAS.Infrastructure/     # Implementacja infrastruktury
│   │   ├── Agents/             # AgentRegistry, AgentRunner, DefaultOrchestrator
│   │   ├── Messaging/          # InProcessMessageBus
│   │   └── DependencyInjection/ # AddMasInfrastructure()
│   │
│   ├── MAS.Api/                # Backend HTTP (ASP.NET Core 10)
│   │   ├── Agents/             # OrchestratorAgent, ResearchAgent, ExecutionAgent, MonitoringAgent
│   │   ├── Controllers/        # TasksController, AgentsController
│   │   ├── Hubs/               # AgentHub (SignalR)
│   │   └── Models/             # API DTOs
│   │
│   └── MAS.App/                # Frontend cross-platform (.NET MAUI)
│       ├── Models/             # AppModels (AgentTaskModel, TaskResultModel)
│       ├── Services/           # IAgentService, AgentService (SignalR + HTTP)
│       ├── ViewModels/         # DashboardViewModel (MVVM)
│       └── Views/              # DashboardPage (XAML)
│
├── tests/
│   ├── MAS.Core.Tests/         # Testy BaseAgent, AgentMessage
│   └── MAS.Infrastructure.Tests/ # Testy AgentRegistry, InProcessMessageBus
│
└── docs/
    └── ARCHITECTURE.md         # Ten dokument
```

---

## 3. Wzorce Projektowe

| Wzorzec          | Zastosowanie                                         |
|------------------|------------------------------------------------------|
| **Mediator**     | `IMessageBus` — agenty komunikują się pośrednio      |
| **Observer**     | Subskrypcje zdarzeń na magistrali wiadomości         |
| **Template Method** | `BaseAgent.ProcessMessageAsync` — nadpisywana w podklasach |
| **Strategy**     | `IOrchestrator` — wymienne strategie wyboru agenta   |
| **MVVM**         | `DashboardViewModel` + MAUI data-binding             |
| **Command**      | `TaskMessage` / `TaskResultMessage` jako komendy     |
| **Repository**   | `IAgentRegistry` — dostęp do agentów                |

---

## 4. Przepływ Danych (Sequence Diagram)

```
Aplikacja MAUI         AgentHub (SignalR)       Orchestrator        Agent
     │                       │                       │                │
     │──SendTask(title,pl)───►│                       │                │
     │                       │──DispatchAsync(task)──►│                │
     │◄──TaskAccepted(id)─────│                       │                │
     │                       │                       │──EnqueueMsg──► │
     │                       │                       │                │──ProcessMessageAsync()
     │                       │                       │                │   (async, nieblokujące)
     │                       │                       │◄──PublishResult─│
     │                       │◄──result──────────────│                │
     │◄──ReceiveTaskResult────│                       │                │
```

---

## 5. Komunikacja Real-Time

### SignalR (WebSocket)
- **Endpoint**: `wss://<host>/hubs/agents`
- **Metody klienta**: `SendTask(title, payload)`
- **Zdarzenia serwera**:
  - `TaskAccepted(taskId)` — natychmiastowe potwierdzenie
  - `ReceiveTaskResult(taskId, status, result, durationMs)` — wynik asynchroniczny
  - `Connected(connectionId)` — potwierdzenie połączenia

### REST API
| Endpoint               | Metoda | Opis                                |
|------------------------|--------|-------------------------------------|
| `/api/tasks`           | POST   | Deleguj zadanie do systemu agentów  |
| `/api/agents`          | GET    | Lista agentów i ich statusów        |
| `/api/agents/{id}`     | GET    | Szczegóły konkretnego agenta        |

---

## 6. Agenty Systemu

### OrchestratorAgent (`orchestrator`)
- Centralny punkt delegowania zadań
- Wybiera wolnego agenta metodą round-robin
- Publikuje odpowiedzi na magistrali

### ResearchAgent (`research-agent`)
- Analiza danych i generowanie raportów
- Integracja z LLM (GPT-4, Claude, LLaMA przez llama.cpp)
- Asynchroniczne przetwarzanie bez blokowania UI

### ExecutionAgent (`execution-agent`)
- Operacje I/O: API calls, pliki, procesy
- Retry policy z Polly (do implementacji produkcyjnej)

### MonitoringAgent (`monitoring-agent`)
- Zbiera metryki systemowe
- Subskrybuje `AgentStatusChangedMessage`
- Generuje alerty przy statusie `Faulted`

---

## 7. Skalowalność — Ścieżka do Produkcji

Aktualna implementacja używa **in-process message bus**. Dla środowisk rozproszonych:

```
InProcessMessageBus → RabbitMQ / Azure Service Bus / Kafka
AgentRegistry       → Redis / etcd (service discovery)
DefaultOrchestrator → LangGraph / CrewAI orchestration
```

### Integracja LLM (przykład dla ResearchAgent):
```csharp
// Zamień SimulateResearchAsync() na wywołanie LLM:
var client = new OpenAIClient(apiKey);
var response = await client.GetChatCompletionsAsync(
    new ChatCompletionsOptions("gpt-4o", [
        new ChatRequestUserMessage(task.Payload)
    ]), cancellationToken);
return response.Value.Choices[0].Message.Content;
```

---

## 8. Wymagania Instalacyjne

### Backend (MAS.Api)
```bash
cd src/MAS.Api
dotnet run
# API dostępne pod: http://localhost:5049 (HTTP) lub https://localhost:7031 (HTTPS)
# SignalR Hub: ws://localhost:5049/hubs/agents
# OpenAPI: http://localhost:5049/openapi/v1.json
```

### Aplikacja MAUI
```bash
dotnet workload install maui
cd src/MAS.App
dotnet run -f net10.0-windows10.0.19041.0  # Windows
dotnet run -f net10.0-android               # Android
dotnet run -f net10.0-ios                   # iOS
```

### Testy
```bash
dotnet test tests/MAS.Core.Tests/MAS.Core.Tests.csproj
dotnet test tests/MAS.Infrastructure.Tests/MAS.Infrastructure.Tests.csproj
```
