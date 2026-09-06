// Importa o contexto e o Health Check da camada de infraestrutura
using Biblioteca.API.Infraestrutura.Contexto;
using Biblioteca.API.Infraestrutura.Health;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace Biblioteca.Tests.Unit.Infraestrutura;

// Suíte de testes unitários do Health Check customizado de banco de dados
public class BancoDadosHealthCheckTests
{
    // Cria um contexto isolado em memória para cada cenário de teste
    private static BibliotecaContexto CriarContexto()
    {
        var opcoes = new DbContextOptionsBuilder<BibliotecaContexto>()
            .UseInMemoryDatabase($"health-{Guid.NewGuid()}")
            .Options;

        return new BibliotecaContexto(opcoes);
    }

    // Monta uma configuração em memória com a chave de simulação de falha
    private static IConfiguration CriarConfiguracao(bool simularFalha) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Biblioteca:SimularFalhaBanco"] = simularFalha.ToString()
            })
            .Build();

    [Fact]
    public async Task CheckHealthAsync_BancoAcessivel_DeveRetornarHealthy()
    {
        // Arrange
        using var contexto = CriarContexto();
        var healthCheck = new BancoDadosHealthCheck(contexto, CriarConfiguracao(simularFalha: false));

        // Act
        var resultado = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        resultado.Status.Should().Be(HealthStatus.Healthy);
        resultado.Description.Should().Contain("sucesso");
        resultado.Data.Should().ContainKey("LatenciaMs");
        resultado.Data.Should().ContainKey("Provedor");
    }

    [Fact]
    public async Task CheckHealthAsync_FalhaSimuladaAtivada_DeveRetornarUnhealthy()
    {
        // Arrange
        using var contexto = CriarContexto();
        var healthCheck = new BancoDadosHealthCheck(contexto, CriarConfiguracao(simularFalha: true));

        // Act
        var resultado = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        resultado.Status.Should().Be(HealthStatus.Unhealthy);
        resultado.Description.Should().Contain("Falha ao conectar");
    }

    [Fact]
    public async Task CheckHealthAsync_ContextoDescartado_DeveRetornarUnhealthy()
    {
        // Arrange
        var contexto = CriarContexto();
        contexto.Dispose(); // força a falha de acesso ao banco

        var healthCheck = new BancoDadosHealthCheck(contexto, CriarConfiguracao(simularFalha: false));

        // Act
        var resultado = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        resultado.Status.Should().Be(HealthStatus.Unhealthy);
        resultado.Data.Should().ContainKey("Erro");
    }
}
