# Gestor de Tarefas

Aplicação web full stack para gerenciamento de tarefas, com API REST em **.NET 9**, interface em **React + TypeScript**, persistência em **PostgreSQL**, autenticação com perfis de acesso e implantação via **Docker** e **Vercel**.

Desenvolvida como desafio técnico para a vaga de Analista de Desenvolvimento.

---

## Sumário

1. [Visão geral](#1-visão-geral)
2. [Funcionalidades](#2-funcionalidades)
3. [Arquitetura](#3-arquitetura)
4. [Tecnologias](#4-tecnologias)
5. [Execução local](#5-execução-local)
6. [Manual de uso](#6-manual-de-uso)
7. [Autenticação e perfis de acesso](#7-autenticação-e-perfis-de-acesso)
8. [Configuração do Firebase Authentication (opcional)](#8-configuração-do-firebase-authentication-opcional)
9. [Publicação (Render + Neon + Vercel)](#9-publicação-render--neon--vercel)
10. [Variáveis de ambiente](#10-variáveis-de-ambiente)
11. [Referência da API](#11-referência-da-api)
12. [Testes automatizados](#12-testes-automatizados)
13. [Estrutura do repositório](#13-estrutura-do-repositório)
14. [Decisões técnicas](#14-decisões-técnicas)
15. [Como foi feito: dificuldades e facilidades](#15-como-foi-feito-dificuldades-e-facilidades)
16. [Pendências do desenvolvedor](#16-pendências-do-desenvolvedor)
17. [Solução de problemas](#17-solução-de-problemas)
18. [Limitações conhecidas e evolução](#18-limitações-conhecidas-e-evolução)

---

## 1. Visão geral

O sistema permite que cada usuário cadastre, acompanhe e conclua suas tarefas, com controle de status, prioridade e prazo. Administradores têm visão consolidada das tarefas de todos os usuários.

O projeto atende aos requisitos do desafio (CRUD, controle de status, persistência, integração front/back, Swagger, tratamento de erros, documentação) e inclui os diferenciais: **Docker, TypeScript, autenticação e autorização, filtros/pesquisa, testes automatizados e publicação online**.

## 2. Funcionalidades

| Área | Descrição |
|---|---|
| CRUD de tarefas | Criar, listar, editar e excluir tarefas |
| Controle de status | Pendente, Em andamento e Concluída; troca rápida direto na lista |
| Listagem em tabela | Paginação no servidor, busca por título/descrição, filtro por prioridade e por status |
| Painel de resumo | Contadores por status que também funcionam como filtro |
| Prazo | Data de vencimento; tarefas vencidas e não concluídas são destacadas em vermelho |
| Autenticação | Cadastro e login; dois modos intercambiáveis (Local e Firebase) |
| Autorização | `User` vê apenas as próprias tarefas; `Admin` vê e gerencia todas |
| API documentada | Swagger/OpenAPI em `/swagger`, com suporte a token Bearer |
| Erros padronizados | Respostas `ProblemDetails` (RFC 7807) com mensagens por campo |

## 3. Arquitetura

```mermaid
flowchart LR
  U["Usuário (navegador)"] --> W["Frontend React"]
  W -->|"/api/*"| A["API .NET 9"]
  A --> D[("PostgreSQL")]
  W -. "modo Firebase" .-> F["Firebase Authentication"]
  A -. "valida ID token" .-> F
```

- O frontend chama sempre caminhos relativos (`/api/...`). Em Docker, o **nginx** encaminha para a API; na Vercel, um **rewrite** em `frontend/vercel.json` faz o mesmo. Isso elimina CORS.
- A API é stateless: toda a persistência está no PostgreSQL, o que permite executá-la em ambientes efêmeros (containers Docker).

## 4. Tecnologias

| Camada | Tecnologia |
|---|---|
| Backend | .NET 9, ASP.NET Core Web API, EF Core 9 (Npgsql), ASP.NET Core Identity, FluentValidation, Swashbuckle |
| Banco de dados | PostgreSQL 16 |
| Frontend | React 18, TypeScript, Vite, Ant Design 5, React Router, Axios, Firebase SDK |
| Autenticação | Identity + JWT (padrão) ou Firebase Authentication (opcional) |
| Testes | xUnit + EF Core InMemory |
| Infraestrutura | Docker, Docker Compose, nginx, Render, Neon, Vercel |

## 5. Execução local

### 5.1 Com Docker (recomendado)

**Pré-requisito:** Docker Desktop em execução (no Windows, com WSL 2).

```bash
docker compose up --build
```

| Serviço | URL |
|---|---|
| Aplicação | http://localhost:3000 |
| API e Swagger | http://localhost:5000/swagger |
| PostgreSQL | localhost:5432 (usuário e senha `postgres`) |

Na primeira execução, o banco é criado e populado com dados de demonstração.

**Contas de demonstração (modo Local)**

| Perfil | E-mail | Senha |
|---|---|---|
| Administrador | `admin@empresa.com` | `Admin@123` |
| Usuário | `usuario@empresa.com` | `User@123` |

Para reiniciar com o banco limpo: `docker compose down -v`.

Para personalizar senhas, chave JWT ou provedor de login, copie `.env.example` para `.env` e ajuste os valores.

### 5.2 Sem Docker (desenvolvimento)

**Pré-requisitos:** .NET SDK 9, Node.js 20+ e um PostgreSQL acessível.

```bash
# 1) Banco de dados (ou aponte a connection string para um PostgreSQL seu)
docker compose up -d db

# 2) API -> http://localhost:5000
cd backend/src/TaskManager.Api
dotnet run --urls http://localhost:5000

# 3) Frontend -> http://localhost:5173
cd frontend
npm install
npm run dev
```

O servidor de desenvolvimento do Vite encaminha `/api` para `http://localhost:5000` (configurável com `VITE_API_URL`).

## 6. Manual de uso

1. **Acesso.** Abra a aplicação e entre com uma conta de demonstração ou crie a sua em **Criar conta**.
2. **Visão geral.** Os quatro cartões no topo mostram o total de tarefas e a quantidade por status. Clique em um cartão para filtrar a lista; clique em **Todas** para limpar.
3. **Buscar e filtrar.** Use o campo de busca (título ou descrição, confirme com Enter) e o seletor de **Prioridade**.
4. **Criar tarefa.** Clique em **Nova tarefa**, preencha título (obrigatório), descrição, status, prioridade e vencimento, e salve.
5. **Alterar status.** Na coluna **Status**, escolha o novo valor no seletor. A alteração é salva imediatamente.
6. **Editar.** Clique no ícone de lápis na linha da tarefa.
7. **Excluir.** Clique no ícone de lixeira e confirme.
8. **Prazos.** Datas em vermelho indicam tarefa vencida e ainda não concluída.
9. **Administrador.** Além das próprias, vê as tarefas de todos os usuários, com a coluna **Responsável**.
10. **Sair.** Botão **Sair**, no cabeçalho.

## 7. Autenticação e perfis de acesso

O sistema suporta dois provedores, escolhidos por configuração. **O padrão é o modo Local**, que funciona sem nenhuma conta externa.

| | Modo Local (padrão) | Modo Firebase (opcional) |
|---|---|---|
| Cadastro e senha | ASP.NET Core Identity (hash de senha) | Firebase Authentication |
| Token | JWT emitido pela própria API | ID token emitido pelo Firebase |
| Validação na API | Chave simétrica (`Jwt:Key`) | Chaves públicas do Google (issuer `securetoken.google.com/<projectId>`) |
| Perfil Admin | Atribuído no banco (usuário `admin@empresa.com`) | E-mails listados em `Firebase:AdminEmails` |
| Dados de demonstração | Sim | Não |
| Quando usar | Avaliação, desenvolvimento, `docker compose up` | Ambiente publicado com login gerenciado |

**Como funciona o modo Firebase**

1. O navegador autentica no Firebase (e-mail e senha) e obtém um ID token, renovado automaticamente pelo SDK.
2. Cada requisição à API envia `Authorization: Bearer <ID token>`.
3. A API valida assinatura, emissor, audiência e validade. No primeiro acesso, cria o registro local do usuário (as tarefas referenciam essa tabela) e define o perfil: `Admin` se o e-mail estiver em `Firebase:AdminEmails`, senão `User`.
4. O frontend consulta `GET /api/auth/me` para conhecer o nome e o perfil.

> **Sobre a Vercel e autenticação.** A Vercel não possui um serviço de login de usuários final próprio equivalente ao Firebase. O que ela oferece são integrações de Marketplace (Clerk, Auth0, Descope) e o "Sign in with Vercel", voltado a contas Vercel. Por isso o projeto usa o Firebase, que funciona com qualquer hospedagem.

## 8. Configuração do Firebase Authentication (opcional)

Necessário apenas para usar o modo Firebase.

1. Acesse o [Console do Firebase](https://console.firebase.google.com) e crie um projeto.
2. Em **Authentication > Método de login**, habilite **E-mail/senha**.
3. Em **Configurações do projeto > Seus apps**, registre um app **Web** e copie `apiKey`, `authDomain`, `projectId` e `appId`.
4. Defina as variáveis (consulte a [seção 10](#10-variáveis-de-ambiente)):
   - **Frontend:** `VITE_AUTH_PROVIDER=firebase` e as quatro `VITE_FIREBASE_*`.
   - **API:** `Auth__Provider=Firebase`, `Firebase__ProjectId` e `Firebase__AdminEmails__0` (e-mail do administrador).
5. Em **Authentication > Configurações > Domínios autorizados**, adicione o domínio publicado (necessário caso passe a usar login social; para e-mail/senha é boa prática).
6. Para testar localmente com Docker, preencha o `.env` e execute `docker compose up --build`. As variáveis `VITE_*` são embutidas **no build**; após alterá-las, é necessário reconstruir a imagem.

> A `apiKey` do Firebase Web **não é um segredo**: ela identifica o projeto no navegador. A segurança vem da validação do token na API.

## 9. Publicação (Render + Neon + Vercel)

Topologia adotada, com cada peça na plataforma mais simples para ela:

| Peça | Plataforma | Como |
|---|---|---|
| Frontend | Vercel | Site estático Vite (pasta `frontend`) |
| API .NET | Render | Container Docker, a partir de `backend/Dockerfile` |
| Banco | Neon | PostgreSQL gerenciado |
| Login | Firebase Authentication | Ver [seção 8](#8-configuração-do-firebase-authentication-opcional) |

**Por que o banco não fica no Render?** O Postgres gratuito do Render é removido após um período (a documentação do Render indica 30 dias). Como o link pode ser aberto por um avaliador dias depois da entrega, o banco fica no Neon, cujo plano gratuito não tem essa expiração. **Confira os limites atuais de cada plano antes de publicar**, pois mudam com frequência.

### 9.1 Pré-requisito: Firebase

Conclua a [seção 8](#8-configuração-do-firebase-authentication-opcional) e anote `apiKey`, `authDomain`, `projectId`, `appId` e o e-mail que será Admin. Para publicar sem Firebase, use `Auth__Provider=Local` na API e `VITE_AUTH_PROVIDER=local` no frontend.

### 9.2 Banco de dados (Neon)

1. Crie uma conta em [neon.tech](https://neon.tech) e um projeto PostgreSQL.
2. Copie a **connection string** (formato `postgresql://usuario:senha@host/banco?sslmode=require`).
3. Guarde-a: será o valor de `DATABASE_URL`. As tabelas são criadas automaticamente na primeira inicialização da API.

### 9.3 API (Render, Docker)

**Opção A: Blueprint (recomendada).** O repositório contém `render.yaml`.

1. No Render, **New > Blueprint** e selecione o repositório.
2. Preencha as variáveis solicitadas: `DATABASE_URL`, `Firebase__ProjectId` e `Firebase__AdminEmails__0`. O `Jwt__Key` é gerado automaticamente.
3. Confirme e aguarde o build.

**Opção B: manual.** **New > Web Service**, conecte o repositório e configure:

| Campo | Valor |
|---|---|
| Runtime / Language | Docker |
| Dockerfile Path | `./backend/Dockerfile` |
| Docker Build Context | `./backend` |
| Health Check Path | `/health` |
| Plano | Free |

Variáveis de ambiente: as da [seção 10](#10-variáveis-de-ambiente). O Render define `PORT` automaticamente e a API já a utiliza.

**Verificação.** Abra `https://<seu-servico>.onrender.com/health` (deve responder `healthy`) e `/swagger`.

### 9.4 Frontend (Vercel)

1. Edite `frontend/vercel.json` e troque `https://SEU-SERVICO-API.onrender.com` pela URL real da API. Faça commit e push.
2. Na Vercel, **Add New > Project**, importe o repositório e defina **Root Directory** como `frontend` (o Vite é detectado automaticamente).
3. Variáveis: `VITE_AUTH_PROVIDER=firebase` e as quatro `VITE_FIREBASE_*`.
4. Faça o deploy.

O `vercel.json` do frontend contém dois rewrites: `/api/*` para a API no Render e um *fallback* para `index.html`, necessário às rotas do React Router.

### 9.5 Pós-publicação

1. No Firebase, em **Authentication > Configurações > Domínios autorizados**, adicione o domínio da Vercel.
2. Acesse o site, crie uma conta com o e-mail definido como Admin e valide: login, criação, edição, troca de status, exclusão e busca.
3. Registre as URLs publicadas neste README e no e-mail de entrega.

> **Atenção ao primeiro acesso.** No plano gratuito, o serviço do Render "dorme" após cerca de 15 minutos sem requisições e leva até aproximadamente um minuto para acordar. Antes de uma demonstração, abra `/health` para aquecê-lo.

### 9.6 Alternativas

- **API na própria Vercel (Container Runtime, Beta).** O repositório inclui `backend/Dockerfile.vercel` e `backend/vercel.json`: crie um projeto com Root Directory `backend`, defina `PORT=8080` e as demais variáveis, e aponte o rewrite do frontend para a URL gerada. Veja a [documentação](https://vercel.com/docs/functions/runtimes/container).
- **Outras plataformas com Docker** (Railway, Fly.io etc.): a API depende apenas de `PORT` e `DATABASE_URL`; use `backend/Dockerfile` e ajuste o rewrite do frontend.

## 10. Variáveis de ambiente

### API

| Variável | Padrão | Descrição |
|---|---|---|
| `ConnectionStrings__Default` | `localhost` (appsettings) | Connection string Npgsql |
| `DATABASE_URL` | vazio | Se definida, tem prioridade. Formato URI (Vercel, Neon, Supabase, Render) |
| `PORT` | vazio | Se definida, a API escuta nessa porta (Render, Vercel e similares a definem) |
| `Jwt__Key` | chave de desenvolvimento | Mínimo de 32 caracteres. **Troque em produção** |
| `Auth__Provider` | `Local` | `Local` ou `Firebase` |
| `Firebase__ProjectId` | vazio | Obrigatório no modo Firebase |
| `Firebase__AdminEmails__0` | vazio | E-mail com perfil Admin (use `__1`, `__2` para mais) |
| `SeedData__Enabled` | `true` | Cria usuários e tarefas de demonstração (somente modo Local) |
| `Cors__Origins__0` | localhost | Origem permitida, caso o frontend chame a API diretamente |

### Frontend (tempo de build)

| Variável | Descrição |
|---|---|
| `VITE_AUTH_PROVIDER` | `local` (padrão) ou `firebase` |
| `VITE_FIREBASE_API_KEY`, `VITE_FIREBASE_AUTH_DOMAIN`, `VITE_FIREBASE_PROJECT_ID`, `VITE_FIREBASE_APP_ID` | Configuração do app Web do Firebase |
| `VITE_API_URL` | Somente `npm run dev`: destino do proxy (padrão `http://localhost:5000`) |

### Docker Compose (`.env`)

`POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`, `JWT_KEY`, `SEED_DATA`, `AUTH_PROVIDER` (`local`/`firebase`), `FIREBASE_PROJECT_ID`, `FIREBASE_API_KEY`, `FIREBASE_AUTH_DOMAIN`, `FIREBASE_APP_ID`, `FIREBASE_ADMIN_EMAIL`. Veja `.env.example`.

## 11. Referência da API

Documentação interativa em `/swagger`. Autenticação: `Authorization: Bearer <token>`.

| Método | Rota | Descrição | Auth |
|---|---|---|---|
| POST | `/api/auth/register` | Cria conta (perfil User) e retorna token. Somente modo Local | Não |
| POST | `/api/auth/login` | Autentica e retorna token. Somente modo Local | Não |
| GET | `/api/auth/me` | Dados e perfil do usuário autenticado | Sim |
| GET | `/api/tasks?search=&status=&priority=&page=&pageSize=` | Lista paginada com filtros | Sim |
| GET | `/api/tasks/summary` | Totais por status | Sim |
| GET | `/api/tasks/{id}` | Detalhe | Sim |
| POST | `/api/tasks` | Cria | Sim |
| PUT | `/api/tasks/{id}` | Atualiza | Sim |
| PATCH | `/api/tasks/{id}/status` | Altera somente o status | Sim |
| DELETE | `/api/tasks/{id}` | Remove | Sim |
| GET | `/health` | Verificação de saúde | Não |

**Valores aceitos.** `status`: `Pending`, `InProgress`, `Completed`. `priority`: `Low`, `Medium`, `High`.

**Códigos de erro** (formato `ProblemDetails`): `400` validação (campo `errors` por propriedade), `401` não autenticado ou credenciais inválidas, `404` recurso inexistente ou de outro usuário, `500` erro interno.

**Exemplo**

```http
POST /api/tasks
Authorization: Bearer <token>
Content-Type: application/json

{
  "title": "Revisar contrato do fornecedor",
  "description": "Conferir cláusulas de reajuste.",
  "status": "Pending",
  "priority": "High",
  "dueDate": "2026-10-30T03:00:00.000Z"
}
```

## 12. Testes automatizados

```bash
cd backend
dotnet test
```

São 13 testes sobre o `TaskService`: criação, validações, isolamento entre usuários, perfil Admin, filtros, paginação, alteração de status, exclusão e resumo. Sem o SDK instalado, é possível executar via Docker:

```bash
docker run --rm -v "${PWD}/backend:/src" -w /src mcr.microsoft.com/dotnet/sdk:9.0 dotnet test
```

## 13. Estrutura do repositório

```
.
├── docker-compose.yml        # db + api + web
├── .env.example
├── backend/
│   ├── Dockerfile            # imagem para Docker/Compose
│   ├── Dockerfile.vercel     # imagem para a Vercel
│   ├── vercel.json           # projeto da API na Vercel
│   ├── src/TaskManager.Api/
│   │   ├── Controllers/      # endpoints (camada HTTP)
│   │   ├── Services/         # regras de negócio, tokens, provisionamento Firebase
│   │   ├── Validators/       # FluentValidation
│   │   ├── Dtos/ Models/     # contratos e entidades
│   │   ├── Data/             # DbContext, seed, connection string
│   │   └── Middleware/       # tratamento global de erros
│   └── tests/TaskManager.Tests/
└── frontend/
    ├── Dockerfile  nginx.conf
    ├── vercel.json           # projeto do frontend na Vercel
    └── src/
        ├── pages/ components/
        ├── api.ts            # cliente HTTP e interceptors
        ├── AuthContext.tsx   # sessão (Local ou Firebase)
        └── firebase.ts       # inicialização condicional do SDK
```

## 14. Decisões técnicas

- **Camadas simples (Controller, Service, DbContext).** O escopo não justifica Clean Architecture; o EF Core já funciona como repositório e unidade de trabalho.
- **Dois provedores de autenticação, com o Local como padrão.** Garante que o avaliador execute o projeto com um único comando, sem criar conta em serviço externo. O Firebase atende ao cenário publicado.
- **Perfil Admin no modo Firebase por lista de e-mails.** Alternativa mais simples que *custom claims* (que exigiriam o Firebase Admin SDK e um fluxo de administração). Em evolução futura, migrar para claims.
- **Isolamento por usuário no serviço.** O filtro por dono está em um único ponto; tarefa de outro usuário retorna 404 (não 403), para não revelar sua existência.
- **Validação com FluentValidation e middleware global.** Erros viram `ProblemDetails` consistentes, exibidos pelo frontend.
- **`EnsureCreated` em vez de migrations.** Permite subir o ambiente (inclusive um Postgres novo na Vercel) sem passos manuais. Para evolução de schema em produção, adotar migrations (`dotnet ef migrations add Initial` e `Database.MigrateAsync()`).
- **Enums como texto** na API e no banco, por legibilidade.
- **Caminhos relativos `/api` no frontend**, resolvidos por nginx (Docker) ou rewrite (Vercel), evitando CORS.
- **`DATABASE_URL` e `PORT`** suportados nativamente, para portabilidade entre plataformas.
- **Token no `localStorage` (modo Local).** Simples para o escopo. Em produção, preferir cookie `HttpOnly` com refresh token.

## 15. Como foi feito: dificuldades e facilidades

> **Revise esta seção com a sua experiência antes de entregar.** O texto abaixo é um ponto de partida e deve refletir o que você de fato vivenciou.

**Como foi feito.** Definição de escopo e stack; backend (modelo, serviço, API, autenticação, Swagger, erros); frontend em lista; Docker; testes; e, por fim, o modo Firebase e a publicação na Vercel.

**Dificuldades**
- Garantir o isolamento entre usuários em um único ponto, sem esquecer filtros em endpoints.
- Mapeamento de *claims* do JWT no .NET 9 (mapeamento automático desativado; nomes curtos `sub`, `role`).
- Datas: fuso horário entre o seletor do navegador e `timestamptz` do Postgres (a API normaliza para UTC).
- Ambiente: Docker Desktop/WSL 2 e dependências de build (pacote de injeção de dependência do FluentValidation, tipos do Node para o `tsc`, `using Xunit` nos testes), resolvidos a partir dos logs de build.
- Integração Firebase: criar o usuário local no primeiro acesso, tratando requisições simultâneas (a lista e o resumo chegam em paralelo).
- Publicação: escolher onde hospedar cada peça (a API em .NET exige container; o Postgres gratuito do Render expira), fazer a API respeitar `PORT` e `DATABASE_URL` e lidar com o "despertar" do plano gratuito.

**Facilidades**
- Identity entrega cadastro, hash de senha e perfis prontos.
- EF Core + Npgsql simplificam persistência, filtros e paginação.
- Ant Design acelera uma interface corporativa consistente.
- Separar serviço de controller tornou as regras testáveis sem subir a API.
- O Firebase dispensa implementar recuperação e armazenamento de credenciais.

## 16. Pendências do desenvolvedor

Itens que **não** podem ser resolvidos pelo código e dependem de você.

### Obrigatório para a entrega

- [ ] **Validar tudo após as últimas alterações:** `docker compose up --build`, `dotnet test` (em `backend`) e um teste manual completo (login com os dois perfis, CRUD, status, busca e filtros).
- [ ] **Revisar a [seção 15](#15-como-foi-feito-dificuldades-e-facilidades)** com o seu relato.
- [ ] **Criar o repositório Git** (GitHub ou GitLab), conferir que `bin/`, `obj/`, `node_modules/` e `.env` não estão versionados, e fazer o push. Se `npm install` gerou `frontend/package-lock.json`, versione-o.
- [ ] **Enviar o link do repositório** dentro do prazo (21/10).

### Para publicar (diferencial)

- [ ] Criar o projeto no **Neon** e copiar a connection string (`DATABASE_URL`).
- [ ] Criar o serviço da API no **Render** (Blueprint com `render.yaml` ou manual) e preencher `DATABASE_URL`, `Firebase__ProjectId` e `Firebase__AdminEmails__0`.
- [ ] Confirmar `/health` e `/swagger` na URL do Render.
- [ ] Copiar a URL da API para `frontend/vercel.json` (hoje há o marcador `SEU-SERVICO-API`), commitar e subir.
- [ ] Criar o projeto do **frontend na Vercel** (Root Directory `frontend`) com as variáveis `VITE_*`.
- [ ] Adicionar o domínio da Vercel em **Domínios autorizados** no Firebase.
- [ ] Testar o ambiente publicado de ponta a ponta e registrar as URLs no README e no e-mail de entrega.
- [ ] Conferir os limites atuais dos planos gratuitos (Render, Neon, Vercel) e lembrar do tempo de despertar da API.

### Para usar o Firebase (opcional)

- [ ] Criar o projeto no Firebase, habilitar e-mail/senha e registrar o app Web.
- [ ] Preencher as variáveis na API e no frontend (e reconstruir o frontend, pois `VITE_*` entram no build).
- [ ] Definir o e-mail do administrador em `Firebase__AdminEmails__0`.
- [ ] Desativar o seed (`SeedData__Enabled=false`) e remover contas de demonstração em ambiente real.

### Boas práticas antes de produção

- [ ] Trocar `Jwt__Key` e senhas padrão do banco.
- [ ] Migrar de `EnsureCreated` para migrations.
- [ ] Revisar a política de CORS e o armazenamento do token.

## 17. Solução de problemas

| Sintoma | Causa provável | Solução |
|---|---|---|
| `cannot find ... dockerDesktopLinuxEngine` | Docker Desktop fechado | Abra o Docker Desktop e aguarde o motor iniciar |
| `dotnet test` não encontra projeto | Pasta errada | Execute em `backend` ou use `dotnet test backend/TaskManager.sln` |
| Login funciona, mas a lista dá 401 (Firebase) | `Firebase__ProjectId` diferente do projeto do app Web | Use o mesmo `projectId` na API e no frontend |
| Tela de login do Firebase não aparece | Build sem `VITE_AUTH_PROVIDER=firebase` | Defina as variáveis e **reconstrua** (`docker compose up --build`) |
| Primeira requisição lenta ou 504 na API publicada (Render gratuito) | Serviço dormindo por inatividade | Abra `/health`, aguarde cerca de um minuto e tente de novo |
| API não inicia: falha de conexão com o Postgres | `DATABASE_URL` ausente ou incorreta | Confira a connection string do Neon no painel do Render |
| Rotas como `/login` dão 404 após o deploy | Falta o rewrite de SPA | Confirme o `frontend/vercel.json` |
| API não inicia: `Firebase:ProjectId é obrigatório` | `Auth__Provider=Firebase` sem `Firebase__ProjectId` | Defina o `projectId` ou use `Auth__Provider=Local` |
| Chamadas `/api` falham no site publicado | URL da API no `frontend/vercel.json` não foi trocada | Substitua `SEU-SERVICO-API` pela URL do Render |

## 18. Limitações conhecidas e evolução

- Schema criado por `EnsureCreated`; adotar migrations.
- Perfil Admin no modo Firebase por lista de e-mails; evoluir para *custom claims*.
- Token do modo Local em `localStorage`; evoluir para cookie `HttpOnly` com refresh token.
- Ordenação por coluna e visão kanban não implementadas.
- Testes cobrem a camada de serviço; faltam testes de integração da API e de componentes do frontend.
- Os testes automatizados não exercitam o fluxo Firebase; ele deve ser validado manualmente.
