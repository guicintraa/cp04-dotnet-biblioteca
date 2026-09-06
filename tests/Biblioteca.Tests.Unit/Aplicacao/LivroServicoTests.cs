// Importa o serviço de aplicação, DTOs e entidades
using Biblioteca.API.Aplicacao.Dtos;
using Biblioteca.API.Aplicacao.Servicos;
using Biblioteca.API.Dominio.Entidades;
using Biblioteca.API.Dominio.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Biblioteca.Tests.Unit.Aplicacao;

// Suíte de testes unitários com Mock de repositório para a camada de aplicação
public class LivroServicoTests
{
    private readonly Mock<ILivroRepositorio> _livroRepositorioMock;
    private readonly Mock<IAutorRepositorio> _autorRepositorioMock;
    private readonly LivroServico _servico;

    public LivroServicoTests()
    {
        // Instancia os Mocks das interfaces de repositório
        _livroRepositorioMock = new Mock<ILivroRepositorio>();
        _autorRepositorioMock = new Mock<IAutorRepositorio>();
        // Injeta as instâncias simuladas no serviço testado
        _servico = new LivroServico(_livroRepositorioMock.Object, _autorRepositorioMock.Object);
    }

    // Monta um pedido de criação válido reaproveitado por vários cenários
    private static CriarLivroRequest CriarRequestValido(params int[] autoresIds) =>
        new("Dom Casmurro", "9788535910663", 39.90m, 10, 1899, autoresIds);

    [Fact]
    public void Criar_DadosValidos_DeveSalvarNoRepositorioERetornarLivro()
    {
        // Arrange
        var autor = new Autor("Machado de Assis", "Brasileira", new DateTime(1839, 6, 21));
        var request = CriarRequestValido(1);

        // Configura os retornos simulados dos repositórios
        _livroRepositorioMock.Setup(r => r.ExisteIsbn(request.Isbn)).Returns(false);
        _autorRepositorioMock.Setup(r => r.ObterPorIds(It.IsAny<IEnumerable<int>>()))
                             .Returns(new List<Autor> { autor });

        // Act
        var livroCriado = _servico.Criar(request);

        // Assert
        livroCriado.Should().NotBeNull();
        livroCriado.Titulo.Should().Be("Dom Casmurro");
        livroCriado.Autores.Should().ContainSingle(a => a.Nome == "Machado de Assis");

        // Verifica se o método Adicionar do repositório foi chamado exatamente 1 vez
        _livroRepositorioMock.Verify(r => r.Adicionar(It.Is<Livro>(l => l.Titulo == "Dom Casmurro")), Times.Once);
        _autorRepositorioMock.Verify(r => r.ObterPorIds(It.IsAny<IEnumerable<int>>()), Times.Once);
    }

    [Fact]
    public void Criar_DoisAutoresInformados_DeveMontarRelacionamentoMuitosParaMuitos()
    {
        // Arrange
        var machado = new Autor("Machado de Assis", "Brasileira", new DateTime(1839, 6, 21));
        var clarice = new Autor("Clarice Lispector", "Brasileira", new DateTime(1920, 12, 10));
        var request = new CriarLivroRequest("Antologia", "9788500000001", 89.90m, 5, 2020, new[] { 1, 2 });

        _livroRepositorioMock.Setup(r => r.ExisteIsbn(request.Isbn)).Returns(false);
        _autorRepositorioMock.Setup(r => r.ObterPorIds(It.IsAny<IEnumerable<int>>()))
                             .Returns(new List<Autor> { machado, clarice });

        // Act
        var livroCriado = _servico.Criar(request);

        // Assert
        livroCriado.Autores.Should().HaveCount(2);
        _livroRepositorioMock.Verify(r => r.Adicionar(It.IsAny<Livro>()), Times.Once);
    }

    [Fact]
    public void Criar_SemAutores_DeveLancarArgumentExceptionENaoSalvar()
    {
        // Arrange
        var request = CriarRequestValido();

        // Act
        Action acao = () => _servico.Criar(request);

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*pelo menos um autor*");

        _livroRepositorioMock.Verify(r => r.Adicionar(It.IsAny<Livro>()), Times.Never);
    }

    [Fact]
    public void Criar_IsbnDuplicado_DeveLancarArgumentExceptionENaoSalvar()
    {
        // Arrange
        var request = CriarRequestValido(1);
        _livroRepositorioMock.Setup(r => r.ExisteIsbn(request.Isbn)).Returns(true);

        // Act
        Action acao = () => _servico.Criar(request);

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*Já existe um livro cadastrado com o ISBN*");

        _livroRepositorioMock.Verify(r => r.ExisteIsbn(request.Isbn), Times.Once);
        _livroRepositorioMock.Verify(r => r.Adicionar(It.IsAny<Livro>()), Times.Never);
    }

    [Fact]
    public void Criar_AutorInexistente_DeveLancarArgumentExceptionENaoSalvar()
    {
        // Arrange
        var request = CriarRequestValido(1, 2);

        _livroRepositorioMock.Setup(r => r.ExisteIsbn(request.Isbn)).Returns(false);
        // Apenas um dos dois autores solicitados retorna do repositório
        _autorRepositorioMock.Setup(r => r.ObterPorIds(It.IsAny<IEnumerable<int>>()))
                             .Returns(new List<Autor>
                             {
                                 new("Machado de Assis", "Brasileira", new DateTime(1839, 6, 21))
                             });

        // Act
        Action acao = () => _servico.Criar(request);

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*autores informados não foram encontrados*");

        _livroRepositorioMock.Verify(r => r.Adicionar(It.IsAny<Livro>()), Times.Never);
    }

    [Fact]
    public void Criar_PrecoInvalido_DevePropagarExcecaoDoDominio()
    {
        // Arrange
        var request = new CriarLivroRequest("Livro Gratuito", "9780000000009", 0m, 5, 2020, new[] { 1 });

        _livroRepositorioMock.Setup(r => r.ExisteIsbn(request.Isbn)).Returns(false);
        _autorRepositorioMock.Setup(r => r.ObterPorIds(It.IsAny<IEnumerable<int>>()))
                             .Returns(new List<Autor>
                             {
                                 new("Machado de Assis", "Brasileira", new DateTime(1839, 6, 21))
                             });

        // Act
        Action acao = () => _servico.Criar(request);

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*O preço do livro deve ser maior que zero.*");

        _livroRepositorioMock.Verify(r => r.Adicionar(It.IsAny<Livro>()), Times.Never);
    }

    [Fact]
    public void BuscarPorId_LivroExistente_DeveRetornarLivroEsperado()
    {
        // Arrange
        int idExistente = 1;
        var livroExistente = new Livro("Dom Casmurro", "9788535910663", 39.90m, 10, 1899);
        livroExistente.AssociarAutor(new Autor("Machado de Assis", "Brasileira", new DateTime(1839, 6, 21)));

        _livroRepositorioMock.Setup(r => r.ObterPorId(idExistente)).Returns(livroExistente);

        // Act
        var resultado = _servico.BuscarPorId(idExistente);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Titulo.Should().Be("Dom Casmurro");
        resultado.Autores.Should().ContainSingle();

        _livroRepositorioMock.Verify(r => r.ObterPorId(idExistente), Times.Once);
    }

    [Fact]
    public void BuscarPorId_LivroInexistente_DeveRetornarNulo()
    {
        // Arrange
        int idInexistente = 999;
        _livroRepositorioMock.Setup(r => r.ObterPorId(idInexistente)).Returns((Livro?)null);

        // Act
        var resultado = _servico.BuscarPorId(idInexistente);

        // Assert
        resultado.Should().BeNull();
        _livroRepositorioMock.Verify(r => r.ObterPorId(idInexistente), Times.Once);
    }

    [Fact]
    public void Remover_LivroExistente_DeveChamarRemoverUmaVezERetornarVerdadeiro()
    {
        // Arrange
        int id = 1;
        var livro = new Livro("Dom Casmurro", "9788535910663", 39.90m, 10, 1899);
        _livroRepositorioMock.Setup(r => r.ObterPorId(id)).Returns(livro);

        // Act
        var resultado = _servico.Remover(id);

        // Assert
        resultado.Should().BeTrue();
        _livroRepositorioMock.Verify(r => r.Remover(livro), Times.Once);
    }

    [Fact]
    public void RegistrarEmprestimo_EstoqueDisponivel_DeveDecrementarEstoqueEPersistir()
    {
        // Arrange
        int id = 1;
        var livro = new Livro("Dom Casmurro", "9788535910663", 39.90m, 3, 1899);
        _livroRepositorioMock.Setup(r => r.ObterPorId(id)).Returns(livro);

        // Act
        var resultado = _servico.RegistrarEmprestimo(id);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Estoque.Should().Be(2);

        _livroRepositorioMock.Verify(r => r.Atualizar(livro), Times.Once);
    }

    [Fact]
    public void RegistrarEmprestimo_SemEstoque_DeveLancarExcecaoENaoPersistir()
    {
        // Arrange
        int id = 1;
        var livro = new Livro("Dom Casmurro", "9788535910663", 39.90m, 0, 1899);
        _livroRepositorioMock.Setup(r => r.ObterPorId(id)).Returns(livro);

        // Act
        Action acao = () => _servico.RegistrarEmprestimo(id);

        // Assert
        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*Não há exemplares disponíveis*");

        _livroRepositorioMock.Verify(r => r.Atualizar(It.IsAny<Livro>()), Times.Never);
    }
}
