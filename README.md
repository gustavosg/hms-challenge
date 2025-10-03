# Hospital Management System (HMS)

## Visão Geral

O Hospital Management System é uma aplicação de microserviços desenvolvida em .NET 9 que oferece funcionalidades completas para gerenciamento hospitalar, incluindo gestão de pacientes, histórico médico, autenticação e integração com sistemas externos.

## Arquitetura

### Microserviços

#### 1. **API Gateway** (`API`)
- **Porta Desenvolvimento**: 5000
- **Porta Docker**: 8080
- **Responsabilidade**: Ponto de entrada único para todas as requisições
- **Funcionalidades**:
  - Roteamento de requisições para serviços apropriados
  - Autenticação e autorização centralizada
  - Comunicação via HTTP e RabbitMQ
  - Endpoints para testes e monitoramento

#### 2. **AuthService** (`AuthService.API`)
- **Porta Desenvolvimento**: 8080
- **Porta Docker**: 8080
- **Responsabilidade**: Autenticação e gestão de usuários
- **Funcionalidades**:
  - Login e registro de usuários
  - Geração e validação de tokens JWT
  - CRUD de usuários
  - Hash seguro de senhas
  - Cache com IMemoryCache

#### 3. **PatientsService** (`PatientsService.API`)
- **Porta Desenvolvimento**: 8081
- **Porta Docker**: 8080
- **Responsabilidade**: Gestão de pacientes
- **Funcionalidades**:
  - CRUD de pacientes
  - Busca e filtros avançados
  - Validação de dados
  - Integração com AuthService
  - Cache com IMemoryCache

#### 4. **MedicalHistoryService** (`MedicalHistoryService.API`)
- **Porta Desenvolvimento**: 8082
- **Porta Docker**: 8080
- **Responsabilidade**: Gestão de histórico médico
- **Funcionalidades**:
  - CRUD de históricos médicos
  - Gestão de diagnósticos, exames e prescrições
  - Comunicação via RabbitMQ
  - Cache com IMemoryCache

#### 5. **Shared** (`Shared`)
- **Responsabilidade**: Componentes compartilhados
- **Funcionalidades**:
  - DTOs e modelos comuns
  - Serviços de cache
  - Messaging (RabbitMQ)
  - Utilitários de autenticação

## Tecnologias Utilizadas

### Backend
- **.NET 9**: Framework principal
- **C# 13**: Linguagem de programação
- **ASP.NET Core**: Web API
- **Entity Framework Core**: ORM
- **PostgreSQL**: Banco de dados principal
- **IMemoryCache**: Cache distribuído
- **RabbitMQ**: Message broker
- **JWT**: Autenticação
- **FluentValidation**: Validação de dados
- **Refit**: Cliente HTTP tipado

### Ferramentas
- **Docker**: Containerização
- **Docker Compose**: Orquestração local
- **Swagger/OpenAPI**: Documentação da API

## Decisões Técnicas e Boas Práticas

### **Clean Code e SOLID Principles**

#### **1. Single Responsibility Principle (SRP)**
- **Serviços especializados**: Cada microserviço tem uma responsabilidade específica
- **Separação de camadas**: Controllers, Services, Repositories, DTOs separados
- **Mappers dedicados**: `IAddMapper`, `IEditMapper`, `IGetMapper` para cada operação

```csharp
// Exemplo: Mapper específico para operação de adição
public interface IAddMapper<TDTO, TEntity>
    where TEntity : class
    where TDTO : class
{
    TEntity ToEntity(TDTO request);
}
```

#### **2. Open/Closed Principle (OCP)**
- **Interfaces para extensibilidade**: `IMessageBrokerService`, `ICacheService`
- **Pattern Strategy**: Diferentes implementações de cache e messaging
- **Validators extensíveis**: FluentValidation permite extensão sem modificação

#### **3. Liskov Substitution Principle (LSP)**
- **Null Object Pattern**: `NullMessageBrokerService` substitui implementação real
- **Abstrações consistentes**: Todas as implementações respeitam os contratos das interfaces

#### **4. Interface Segregation Principle (ISP)**
- **Interfaces específicas**: `IAddMapper`, `IEditMapper`, `IGetMapper` ao invés de uma interface genérica
- **Contratos mínimos**: Cada interface expõe apenas métodos necessários

#### **5. Dependency Inversion Principle (DIP)**
- **Injeção de Dependência**: Todos os serviços dependem de abstrações
- **Primary Constructor Injection**: C# 13 syntax para injeção limpa

```csharp
public class MedicalHistoryService(
    IAddMapper<Request, MedicalHistory> addMapper,
    IUnitOfWork<ApplicationDbContext> unitOfWork,
    ICacheService cache) : IMedicalHistoryService
```

### **Padrões de Projeto Implementados**

#### **Repository Pattern + Unit of Work**
```csharp
// Unit of Work centraliza transações
await unitOfWork.Context.MedicalHistories.AddAsync(medicalHistory);
await unitOfWork.CommitAsync();
```

#### **Mapper Pattern**
- **Separação clara**: DTOs ↔ Entities mapeamento dedicado
- **Type Safety**: Mappers tipados evitam erros de runtime
- **Reutilização**: Mappers injetados e reutilizáveis

#### **Factory Pattern**
- **Service Factory**: `MessagingExtensions.AddRabbitMQMessaging()`
- **Connection Factory**: RabbitMQ connection management

#### **Null Object Pattern**
```csharp
// Fallback quando RabbitMQ não está disponível
public class NullMessageBrokerService : IMessageBrokerService
{
    public Task<GetMedicalHistoryResponse> RequestMedicalHistoryAsync(...)
    {
        return Task.FromResult(new GetMedicalHistoryResponse(..., false, null, "Service not available"));
    }
}
```

#### **Request-Reply Pattern (RabbitMQ)**
- **Async Communication**: Correlation IDs para associar requests/responses
- **Timeout Handling**: Requests com timeout configurável
- **Graceful Degradation**: Fallback para HTTP se messaging falhar

### **Validation e Data Integrity**

#### **FluentValidation**
```csharp
public class RequestValidator : AbstractValidator<Request>
{
    public RequestValidator()
    {
        RuleFor(x => x.Document)
            .NotEmpty().WithMessage("Document is required.")
            .Matches(@"^\d{11}$").WithMessage("Must be valid CPF with 11 digits.");
            
        RuleFor(x => x.Password)
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])")
            .WithMessage("Must contain lowercase, uppercase, number and special char.");
    }
}
```

#### **Soft Delete Pattern**
- **Preservação de dados**: `IsDeleted` ao invés de exclusão física
- **Queries filtradas**: Todos os queries filtram `IsDeleted = false`
- **Auditoria**: Campos `CreatedAt` e `UpdatedAt` em todas as entidades

### **Performance e Caching**

#### **Estratégia de Cache em Camadas**
```csharp
// Cache individual (15 min)
private const string MEDICAL_HISTORY_CACHE_KEY = "medical-history:{0}";

// Cache de listagem (5 min)
private const string MEDICAL_HISTORIES_FILTERED_CACHE_KEY = "medical-histories:filtered:{0}:page:{1}:{2}";

// Invalidação inteligente
private void InvalidateListCaches()
{
    // Invalida múltiplas páginas de cache relacionadas
}
```

#### **Query Optimization**
- **AsNoTracking()**: Para queries read-only
- **Projection**: Select apenas campos necessários
- **Lazy Loading**: Controlled includes para evitar N+1

#### **Bulk Operations**
```csharp
// ExecuteUpdateAsync para operações em lote
await unitOfWork.Context.MedicalHistories.Where(_ => _.Id == id)
    .ExecuteUpdateAsync(_ =>
        _.SetProperty(mh => mh.IsDeleted, true)
        .SetProperty(mh => mh.UpdatedAt, DateTime.UtcNow));
```

### **Error Handling e Resilience**

#### **Graceful Degradation**
- **Fallback services**: Sistema continua funcionando mesmo com falhas parciais
- **Circuit Breaker**: Refit com Polly para retry policies
- **Timeout Handling**: Timeouts configuráveis para operações async

#### **Exception Handling**
```csharp
catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
{
    return Results.NotFound("Resource not found");
}
catch (ApiException ex)
{
    return Results.Problem($"Service error: {ex.Message}", statusCode: 500);
}
```

### **Security Best Practices**

#### **JWT Authentication**
- **Secret Management**: Secrets em configuração, não hardcoded
- **Token Expiration**: Tokens com tempo de vida limitado
- **Bearer Token Propagation**: Headers propagados entre serviços

#### **Password Security**
- **Password Hashing**: Senhas sempre hasheadas, nunca em plain text
- **Password Validation**: Regras complexas de senha obrigatórias
- **Sensitive Data**: Passwords marcadas com `[JsonIgnore]`

### **API Design**

#### **RESTful Design**
- **Resource-based URLs**: `/api/patients/{id}/with-medical-history`
- **HTTP Verbs**: GET, POST, PUT, DELETE semanticamente corretos
- **Status Codes**: 200, 201, 404, 409, 500 apropriados

#### **Pagination**
```csharp
// Pagination consistente em todos os endpoints de listagem
public async Task<PaginationResponse<T>> GetAsync(Request request, int page, int pageSize)
{
    // Implementação com total items e total pages
}
```

#### **Versioning Ready**
- **Namespace structure**: Preparado para versionamento futuro
- **DTO Versioning**: DTOs organizados por versão (`Add.Request`, `Edit.Request`)

### **Microservices Communication**

#### **Dual Communication Strategy**
1. **HTTP (Synchronous)**: Para operações CRUD diretas
2. **RabbitMQ (Asynchronous)**: Para operações que requerem agregação de dados

#### **Service Discovery**
- **Configuration-based**: URLs de serviços em appsettings
- **Environment-aware**: Diferentes configs para dev/prod

### **Monitoring e Observability**

#### **Health Checks**
```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString)
    .AddCheck("self", () => HealthCheckResult.Healthy());
```

#### **Structured Logging**
- **Log Levels**: Information para fluxo normal, Warning/Error para problemas
- **Contextual Logging**: IDs de correlação em logs RabbitMQ

### **Development Experience**

#### **Modern C# Features**
- **Primary Constructors**: Syntax limpa para DI
- **Records**: DTOs imutáveis e performance otimizada
- **Pattern Matching**: Switch expressions para queries condicionais
- **Null Safety**: Nullable reference types habilitado

#### **Configuration Management**
- **Environment-specific**: `appsettings.Development.json` vs `appsettings.json`
- **Secrets Management**: Configurações sensíveis separadas
- **Docker-aware**: Configurações diferentes para containerização

## Estrutura do Projeto

```
HMS/
├── API/                           # API Gateway
│   └── src/API/
├── AuthService/                   # Serviço de Autenticação
│   └── src/AuthService.API/
├── PatientsService/               # Serviço de Pacientes
│   └── src/PatientsService.API/
├── MedicalHistoryService/         # Serviço de Histórico Médico
│   └── src/MedicalHistoryService.API/
├── Shared/                        # Componentes compartilhados
├── docker-compose.yml             # Docker Compose
```

## Configuração e Execução

### Pré-requisitos
- .NET 9 SDK
- Docker e Docker Compose
- PostgreSQL (se executar localmente)
- RabbitMQ (se executar localmente)

### Executar com Docker Compose

```bash
# Executar todos os serviços
docker-compose up -d --build

# Parar serviços
docker-compose down
```

### Executar Localmente

```bash
# Executar cada serviço individualmente
dotnet run --project API/src/API                                    # Porta 5000
dotnet run --project AuthService/src/AuthService.API                # Porta 5002
dotnet run --project PatientsService/src/PatientsService.API        # Porta 5102
dotnet run --project MedicalHistoryService/src/MedicalHistoryService.API  # Porta 5202
```

## Endpoints Principais

### API Gateway - Desenvolvimento (http://localhost:5000)
### API Gateway - Docker (http://localhost:5000)

#### Autenticação
- `POST /api/auth/login` - Login de usuário
- `POST /api/auth/register` - Registro de usuário

#### Usuários
- `GET /api/users` - Listar usuários
- `GET /api/users/{id}` - Buscar usuário por ID
- `POST /api/users` - Criar usuário
- `PUT /api/users/{id}` - Atualizar usuário
- `DELETE /api/users/{id}` - Deletar usuário

#### Pacientes
- `GET /api/patients` - Listar pacientes
- `GET /api/patients/{id}` - Buscar paciente por ID
- `POST /api/patients` - Criar paciente
- `PUT /api/patients/{id}` - Atualizar paciente
- `DELETE /api/patients/{id}` - Deletar paciente
- `GET /api/patients/{id}/with-medical-history` - Paciente com histórico (HTTP)
- `GET /api/patients/{id}/with-medical-history-rabbitmq` - Paciente com histórico (RabbitMQ)

#### Histórico Médico
- `GET /api/medical-histories` - Listar históricos
- `GET /api/medical-histories/{id}` - Buscar histórico por ID
- `GET /api/medical-histories/patient/{patientId}` - Histórico por paciente
- `POST /api/medical-histories` - Criar histórico
- `PUT /api/medical-histories/{id}` - Atualizar histórico
- `DELETE /api/medical-histories/{id}` - Deletar histórico

#### Exames Externos
- `GET /api/external-exams` - Listar exames externos
- `GET /api/external-exams/{examId}` - Buscar exame por ID

#### Testes (Sem Autenticação)
- `GET /api/test/health` - Health check
- `GET /api/test/rabbitmq-test/{patientId}` - Teste RabbitMQ

## Autenticação

### JWT Token
Todos os endpoints (exceto testes e autenticação) requerem autenticação via JWT token.

```bash
# Login (Docker)
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "password123"
  }'

# Usar token (Docker)
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  http://localhost:8080/api/patients
```

## Comunicação entre Serviços

### HTTP (Refit)
- **API Gateway** → **AuthService**: CRUD de usuários, autenticação
- **API Gateway** → **PatientsService**: CRUD de pacientes
- **API Gateway** → **MedicalHistoryService**: CRUD de histórico médico

### RabbitMQ (Message Broker)
- **API Gateway** → **MedicalHistoryService**: Busca de histórico médico
- **PatientsService** → **MedicalHistoryService**: Criação automática de histórico

#### Filas RabbitMQ
- `medical-history-requests`: Requisições de histórico médico
- `medical-history-responses`: Respostas de histórico médico
- `patient.created`: Notificação de paciente criado

## Base de Dados

### Estrutura de Tabelas

#### AuthService
```sql
Users
├── Id (uuid, PK)
├── Username (varchar)
├── Email (varchar)
├── PasswordHash (varchar)
├── PhoneNumber (varchar)
├── BirthDate (date)
├── IsDeleted (boolean)
├── CreatedAt (timestamp)
└── UpdatedAt (timestamp)
```

#### PatientsService
```sql
Patients
├── Id (uuid, PK)
├── UserId (uuid, FK)
├── Name (varchar)
├── BirthDate (date)
├── Document (varchar)
├── Contact (varchar)
├── Email (varchar)
├── PhoneNumber (varchar)
├── IsDeleted (boolean)
├── CreatedAt (timestamp)
└── UpdatedAt (timestamp)
```

#### MedicalHistoryService
```sql
MedicalHistories
├── Id (uuid, PK)
├── PatientId (uuid)
├── Document (varchar)
├── Notes (text)
├── IsDeleted (boolean)
├── CreatedAt (timestamp)
└── UpdatedAt (timestamp)

Diagnoses
├── Id (uuid, PK)
├── MedicalHistoryId (uuid, FK)
├── Description (varchar)
├── Date (timestamp)
├── IsDeleted (boolean)
├── CreatedAt (timestamp)
└── UpdatedAt (timestamp)

Exams
├── Id (uuid, PK)
├── MedicalHistoryId (uuid, FK)
├── Type (varchar)
├── Date (timestamp)
├── Result (text)
├── IsDeleted (boolean)
├── CreatedAt (timestamp)
└── UpdatedAt (timestamp)

Prescriptions
├── Id (uuid, PK)
├── MedicalHistoryId (uuid, FK)
├── Medication (varchar)
├── Dosage (varchar)
├── Date (timestamp)
├── IsDeleted (boolean)
├── CreatedAt (timestamp)
└── UpdatedAt (timestamp)
```

## Cache (IMemoryCache)

### Estratégias de Cache
- **Get operations**: Cache por 15 minutos
- **List operations**: Cache por 5 minutos
- **Invalidação**: Cache invalidado em operações de escrita

### Chaves de Cache
```
user:{id}
users:filtered:{request}:page:{page}:{pageSize}
patient:{id}
patients:filtered:{request}:page:{page}:{pageSize}
medical-history:{id}
medical-histories:filtered:{request}:page:{page}:{pageSize}
patient-medical-history:{patientId}
```

## Configuração de Ambiente

### Desenvolvimento Local
```json
{
  "HttpSettings": {
    "Url": "http://+:5000"
  },
  "Services": {
    "AuthUserService": {
      "BaseUrl": "http://localhost:8080"
    },
    "PatientService": {
      "BaseUrl": "http://localhost:8081"
    },
    "MedicalHistoryService": {
      "BaseUrl": "http://localhost:8082"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=main_db;Username=admin;Password=admin123;Port=5433"
  },
  "MessageBroker": {
    "RabbitMQ": "amqp://admin:admin123@localhost:5672"
  },
  "Jwt": {
    "Secret": "your-secret-key",
    "Issuer": "hms-auth",
    "Audience": "hms-api",
    "ExpirationHours": 24
  }
}
```

### Docker/Produção
```json
{
  "HttpSettings": {
    "Url": "http://+:8080"
  },
  "Services": {
    "AuthUserService": {
      "BaseUrl": "http://auth_service:8080"
    },
    "PatientService": {
      "BaseUrl": "http://patients_service:8080"
    },
    "MedicalHistoryService": {
      "BaseUrl": "http://medical_history_service:8080"
    }
  },
  "MessageBroker": {
    "RabbitMQ": "amqp://admin:admin123@rabbitmq:5672"
  }
}
```

## Monitoramento e Saúde

### Health Checks
- `GET /health` - Disponível em todos os serviços
- Verifica conectividade com banco de dados
- Verifica status geral do serviço

### Logs
- Logs estruturados com níveis apropriados
- Informações de debug para RabbitMQ
- Rastreamento de erros e exceções

## Desenvolvimento

### Padrões Utilizados
- **Repository Pattern**: Para acesso a dados
- **Unit of Work**: Para transações
- **Mapper Pattern**: Para conversão de DTOs
- **Factory Pattern**: Para criação de serviços
- **Null Object Pattern**: Para fallback de serviços

### Boas Práticas
- Validação de entrada com FluentValidation
- Tratamento de exceções centralizado
- Separação de responsabilidades
- Dependency Injection
- Configuration pattern
- Async/await para operações I/O

## Troubleshooting

### Problemas Comuns

#### RabbitMQ não conecta
```bash
# Verificar se RabbitMQ está rodando
docker ps | grep rabbitmq

# Testar endpoint de teste (Desenvolvimento)
curl http://localhost:5000/api/test/rabbitmq-test/123e4567-e89b-12d3-a456-426614174000

# Testar endpoint de teste (Docker)
curl http://localhost:8080/api/test/rabbitmq-test/123e4567-e89b-12d3-a456-426614174000
```

#### Banco de dados não conecta
```bash
# Verificar se PostgreSQL está rodando
docker ps | grep postgres

# Verificar logs do serviço
docker-compose logs medical_history_service
```

#### Cache não funciona
```bash
# Verificar se o cache está funcionando
# IMemoryCache é interno ao processo, não há serviço externo para verificar
```

## Contribuição

### Desenvolvimento Local
1. Clone o repositório
2. Configure as variáveis de ambiente
3. Execute `dotnet restore` em cada projeto
4. Configure banco de dados e dependências
5. Execute `dotnet run` ou use Docker Compose

## Resumo de Portas

### **Desenvolvimento Local:**
- **API Gateway**: `http://localhost:5000` ← **Use esta no Postman**
- **AuthService**: `http://localhost:8080`
- **PatientsService**: `http://localhost:8081`
- **MedicalHistoryService**: `http://localhost:8082`

### **Docker:**
- **API Gateway**: `http://localhost:8080`
- **Outros serviços**: Portas internas dos containers

