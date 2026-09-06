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

// Suíte de testes da camada de endpoint de Livros, com Mock<ILivroServico>.
// Valida o contrato HTTP: 200, 201, 204, 400 e 404 em cada cenário.
public class LivrosEndpointsTests
{
    private readonly Mock<ILivroServico> _servicoMock;
    private readonly ILogger<CategoriaLivros> _logger;

    public LivrosEndpointsTests()
    {
        // Instancia o Mock da interface ILivroServico
        _servicoMock = new Mock<ILivroServico>();
        _logger = NullLogger<CategoriaLivros>.Instance;
    }

    // Extrai o status HTTP de qualquer IResult devolvido pelo endpoint
    private static int ObterStatus(IResult resultado) =>
        resultado.Should().BeAssignableTo<IStatusCodeHttpResult>().Which.StatusCode
            ?? throw new InvalidOperationException("O resultado não expôs um status HTTP.");

    // Extrai o corpo de qualquer IResult que carregue valor
    private static object? ObterValor(IResult resultado) =>
        resultado.Should().BeAssignableTo<IValueHttpResult>().Which.Value;

    // Monta uma resposta de livro reaproveitada por vários cenários
    private static LivroResponse MontarResposta(int id = 1, int estoque = 10) =>
        new(id, "Dom Casmurro", "9788535910663", 39.90m, estoque, 1899,
            new[] { new AutorResumoResponse(1, "Machado de Assis", "Brasileira") });

    [Fact]
    public void ObterTodos_ExistemLivros_DeveRetornar200ComAColecao()
    {
        // Arrange
        _servicoMock.Setup(s => s.ListarTodos()).Returns(new List<LivroResponse> { MontarResposta() });

        // Act
        var resultado = LivrosEndpoints.ObterTodos(_servicoMock.Object, _logger);

        // Assert
        ObterStatus(resultado).Should().Be(StatusCodes.Status200OK);
        _servicoMock.Verify(s => s.ListarTodos(), Times.Once);
    }

    [Fact]
    public void ObterPorId_LivroInexistente_DeveRetornar404()
    {
        // Arrange
        int idInexistente = 999;
        _servicoMock.Setup(s => s.BuscarPorId(idInexistente)).Returns((LivroResponse?)null);

        // Act
        var resultado = LivrosEndpoints.ObterPorId(idInexistente, _servicoMock.Object, _logger);

        // Assert
        ObterStatus(resultado).Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void Criar_DadosValidos_DeveRetornar201EChamarOServicoUmaVez()
    {
        // Arrange
        var request = new CriarLivroRequest("Dom Casmurro", "9788535910663", 39.90m, 10, 1899, new[] { 1 });
        _servicoMock.Setup(s => s.Criar(request)).Returns(MontarResposta());

        // Act
        var resultado = LivrosEndpoints.Criar(request, _servicoMock.Object, _logger);

        // Assert
        ObterStatus(resultado).Should().Be(StatusCodes.Status201Created);
        _servicoMock.Verify(s => s.Criar(request), Times.Once);
    }

    [Fact]
    public void Criar_ServicoLancaArgumentException_DeveRetornar400()
    {
        // Arrange
        var request = new CriarLivroRequest("Livro Inválido", "9788599999999", -50m, 5, 2021, new[] { 1 });

        _servicoMock.Setup(s => s.Criar(It.IsAny<CriarLivroRequest>()))
                    .Throws(new ArgumentException("O preço do livro deve ser maior que zero."));

        // Act
        var resultado = LivrosEndpoints.Criar(request, _servicoMock.Object, _logger);

        // Assert
        ObterStatus(resultado).Should().Be(StatusCodes.Status400BadRequest);
        ObterValor(resultado).Should().NotBeNull();
    }

    [Fact]
    public void Remover_LivroExistente_DeveRetornar204()
    {
        // Arrange
        int id = 1;
        _servicoMock.Setup(s => s.Remover(id)).Returns(true);

        // Act
        var resultado = LivrosEndpoints.Remover(id, _servicoMock.Object, _logger);

        // Assert
        ObterStatus(resultado).Should().Be(StatusCodes.Status204NoContent);
        _servicoMock.Verify(s => s.Remover(id), Times.Once);
    }

    [Fact]
    public void RegistrarEmprestimo_EstoqueDisponivel_DeveRetornar200ComEstoqueAtualizado()
    {
        // Arrange
        int id = 1;
        _servicoMock.Setup(s => s.RegistrarEmprestimo(id)).Returns(MontarResposta(id, estoque: 9));

        // Act
        var resultado = LivrosEndpoints.RegistrarEmprestimo(id, _servicoMock.Object, _logger);

        // Assert
        ObterStatus(resultado).Should().Be(StatusCodes.Status200OK);
        ObterValor(resultado).Should().BeOfType<LivroResponse>()
            .Which.Estoque.Should().Be(9);
    }

    [Fact]
    public void RegistrarEmprestimo_SemEstoque_DeveRetornar400()
    {
        // Arrange
        int id = 1;
        _servicoMock.Setup(s => s.RegistrarEmprestimo(id))
                    .Throws(new InvalidOperationException("Não há exemplares disponíveis do livro 'Dom Casmurro'."));

        // Act
        var resultado = LivrosEndpoints.RegistrarEmprestimo(id, _servicoMock.Object, _logger);

        // Assert
        ObterStatus(resultado).Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void RegistrarEmprestimo_LivroInexistente_DeveRetornar404()
    {
        // Arrange
        int idInexistente = 42;
        _servicoMock.Setup(s => s.RegistrarEmprestimo(idInexistente)).Returns((LivroResponse?)null);

        // Act
        var resultado = LivrosEndpoints.RegistrarEmprestimo(idInexistente, _servicoMock.Object, _logger);

        // Assert
        ObterStatus(resultado).Should().Be(StatusCodes.Status404NotFound);
    }
}
