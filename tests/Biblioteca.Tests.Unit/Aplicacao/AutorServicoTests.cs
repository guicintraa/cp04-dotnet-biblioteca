// Importa o serviço de aplicação e entidades
using Biblioteca.API.Aplicacao.Servicos;
using Biblioteca.API.Dominio.Entidades;
using Biblioteca.API.Dominio.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Biblioteca.Tests.Unit.Aplicacao;

// Suíte de testes unitários com Mock de repositório para a camada de aplicação
public class AutorServicoTests
{
    private readonly Mock<IAutorRepositorio> _repositorioMock;
    private readonly AutorServico _servico;

    public AutorServicoTests()
    {
        // Instancia o Mock da interface IAutorRepositorio
        _repositorioMock = new Mock<IAutorRepositorio>();
        // Injeta a instância simulada no serviço testado
        _servico = new AutorServico(_repositorioMock.Object);
    }

    [Fact]
    public void Criar_DadosValidos_DeveSalvarNoRepositorioERetornarAutor()
    {
        // Arrange
        string nome = "Machado de Assis";
        string nacionalidade = "Brasileira";
        DateTime dataNascimento = new DateTime(1839, 6, 21);

        // Act
        var autorCriado = _servico.Criar(nome, nacionalidade, dataNascimento);

        // Assert
        autorCriado.Should().NotBeNull();
        autorCriado.Nome.Should().Be(nome);
        autorCriado.Livros.Should().BeEmpty();

        // Verifica se o método Adicionar do repositório foi chamado exatamente 1 vez
        _repositorioMock.Verify(r => r.Adicionar(It.Is<Autor>(a => a.Nome == nome)), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Criar_NomeInvalido_DeveLancarExcecaoENaoSalvarNoRepositorio(string nomeInvalido)
    {
        // Arrange
        string nacionalidade = "Brasileira";
        DateTime dataNascimento = new DateTime(1900, 1, 1);

        // Act
        Action acao = () => _servico.Criar(nomeInvalido, nacionalidade, dataNascimento);

        // Assert
        acao.Should().Throw<ArgumentException>();

        // Garante que nada foi persistido quando a regra de domínio falhou
        _repositorioMock.Verify(r => r.Adicionar(It.IsAny<Autor>()), Times.Never);
    }

    [Fact]
    public void ListarTodos_AutoresCadastrados_DeveRetornarAutoresComSeusLivros()
    {
        // Arrange
        var autor = new Autor("Machado de Assis", "Brasileira", new DateTime(1839, 6, 21));
        autor.AssociarLivro(new Livro("Dom Casmurro", "9788535910663", 39.90m, 10, 1899));

        // Configura o comportamento do Mock para devolver a lista simulada
        _repositorioMock.Setup(r => r.ObterTodos())
                        .Returns(new List<Autor> { autor });

        // Act
        var resultado = _servico.ListarTodos();

        // Assert
        resultado.Should().HaveCount(1);
        resultado.First().Livros.Should().ContainSingle(l => l.Titulo == "Dom Casmurro");

        _repositorioMock.Verify(r => r.ObterTodos(), Times.Once);
    }

    [Fact]
    public void BuscarPorId_AutorExistente_DeveRetornarAutorEsperado()
    {
        // Arrange
        int idExistente = 1;
        var autorExistente = new Autor("Clarice Lispector", "Brasileira", new DateTime(1920, 12, 10));

        // Configura o comportamento do Mock para retornar o autor quando o ID bater
        _repositorioMock.Setup(r => r.ObterPorId(idExistente))
                        .Returns(autorExistente);

        // Act
        var resultado = _servico.BuscarPorId(idExistente);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Nome.Should().Be("Clarice Lispector");

        _repositorioMock.Verify(r => r.ObterPorId(idExistente), Times.Once);
    }

    [Fact]
    public void BuscarPorId_AutorInexistente_DeveRetornarNulo()
    {
        // Arrange
        int idInexistente = 999;
        _repositorioMock.Setup(r => r.ObterPorId(idInexistente))
                        .Returns((Autor?)null);

        // Act
        var resultado = _servico.BuscarPorId(idInexistente);

        // Assert
        resultado.Should().BeNull();
        _repositorioMock.Verify(r => r.ObterPorId(idInexistente), Times.Once);
    }

    [Fact]
    public void Atualizar_AutorExistente_DeveAlterarOsDadosEChamarORepositorio()
    {
        // Arrange
        int id = 1;
        var autorExistente = new Autor("Nome Original", "Brasileira", new DateTime(1950, 5, 5));

        _repositorioMock.Setup(r => r.ObterPorId(id))
                        .Returns(autorExistente);

        // Act
        var resultado = _servico.Atualizar(id, "Nome Atualizado", "Portuguesa", new DateTime(1951, 6, 6));

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Nome.Should().Be("Nome Atualizado");

        _repositorioMock.Verify(r => r.Atualizar(autorExistente), Times.Once);
    }

    [Fact]
    public void Remover_AutorInexistente_DeveRetornarFalsoENaoChamarORepositorio()
    {
        // Arrange
        int idInexistente = 42;
        _repositorioMock.Setup(r => r.ObterPorId(idInexistente))
                        .Returns((Autor?)null);

        // Act
        var resultado = _servico.Remover(idInexistente);

        // Assert
        resultado.Should().BeFalse();
        _repositorioMock.Verify(r => r.Remover(It.IsAny<Autor>()), Times.Never);
    }
}
