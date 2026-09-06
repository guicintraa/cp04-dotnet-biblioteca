// Importa os DTOs, os endpoints e os serviços do projeto principal
using Biblioteca.API.Aplicacao.Dtos;
using Biblioteca.API.Aplicacao.Servicos;
using Biblioteca.API.Endpoints;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Biblioteca.Tests.Unit.Endpoints;

// Suíte de testes da camada de endpoint (apresentação HTTP).
// Aqui o alvo não é a regra de negócio e sim a tradução requisição/resposta:
// qual status HTTP é devolvido em cada cenário. O serviço de aplicação é
// substituído por um Mock<IAutorServico>.
public class AutoresEndpointsTests
{
    private readonly Mock<IAutorServico> _servicoMock;
    private readonly ILogger<CategoriaAutores> _logger;

    public AutoresEndpointsTests()
    {
        // Instancia o Mock da interface IAutorServico
        _servicoMock = new Mock<IAutorServico>();
        // Logger nulo: o teste não valida logs, apenas o contrato HTTP
        _logger = NullLogger<CategoriaAutores>.Instance;
    }

    // Extrai o status HTTP de qualquer IResult devolvido pelo endpoint
    private static int ObterStatus(IResult resultado) =>
        resultado.Should().BeAssignableTo<IStatusCodeHttpResult>().Which.StatusCode
            ?? throw new InvalidOperationException("O resultado não expôs um status HTTP.");

    // Extrai o corpo de qualquer IResult que carregue valor
    private static object? ObterValor(IResult resultado) =>
        resultado.Should().BeAssignableTo<IValueHttpResult>().Which.Value;

    [Fact]
    public void ObterTodos_ExistemAutores_DeveRetornar200ComAColecao()
    {
        // Arrange
        var autores = new List<AutorResponse>
        {
            new(1, "Machado de Assis", "Brasileira", new DateTime(1839, 6, 21), Array.Empty<LivroResumoResponse>())
        };

        _servicoMock.Setup(s => s.ListarTodos()).Returns(autores);

        // Act
        var resultado = AutoresEndpoints.ObterTodos(_servicoMock.Object, _logger);

        // Assert
        ObterStatus(resultado).Should().Be(StatusCodes.Status200OK);
        _servicoMock.Verify(s => s.ListarTodos(), Times.Once);
    }

    [Fact]
    public void ObterPorId_AutorExistente_DeveRetornar200ComOAutor()
    {
        // Arrange
        int id = 1;
        var autor = new AutorResponse(id, "Clarice Lispector", "Brasileira",
            new DateTime(1920, 12, 10), Array.Empty<LivroResumoResponse>());

        _servicoMock.Setup(s => s.BuscarPorId(id)).Returns(autor);

        // Act
        var resultado = AutoresEndpoints.ObterPorId(id, _servicoMock.Object, _logger);

        // Assert
        ObterStatus(resultado).Should().Be(StatusCodes.Status200OK);
        ObterValor(resultado).Should().BeEquivalentTo(autor);
        _servicoMock.Verify(s => s.BuscarPorId(id), Times.Once);
    }

    [Fact]
    public void ObterPorId_AutorInexistente_DeveRetornar404()
    {
        // Arrange
        int idInexistente = 999;
        _servicoMock.Setup(s => s.BuscarPorId(idInexistente)).Returns((AutorResponse?)null);

        // Act
        var resultado = AutoresEndpoints.ObterPorId(idInexistente, _servicoMock.Object, _logger);

        // Assert
        ObterStatus(resultado).Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void Criar_DadosValidos_DeveRetornar201EChamarOServicoUmaVez()
    {
        // Arrange
        var request = new CriarAutorRequest("Graciliano Ramos", "Brasileira", new DateTime(1892, 10, 27));
        var criado = new AutorResponse(5, request.Nome, request.Nacionalidade,
            request.DataNascimento, Array.Empty<LivroResumoResponse>());

        _servicoMock.Setup(s => s.Criar(request.Nome, request.Nacionalidade, request.DataNascimento))
                    .Returns(criado);

        // Act
        var resultado = AutoresEndpoints.Criar(request, _servicoMock.Object, _logger);

        // Assert
        ObterStatus(resultado).Should().Be(StatusCodes.Status201Created);
        _servicoMock.Verify(s => s.Criar(request.Nome, request.Nacionalidade, request.DataNascimento), Times.Once);
    }

    [Fact]
    public void Criar_ServicoLancaArgumentException_DeveRetornar400ComAMensagem()
    {
        // Arrange
        var request = new CriarAutorRequest("", "Brasileira", new DateTime(1892, 10, 27));

        _servicoMock.Setup(s => s.Criar(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                    .Throws(new ArgumentException("O nome do autor é obrigatório."));

        // Act
        var resultado = AutoresEndpoints.Criar(request, _servicoMock.Object, _logger);

        // Assert
        ObterStatus(resultado).Should().Be(StatusCodes.Status400BadRequest);
        ObterValor(resultado).Should().NotBeNull();
    }

    [Fact]
    public void Atualizar_AutorInexistente_DeveRetornar404()
    {
        // Arrange
        int idInexistente = 999;
        var request = new AtualizarAutorRequest("Nome", "Brasileira", new DateTime(1950, 1, 1));

        _servicoMock.Setup(s => s.Atualizar(idInexistente, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                    .Returns((AutorResponse?)null);

        // Act
        var resultado = AutoresEndpoints.Atualizar(idInexistente, request, _servicoMock.Object, _logger);

        // Assert
        ObterStatus(resultado).Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void Remover_AutorExistente_DeveRetornar204()
    {
        // Arrange
        int id = 1;
        _servicoMock.Setup(s => s.Remover(id)).Returns(true);

        // Act
        var resultado = AutoresEndpoints.Remover(id, _servicoMock.Object, _logger);

        // Assert
        ObterStatus(resultado).Should().Be(StatusCodes.Status204NoContent);
        _servicoMock.Verify(s => s.Remover(id), Times.Once);
    }

    [Fact]
    public void Remover_AutorInexistente_DeveRetornar404()
    {
        // Arrange
        int idInexistente = 42;
        _servicoMock.Setup(s => s.Remover(idInexistente)).Returns(false);

        // Act
        var resultado = AutoresEndpoints.Remover(idInexistente, _servicoMock.Object, _logger);

        // Assert
        ObterStatus(resultado).Should().Be(StatusCodes.Status404NotFound);
    }
}
