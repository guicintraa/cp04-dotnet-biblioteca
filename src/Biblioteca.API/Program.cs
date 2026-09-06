// Importa os middlewares customizados da aplicação
using Biblioteca.API.Aplicacao.Middlewares;
// Importa os serviços da camada de aplicação
using Biblioteca.API.Aplicacao.Servicos;
// Importa as entidades de domínio (usadas na carga inicial)
using Biblioteca.API.Dominio.Entidades;
// Importa os contratos de interface do domínio
using Biblioteca.API.Dominio.Interfaces;
// Importa o mapeamento das rotas
using Biblioteca.API.Endpoints;
// Importa o contexto do Entity Framework Core
using Biblioteca.API.Infraestrutura.Contexto;
// Importa as verificações de saúde da infraestrutura
using Biblioteca.API.Infraestrutura.Health;
// Importa a classe estática de métricas e tracing customizados
using Biblioteca.API.Infraestrutura.Observabilidade;
// Importa as implementações da camada de infraestrutura
using Biblioteca.API.Infraestrutura.Repositorios;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
// Importa o namespace principal do Serilog
using Serilog;

// Inicializa o construtor da aplicação Web ASP.NET Core
var builder = WebApplication.CreateBuilder(args);

// Configuração do Logger global do Serilog
Log.Logger = new LoggerConfiguration()
    // Define o nível mínimo de detalhamento dos logs como Information
    .MinimumLevel.Information()
    // Reduz o ruído dos logs internos do framework e do EF Core
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    // Habilita a captura de propriedades injetadas pelo LogContext (ex: CorrelationId)
    .Enrich.FromLogContext()
    // Configura a gravação no Console com template formatado
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    // Configura a gravação em arquivos de texto na pasta logs/ com rotação diária
    .WriteTo.File("logs/app-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate:
            "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    // Cria a instância do logger
    .CreateLogger();

// Substitui o provedor de logging padrão da Microsoft pelo Serilog
builder.Host.UseSerilog();

// Configura o Entity Framework Core apontando para o banco de dados Oracle.
// A senha deve vir do User Secrets (dotnet user-secrets) ou de variável de ambiente,
// nunca do appsettings.json versionado.
// A chave Biblioteca:UsarBancoEmMemoria permite demonstrar a API sem o servidor Oracle.
var usarBancoEmMemoria = builder.Configuration.GetValue<bool>("Biblioteca:UsarBancoEmMemoria");
var stringConexao = builder.Configuration.GetConnectionString("Oracle");

builder.Services.AddDbContext<BibliotecaContexto>(opcoes =>
{
    if (usarBancoEmMemoria || string.IsNullOrWhiteSpace(stringConexao))
        opcoes.UseInMemoryDatabase("BibliotecaDemo");
    else
        opcoes.UseOracle(stringConexao);
});

// Habilita a leitura de metadados das rotas para gerar documentação OpenAPI
builder.Services.AddEndpointsApiExplorer();
// Adiciona o serviço gerador de interface gráfica e documentação do Swagger
builder.Services.AddSwaggerGen();

// Registra os repositórios como Scoped (mesmo tempo de vida do DbContext)
builder.Services.AddScoped<IAutorRepositorio, AutorRepositorio>();
builder.Services.AddScoped<ILivroRepositorio, LivroRepositorio>();
// Registra os serviços da aplicação como Scoped (instância nova a cada requisição HTTP)
builder.Services.AddScoped<IAutorServico, AutorServico>();
builder.Services.AddScoped<ILivroServico, LivroServico>();

// Registra os serviços de Health Check e adiciona a verificação do banco
builder.Services.AddHealthChecks()
    .AddCheck<BancoDadosHealthCheck>("banco_dados", tags: new[] { "infraestrutura", "pronto" })
    .AddCheck("api_viva", () => HealthCheckResult.Healthy("A API está respondendo."),
        tags: new[] { "aplicacao", "vivo" });

// Configuração e Registro do OpenTelemetry (Tracing & Metrics)
builder.Services.AddOpenTelemetry()
    .ConfigureResource(recurso => recurso.AddService(AplicacaoMetricas.NomeServico))
    .WithTracing(tracing => tracing
        // Auto-instrumentação para requisições HTTP recebidas do ASP.NET Core
        .AddAspNetCoreInstrumentation()
        // Auto-instrumentação para chamadas HTTP de saída (HttpClient)
        .AddHttpClientInstrumentation()
        // Escuta a fonte de Spans customizados criada na nossa aplicação
        .AddSource(AplicacaoMetricas.NomeServico)
        // Exporta os dados de Tracing no Console (para fins didáticos)
        .AddConsoleExporter())
    .WithMetrics(metricas => metricas
        // Auto-instrumentação para métricas padrão do ASP.NET Core
        .AddAspNetCoreInstrumentation()
        // Escuta o Meter customizado da biblioteca
        .AddMeter(AplicacaoMetricas.NomeServico)
        // Exporta as medições no Console
        .AddConsoleExporter());

// Constrói e inicializa a aplicação com as dependências registradas
var app = builder.Build();

// ATENÇÃO À ORDEM: o CorrelationIdMiddleware precisa vir ANTES do
// UseSerilogRequestLogging. O middleware de request logging só escreve a linha
// de resumo HTTP depois que os middlewares internos terminaram — se ele fosse o
// mais externo, o escopo do LogContext já estaria descartado e aquela linha
// sairia sem a chave de rastreabilidade.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

// Verifica se o ambiente de execução atual é o de Desenvolvimento
if (app.Environment.IsDevelopment())
{
    // Habilita o middleware que gera o arquivo JSON do Swagger
    app.UseSwagger();
    // Habilita a interface visual interativa do Swagger UI no navegador
    app.UseSwaggerUI();
}

// Habilita o middleware para redirecionar requisições HTTP inseguras para HTTPS
app.UseHttpsRedirection();

// Mapeia os endpoints de diagnóstico (/health, /health/vivo, /health/pronto)
app.MapDiagnosticosEndpoints();
// Mapeia o CRUD de autores
app.MapAutoresEndpoints();
// Mapeia o CRUD de livros e o registro de empréstimo
app.MapLivrosEndpoints();

// Executa a carga inicial de dados quando a API sobe em modo demonstração
if (usarBancoEmMemoria || string.IsNullOrWhiteSpace(stringConexao))
    SemearDados(app);

Log.Information("Aplicação iniciada. Swagger em /swagger e diagnósticos em /health.");

// Inicia a execução do servidor web e escuta as requisições
app.Run();

// Popula a base em memória com dados de exemplo para a demonstração em vídeo
static void SemearDados(WebApplication app)
{
    using var escopo = app.Services.CreateScope();
    var contexto = escopo.ServiceProvider.GetRequiredService<BibliotecaContexto>();
    contexto.Database.EnsureCreated();

    if (contexto.Autores.Any())
        return;

    var machado = new Autor("Machado de Assis", "Brasileira", new DateTime(1839, 6, 21));
    var clarice = new Autor("Clarice Lispector", "Brasileira", new DateTime(1920, 12, 10));

    var domCasmurro = new Livro("Dom Casmurro", "9788535910663", 39.90m, 12, 1899);
    var horaDaEstrela = new Livro("A Hora da Estrela", "9788520937136", 44.50m, 8, 1977);
    var antologia = new Livro("Antologia da Literatura Brasileira", "9788500000001", 89.90m, 5, 2020);

    domCasmurro.AssociarAutor(machado);
    horaDaEstrela.AssociarAutor(clarice);

    // Demonstra o relacionamento N:N: um mesmo livro com dois autores
    antologia.AssociarAutor(machado);
    antologia.AssociarAutor(clarice);

    contexto.Livros.AddRange(domCasmurro, horaDaEstrela, antologia);
    contexto.SaveChanges();

    Log.Information("Carga inicial concluída: 2 autores e 3 livros cadastrados.");
}
