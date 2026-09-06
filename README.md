# CP04 — Sistema de Gerenciamento de Biblioteca

#INTEGRANTES

Guilherme Cintra RM562850
Pedro Fonseca de Almeida RM563466
Daniel Fonseca de Almeida RM563045

Evolução do projeto do CP03. **Minimal API** em ASP.NET Core 8 com relacionamento **N:N entre Autores e Livros** persistido em Oracle, acrescida de **Health Checks**, **logging estruturado com Serilog + Correlation ID**, **observabilidade com OpenTelemetry (Tracing e Métricas)** e **testes unitários com xUnit, Moq e FluentAssertions**.

A estrutura segue o padrão das Aulas 01 a 04: camadas `Dominio` / `Aplicacao` / `Infraestrutura`, endpoints mapeados no `Program.cs` com `MapGet`/`MapPost`, validações de domínio lançando `ArgumentException` e testes nomeados como `MetodoTestado_Cenario_ResultadoEsperado`.

---

## Sumário

- [Arquitetura](#arquitetura)
- [Como executar](#como-executar)
- [Banco de dados Oracle](#banco-de-dados-oracle)
- [Health Checks](#health-checks)
- [Logging e Correlation ID](#logging-e-correlation-id)
- [OpenTelemetry](#opentelemetry)
- [Testes unitários](#testes-unitários)
- [Endpoints](#endpoints)
- [Mapa dos critérios de avaliação](#mapa-dos-critérios-de-avaliação)

---

## Arquitetura

```
Biblioteca/
├── Biblioteca.sln
├── integrantes.txt
├── docs/
│   ├── script-oracle.sql          # DDL + carga inicial
│   ├── roteiro-video.md           # roteiro da demonstração no YouTube
│   └── Evidencias/                # prints da API em execução
├── src/Biblioteca.API/
│   ├── Program.cs                 # composição: Serilog, OpenTelemetry, DI, pipeline
│   ├── Biblioteca.API.http        # requisições prontas para executar no Visual Studio
│   ├── Endpoints/                 # AutoresEndpoints, LivrosEndpoints, DiagnosticosEndpoints
│   ├── Aplicacao/
│   │   ├── Dtos/                  # CriarAutorRequest, CriarLivroRequest, responses
│   │   ├── Middlewares/           # CorrelationIdMiddleware
│   │   └── Servicos/              # IAutorServico/AutorServico, ILivroServico/LivroServico
│   ├── Dominio/
│   │   ├── Entidades/             # Autor, Livro, LivroAutor (junção N:N)
│   │   └── Interfaces/            # IAutorRepositorio, ILivroRepositorio
│   └── Infraestrutura/
│       ├── Contexto/              # BibliotecaContexto (EF Core + Oracle)
│       ├── Health/                # BancoDadosHealthCheck
│       ├── Observabilidade/       # AplicacaoMetricas (Meter + ActivitySource)
│       └── Repositorios/          # AutorRepositorio, LivroRepositorio
└── tests/Biblioteca.Tests.Unit/
    ├── Dominio/                   # AutorTests, LivroTests
    ├── Aplicacao/                 # AutorServicoTests, LivroServicoTests
    ├── Endpoints/                 # AutoresEndpointsTests, LivrosEndpointsTests
    └── Infraestrutura/            # CorrelationIdMiddlewareTests, BancoDadosHealthCheckTests
```

O relacionamento N:N é materializado pela entidade de junção `LivroAutor`, com chave primária composta `(ID_LIVRO, ID_AUTOR)`.

Os endpoints não ficam como lambdas anônimas dentro do `Program.cs`: cada grupo de rotas
vive em uma classe estática na pasta `Endpoints/`, com os manipuladores escritos como
métodos nomeados. Isso mantém o `Program.cs` apenas como composição da aplicação e, mais
importante, permite **testar a camada de endpoint isoladamente**, chamando os métodos
diretamente com um mock do serviço.

Diferença em relação às aulas: como este projeto persiste em Oracle via EF Core, os repositórios e serviços são registrados como **Scoped** (mesmo tempo de vida do `DbContext`) em vez de Singleton, e o `Id` das entidades é `int` gerado pela sequência `IDENTITY` da tabela, não `Guid`.

---

## Como executar

Pré-requisito: **.NET SDK 8.0**.

```bash
dotnet restore
dotnet run --project src/Biblioteca.API   # Swagger em https://localhost:7290/swagger
dotnet test                                # suíte de testes
```

Por padrão `appsettings.json` traz `"UsarBancoEmMemoria": true`, o que permite executar e gravar a demonstração **sem depender do servidor Oracle da FIAP** (a base sobe com 2 autores e 3 livros, sendo um deles com dois autores). Para usar Oracle, veja a seção abaixo.

### Instalação dos pacotes (setup do zero)

```bash
# Projeto principal
cd src/Biblioteca.API
dotnet add package Microsoft.Extensions.Diagnostics.HealthChecks
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Exporter.Console
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol

# Projeto de testes
cd ../../tests/Biblioteca.Tests.Unit
dotnet add package Moq
dotnet add package FluentAssertions
```

---

## Banco de dados Oracle

1. Execute `docs/script-oracle.sql` no SQL Developer conectado à instância da FIAP.
2. Em `src/Biblioteca.API/appsettings.json`, troque para `"UsarBancoEmMemoria": false`.
3. Guarde a string de conexão no **User Secrets**, nunca no arquivo versionado:

No Visual Studio, botão direito no projeto `Biblioteca.API` > **Gerenciar Segredos do Usuário**, e cole:

```json
{
  "ConnectionStrings": {
    "Oracle": "User Id=RM000000;Password=SUA_SENHA;Data Source=oracle.fiap.com.br:1521/ORCL;"
  }
}
```

Ou pelo terminal, na pasta do projeto:

```bash
dotnet user-secrets set "ConnectionStrings:Oracle" "User Id=RM000000;Password=SUA_SENHA;Data Source=oracle.fiap.com.br:1521/ORCL;"
```

O arquivo de segredos fica fora da pasta do projeto, então a senha nunca vai para o Git. Em produção o mesmo valor viria da variável de ambiente `ConnectionStrings__Oracle`.

### Alternativa: EF Migrations

Em vez de rodar o DDL na mão, dá para gerar o schema pelo próprio EF Core. No **Console do Gerenciador de Pacotes** do Visual Studio, com `Biblioteca.API` como projeto padrão e a conexão Oracle já configurada:

```powershell
Add-Migration CriacaoInicial
Update-Database
```

O `docs/script-oracle.sql` continua útil como referência do schema esperado e para quem quiser criar as tabelas manualmente.

---

## Health Checks

`BancoDadosHealthCheck` (em `Infraestrutura/Health`) implementa `IHealthCheck` e valida a conexão real com o banco via `Database.CanConnectAsync()`, cronometrando a latência:

| Situação | Status |
|---|---|
| Conexão OK e resposta < 1500 ms | `Healthy` |
| Conexão OK porém lenta (> 1500 ms) | `Degraded` |
| Falha ou exceção na conexão | `Unhealthy` |

Para demonstrar a falha no vídeo sem derrubar o banco, basta trocar `"SimularFalhaBanco": true` no `appsettings.json` — equivalente ao `bool conexaoOk = false;` do roteiro da Aula 02.

Endpoints: `GET /health` (relatório completo em JSON), `GET /health/vivo` (liveness) e `GET /health/pronto` (readiness, inclui o banco).

```json
{
  "status": "Healthy",
  "duracaoTotalMs": 12.4,
  "verificacoes": [
    {
      "nome": "banco_dados",
      "status": "Healthy",
      "descricao": "Conexão com o Banco de Dados estabelecida com sucesso.",
      "dados": { "Provedor": "Oracle.EntityFrameworkCore", "LatenciaMs": 11 }
    }
  ]
}
```

---

## Logging e Correlation ID

O Serilog é configurado como logger global no `Program.cs` (`Log.Logger = new LoggerConfiguration()...` + `builder.Host.UseSerilog()`), com sink de **Console** (template já exibindo o Correlation ID) e sink de **arquivo rotativo diário** em `logs/app-.log`.

**Ordem dos middlewares.** O `CorrelationIdMiddleware` é registrado **antes** do `UseSerilogRequestLogging()`. Isso importa: o middleware de request logging só escreve a linha de resumo HTTP depois que os middlewares internos terminaram. Se ele fosse o mais externo, o escopo do `LogContext` já teria sido descartado e justamente aquela linha sairia sem a chave de rastreabilidade.

`app.UseSerilogRequestLogging()` grava uma linha por requisição HTTP. O `CorrelationIdMiddleware` lê o cabeçalho `X-Correlation-ID` (ou gera um GUID), empurra o valor no `LogContext` para que todos os logs da requisição carreguem a chave, e devolve o cabeçalho na resposta.

```bash
curl -k -i -H "X-Correlation-ID: demo-cp04-001" https://localhost:7290/api/livros
```

Todas as linhas do console referentes a essa chamada aparecem com `[demo-cp04-001]`.

---

## OpenTelemetry

`AplicacaoMetricas` (em `Infraestrutura/Observabilidade`) concentra os instrumentos:

| Instrumento | Nome | Tags |
|---|---|---|
| `ActivitySource` | `Biblioteca.API` | spans manuais dos endpoints de escrita |
| `Counter<long>` | `cadastros_total` | `entidade`, `status` |
| `Counter<long>` | `emprestimos_total` | `status` |

No `Program.cs`, `AddOpenTelemetry()` habilita **Tracing** (auto-instrumentação de ASP.NET Core e HttpClient + `AddSource("Biblioteca.API")`, exportador Console) e **Métricas** (`AddMeter("Biblioteca.API")`, exportador Console).

Nos endpoints de escrita — `POST /api/autores`, `POST /api/livros` e `POST /api/livros/{id}/emprestimos` — abre-se um span com `ActivitySourceAplicacao.StartActivity(...)`, anexam-se tags (`livro.isbn`, `livro.autores.quantidade`, `autor.nome`), marca-se `ActivityStatusCode.Error` no `catch` e incrementa-se o contador com a tag `status` (`sucesso`, `erro_validacao`, `sem_estoque`).

---

## Testes unitários

```bash
dotnet test
dotnet test --logger "console;verbosity=detailed"   # saída detalhada para o vídeo
```

| Arquivo | Camada | O que valida |
|---|---|---|
| `AutorTests.cs` | Domínio | criação válida (`[Fact]`), nome e nacionalidade vazios/nulos (`[Theory]` + `[InlineData]`), data futura, associação de livro |
| `LivroTests.cs` | Domínio | criação válida, título/ISBN inválidos, **preço zerado ou negativo**, **estoque negativo**, ano inválido, N:N com dois autores, empréstimo sem estoque |
| `AutorServicoTests.cs` | Aplicação | `Mock<IAutorRepositorio>` com `Setup(...)` e `Verify(..., Times.Once / Times.Never)` |
| `LivroServicoTests.cs` | Aplicação | orquestração N:N, ISBN duplicado, autor inexistente, empréstimo com e sem estoque |
| `AutoresEndpointsTests.cs` | Endpoint | contrato HTTP com `Mock<IAutorServico>`: 200, 201, 400, 404 e 204 |
| `LivrosEndpointsTests.cs` | Endpoint | contrato HTTP com `Mock<ILivroServico>`, incluindo empréstimo sem estoque |
| `CorrelationIdMiddlewareTests.cs` | Infraestrutura | geração e reaproveitamento da chave de rastreabilidade |
| `BancoDadosHealthCheckTests.cs` | Infraestrutura | `Healthy`, falha simulada e exceção no check customizado |

Todos os métodos seguem o padrão **AAA (Arrange, Act, Assert)** com os blocos comentados e a nomenclatura `MetodoTestado_Cenario_ResultadoEsperado`.

---

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/autores` | lista autores com os livros associados |
| GET | `/api/autores/{id}` | detalha um autor |
| POST | `/api/autores` | cadastra autor *(span + métrica)* |
| PUT | `/api/autores/{id}` | atualiza autor |
| DELETE | `/api/autores/{id}` | remove autor |
| GET | `/api/livros` | lista livros com os autores associados |
| GET | `/api/livros/{id}` | detalha um livro |
| POST | `/api/livros` | cadastra livro vinculando N autores *(span + métrica)* |
| PUT | `/api/livros/{id}` | atualiza livro e refaz os vínculos |
| DELETE | `/api/livros/{id}` | remove livro |
| POST | `/api/livros/{id}/emprestimos` | registra empréstimo *(span + métrica)* |
| GET | `/health` | diagnóstico completo |

### Testando sem sair do Visual Studio

O arquivo `src/Biblioteca.API/Biblioteca.API.http` traz todas as requisições prontas, incluindo o cabeçalho `X-Correlation-ID`. Abra o arquivo com a API rodando e clique em **Enviar Solicitação** acima de cada bloco.

Exemplo de cadastro com dois autores (N:N):

```bash
curl -k -X POST https://localhost:7290/api/livros \
  -H "Content-Type: application/json" \
  -H "X-Correlation-ID: demo-cp04-002" \
  -d '{
    "titulo": "Contos Reunidos",
    "isbn": "9788500000123",
    "preco": 79.90,
    "estoque": 4,
    "anoPublicacao": 2021,
    "autoresIds": [1, 2]
  }'
```

---
