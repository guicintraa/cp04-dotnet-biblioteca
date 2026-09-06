// Importa as entidades do domínio do projeto principal
using Biblioteca.API.Dominio.Entidades;
// Importa o FluentAssertions para sintaxe expressiva de asserção
using FluentAssertions;
// Importa o xUnit para anotações e execução de testes
using Xunit;

namespace Biblioteca.Tests.Unit.Dominio;

// Suíte de testes unitários para validar as regras puras do Domínio (entidade Livro)
public class LivroTests
{
    [Fact]
    public void Construtor_DadosValidos_DeveCriarInstanciaComSucesso()
    {
        // Arrange (Preparação)
        string tituloValido = "Dom Casmurro";
        string isbnValido = "9788535910663";
        decimal precoValido = 39.90m;
        int estoqueValido = 10;
        int anoValido = 1899;

        // Act (Ação)
        var livro = new Livro(tituloValido, isbnValido, precoValido, estoqueValido, anoValido);

        // Assert (Validação/Asserção)
        livro.Should().NotBeNull();
        livro.Titulo.Should().Be(tituloValido);
        livro.Isbn.Should().Be(isbnValido);
        livro.Preco.Should().Be(precoValido);
        livro.Estoque.Should().Be(estoqueValido);
        livro.AnoPublicacao.Should().Be(anoValido);
        livro.Autores.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Construtor_TituloInvalido_DeveLancarArgumentException(string tituloInvalido)
    {
        // Arrange
        string isbnValido = "9788535910663";

        // Act
        Action acao = () => new Livro(tituloInvalido, isbnValido, 39.90m, 10, 1899);

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*O título do livro é obrigatório.*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Construtor_IsbnInvalido_DeveLancarArgumentException(string isbnInvalido)
    {
        // Arrange
        string tituloValido = "Livro de Teste";

        // Act
        Action acao = () => new Livro(tituloValido, isbnInvalido, 39.90m, 10, 2024);

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*O ISBN do livro é obrigatório.*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10.50)]
    [InlineData(-1)]
    public void Construtor_PrecoInvalido_DeveLancarArgumentException(decimal precoInvalido)
    {
        // Arrange
        string tituloValido = "Livro de Teste";
        int estoqueValido = 5;

        // Act
        Action acao = () => new Livro(tituloValido, "9780000000001", precoInvalido, estoqueValido, 2024);

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*O preço do livro deve ser maior que zero.*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-50)]
    public void Construtor_EstoqueNegativo_DeveLancarArgumentException(int estoqueInvalido)
    {
        // Arrange
        string tituloValido = "Livro de Teste";
        decimal precoValido = 200.00m;

        // Act
        Action acao = () => new Livro(tituloValido, "9780000000001", precoValido, estoqueInvalido, 2024);

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*O estoque não pode ser negativo.*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1100)]
    [InlineData(9999)]
    public void Construtor_AnoPublicacaoInvalido_DeveLancarArgumentException(int anoInvalido)
    {
        // Arrange
        string tituloValido = "Livro de Teste";

        // Act
        Action acao = () => new Livro(tituloValido, "9780000000001", 25.00m, 10, anoInvalido);

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*O ano de publicação informado é inválido.*");
    }

    [Fact]
    public void AssociarAutor_DoisAutores_DeveMontarRelacionamentoMuitosParaMuitos()
    {
        // Arrange
        var livro = new Livro("Antologia Brasileira", "9788500000001", 89.90m, 5, 2020);
        var machado = new Autor("Machado de Assis", "Brasileira", new DateTime(1839, 6, 21));
        var clarice = new Autor("Clarice Lispector", "Brasileira", new DateTime(1920, 12, 10));

        // Act
        livro.AssociarAutor(machado);
        livro.AssociarAutor(clarice);

        // Assert
        livro.Autores.Should().HaveCount(2);
        livro.Autores.Select(la => la.Autor!.Nome)
             .Should().Contain(new[] { "Machado de Assis", "Clarice Lispector" });
    }

    [Fact]
    public void LimparAutores_LivroComAutores_DeveEsvaziarAColecao()
    {
        // Arrange
        var livro = new Livro("Antologia Brasileira", "9788500000001", 89.90m, 5, 2020);
        livro.AssociarAutor(new Autor("Machado de Assis", "Brasileira", new DateTime(1839, 6, 21)));

        // Act
        livro.LimparAutores();

        // Assert
        livro.Autores.Should().BeEmpty();
    }

    [Fact]
    public void RegistrarEmprestimo_EstoqueDisponivel_DeveDecrementarEmUmaUnidade()
    {
        // Arrange
        var livro = new Livro("Dom Casmurro", "9788535910663", 39.90m, 3, 1899);

        // Act
        livro.RegistrarEmprestimo();

        // Assert
        livro.Estoque.Should().Be(2);
    }

    [Fact]
    public void RegistrarEmprestimo_SemEstoque_DeveLancarInvalidOperationException()
    {
        // Arrange
        var livro = new Livro("Dom Casmurro", "9788535910663", 39.90m, 0, 1899);

        // Act
        Action acao = () => livro.RegistrarEmprestimo();

        // Assert
        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*Não há exemplares disponíveis*");
        livro.Estoque.Should().Be(0);
    }

    [Fact]
    public void Atualizar_DadosValidos_DeveAlterarOsCamposDoLivro()
    {
        // Arrange
        var livro = new Livro("Título Original", "9780000000001", 20.00m, 5, 2010);

        // Act
        livro.Atualizar("Título Atualizado", "9780000000002", 55.50m, 20, 2022);

        // Assert
        livro.Titulo.Should().Be("Título Atualizado");
        livro.Isbn.Should().Be("9780000000002");
        livro.Preco.Should().Be(55.50m);
        livro.Estoque.Should().Be(20);
        livro.AnoPublicacao.Should().Be(2022);
    }
}
