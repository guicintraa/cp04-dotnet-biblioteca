// Importa o middleware customizado da camada de aplicação
using Biblioteca.API.Aplicacao.Middlewares;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Biblioteca.Tests.Unit.Infraestrutura;

// Suíte de testes unitários do middleware de rastreabilidade (Correlation ID)
public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_SemCabecalho_DeveGerarNovoCorrelationId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        string? gerado = context.Response.Headers[CorrelationIdMiddleware.CorrelationIdHeader];
        gerado.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(gerado, out _).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ComCabecalhoInformado_DevePreservarOValorRecebido()
    {
        // Arrange
        string identificadorEsperado = "cp04-teste-12345";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.CorrelationIdHeader] = identificadorEsperado;

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        string? devolvido = context.Response.Headers[CorrelationIdMiddleware.CorrelationIdHeader];
        devolvido.Should().Be(identificadorEsperado);
    }

    [Fact]
    public async Task InvokeAsync_PipelineValida_DeveInvocarOProximoMiddleware()
    {
        // Arrange
        bool proximoFoiChamado = false;
        var context = new DefaultHttpContext();

        var middleware = new CorrelationIdMiddleware(_ =>
        {
            proximoFoiChamado = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        proximoFoiChamado.Should().BeTrue();
    }
}
