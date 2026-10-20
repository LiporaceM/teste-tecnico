# Gestor de Tarefas

Aplicação web para gerenciamento de tarefas, desenvolvida como desafio técnico para Analista de Desenvolvimento.

## 1. Visão geral

Permite cadastrar, listar, editar, excluir e acompanhar o status de tarefas, com autenticação e controle de acesso.

- **Listagem em tabela** com paginação, busca por texto, filtro por prioridade e filtro por status (cards de resumo clicáveis).
- **Controle de status**: Pendente, Em andamento e Concluída, com troca rápida direto na lista.
- **Autenticação JWT** com dois perfis: `User` (vê apenas as próprias tarefas) e `Admin` (vê e gerencia todas).
- **Prazo e prioridade**: tarefas vencidas e não concluídas são destacadas em vermelho.
- **Swagger/OpenAPI** disponível em `/swagger`.

## 2. Tecnologias

| Camada | Tecnologia |
|---|---|
| Backend | .NET 9, ASP.NET Core Web API (controllers), EF Core 9, ASP.NET Core Identity, FluentValidation, Swashbuckle |
| Banco | PostgreSQL 16 |
| Frontend | React 18, TypeScript, Vite, Ant Design 5, React Router, Axios |
| Testes | xUnit + EF Core InMemory |
| Infra | Docker e Docker Compose (db + api + web/nginx) |

## 3. Como executar

### Com Docker (recomendado)

Pré-requisito: Docker com Compose.

```bash
docker compose up --build
```

| Serviço | URL |
|---|---|
| Aplicação (frontend) | http://localhost:3000 |
| API + Swagger | http://localhost:5000/swagger |
| PostgreSQL | localhost:5432 (usuário/senha `postgres`) |

Na primeira execução o banco é criado e populado com dados de exemplo. Para alterar senhas e a chave JWT, copie `.env.example` para `.env`.

**Contas de demonstração**

| Perfil | E-mail | Senha |
|---|---|---|
| Admin | admin@empresa.com | Admin@123 |
| Usuário | usuario@empresa.com | User@123 |

Também é possível criar uma conta na tela de login (perfil `User`).

Para recomeçar do zero: `docker compose down -v`.

### Sem Docker (desenvolvimento)

Pré-requisitos: .NET SDK 9, Node 20+ e um PostgreSQL local.

```bash
# 1) Banco (ajuste a connection string em backend/src/TaskManager.Api/appsettings.json, se necessário)
docker compose up -d db

# 2) API  -> http://localhost:5000
cd backend/src/TaskManager.Api
dotnet run --urls http://localhost:5000

# 3) Frontend -> http://localhost:5173
cd frontend
npm install
npm run dev
```

O Vite encaminha `/api` para `http://localhost:5000` (configurável com `VITE_API_URL`).

### Testes automatizados

```bash
cd backend
dotnet test
```

Cobrem o `TaskService`: criação, validações, isolamento entre usuários, perfil Admin, filtros, paginação, mudança de status, exclusão e resumo.

## 4. API

Autenticação: `Authorization: Bearer <token>`. No Swagger, use **Authorize**.

| Método | Rota | Descrição |
|---|---|---|
| POST | `/api/auth/register` | Cria conta (perfil User) e retorna token |
| POST | `/api/auth/login` | Autentica e retorna token |
| GET | `/api/tasks?search=&status=&priority=&page=&pageSize=` | Lista paginada com filtros |
| GET | `/api/tasks/summary` | Totais por status |
| GET | `/api/tasks/{id}` | Detalhe |
| POST | `/api/tasks` | Cria |
| PUT | `/api/tasks/{id}` | Atualiza |
| PATCH | `/api/tasks/{id}/status` | Altera só o status |
| DELETE | `/api/tasks/{id}` | Remove |

Valores de `status`: `Pending`, `InProgress`, `Completed`. Valores de `priority`: `Low`, `Medium`, `High`.

Erros seguem o padrão `ProblemDetails` (RFC 7807): 400 (validação, com o campo `errors`), 401, 404 e 500.

## 5. Estrutura do código

```
backend/
  src/TaskManager.Api/
    Controllers/   endpoints finos (HTTP apenas)
    Services/      regras de negócio (TaskService, TokenService)
    Validators/    regras de validação (FluentValidation)
    Dtos/ Models/  contratos da API e entidades
    Data/          DbContext e seed
    Middleware/    tratamento global de erros
  tests/TaskManager.Tests/
frontend/src/
  pages/ components/ api.ts AuthContext.tsx types.ts
docker-compose.yml
```

## 6. Decisões técnicas

- **Camadas simples (Controller → Service → DbContext)**: o escopo não justifica Clean Architecture; o EF Core já funciona como repositório/unit of work, então não criei uma camada de repositórios só para encapsular o DbContext.
- **ASP.NET Core Identity + JWT**: sem container extra (diferente do Keycloak), roda junto da API e cobre registro, hash de senha e perfis.
- **Isolamento por usuário na camada de serviço**: o filtro por dono é aplicado em um único ponto (`Scoped`/`FindAsync`). Tarefa de outro usuário retorna 404, e não 403, para não revelar que ela existe.
- **Validação com FluentValidation + middleware global**: validações viram `ValidationException`, convertida em `ProblemDetails` 400 com os erros por campo. O frontend exibe a mensagem retornada.
- **Enums como texto** na API e no banco: legível no Swagger e no SQL.
- **`EnsureCreated` em vez de migrations**: para que `docker compose up` funcione sem passos extras, o schema é criado na subida (com retentativas, aguardando o Postgres). Em um projeto com evolução contínua de schema, o próximo passo é adotar migrations (`dotnet ef migrations add Initial` e trocar para `Database.MigrateAsync()`).
- **Nginx como proxy no container web**: o navegador fala só com `:3000` (`/api` é encaminhado para a API), evitando CORS no ambiente Docker.
- **Ant Design**: componentes prontos de tabela, formulário e modal para uma interface corporativa de forma rápida e acessível.
- **Token no `localStorage`**: simples para o escopo do desafio. Em produção, o ideal seria cookie `HttpOnly` com refresh token.

## 6.1 Como fiz, dificuldades e facilidades

> Revise esta seção com a sua própria experiência antes de entregar.

**Como fiz.** Defini escopo e stack, montei o backend (modelo, serviço, API, autenticação, Swagger, erros), depois o frontend em lista, depois Docker, testes e documentação.

**Dificuldades.**
- Garantir que o isolamento entre usuários ficasse em um ponto só, para não esquecer o filtro em algum endpoint.
- Mapeamento de claims do JWT no .NET 9 (desativei o mapeamento automático e usei nomes curtos como `sub` e `role`).
- Datas: tratar fuso horário entre o seletor de data do navegador e o `timestamptz` do Postgres (a API normaliza tudo para UTC).
- Timing de subida do Postgres no Compose (healthcheck + retentativas).

**Facilidades.**
- Identity entrega cadastro, hash e perfis prontos.
- EF Core + Npgsql tornaram persistência, filtros e paginação diretos.
- Ant Design acelerou a interface.
- Separar o serviço do controller deixou as regras testáveis sem subir a API.

## 7. Melhorias futuras

Migrations do EF Core, refresh token, ordenação por coluna, testes de integração da API e de componentes do frontend, deploy online e visão kanban.
