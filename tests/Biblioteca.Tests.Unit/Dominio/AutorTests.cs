// Importa as entidades do domínio do projeto principal
using Biblioteca.API.Dominio.Entidades;
// Importa o FluentAssertions para sintaxe expressiva de asserção
using FluentAssertions;
// Importa o xUnit para anotações e execução de testes
using Xunit;

namespace Biblioteca.Tests.Unit.Dominio;

// Suíte de testes unitários para validar as regras puras do Domínio (entidade Autor)
public class AutorTests
{
    [Fact]
    public void Construtor_DadosValidos_DeveCriarInstanciaComSucesso()
    {
        // Arrange (Preparação)
        string nomeValido = "Machado de Assis";
        string nacionalidadeValida = "Brasileira";
        DateTime dataNascimentoValida = new DateTime(1839, 6, 21);

        // Act (Ação)
        var autor = new Autor(nomeValido, nacionalidadeValida, dataNascimentoValida);

        // Assert (Validação/Asserção)
        autor.Should().NotBeNull();
        autor.Nome.Should().Be(nomeValido);
        autor.Nacionalidade.Should().Be(nacionalidadeValida);
        autor.DataNascimento.Should().Be(dataNascimentoValida);
        autor.Livros.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Construtor_NomeInvalido_DeveLancarArgumentException(string nomeInvalido)
    {
        // Arrange
        string nacionalidadeValida = "Brasileira";
        DateTime dataNascimentoValida = new DateTime(1900, 1, 1);

        // Act
        Action acao = () => new Autor(nomeInvalido, nacionalidadeValida, dataNascimentoValida);

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*O nome do autor é obrigatório.*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Construtor_NacionalidadeInvalida_DeveLancarArgumentException(string nacionalidadeInvalida)
    {
        // Arrange
        string nomeValido = "Jorge Amado";
        DateTime dataNascimentoValida = new DateTime(1912, 8, 10);

        // Act
        Action acao = () => new Autor(nomeValido, nacionalidadeInvalida, dataNascimentoValida);

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*A nacionalidade do autor é obrigatória.*");
    }

    [Fact]
    public void Construtor_DataNascimentoFutura_DeveLancarArgumentException()
    {
        // Arrange
        string nomeValido = "Autor Futurista";
        DateTime dataInvalida = DateTime.Today.AddYears(1);

        // Act
        Action acao = () => new Autor(nomeValido, "Brasileira", dataInvalida);

        // Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*A data de nascimento não pode estar no futuro.*");
    }

    [Fact]
    public void Atualizar_DadosValidos_DeveAlterarOsCamposDoAutor()
    {
        // Arrange
        var autor = new Autor("Nome Original", "Brasileira", new DateTime(1950, 5, 5));

        // Act
        autor.Atualizar("Nome Atualizado", "Portuguesa", new DateTime(1951, 6, 6));

        // Assert
        autor.Nome.Should().Be("Nome Atualizado");
        autor.Nacionalidade.Should().Be("Portuguesa");
        autor.DataNascimento.Should().Be(new DateTime(1951, 6, 6));
    }

    [Fact]
    public void Atualizar_NomeInvalido_DeveManterOEstadoOriginal()
    {
        // Arrange
        var autor = new Autor("Nome Original", "Brasileira", new DateTime(1950, 5, 5));

        // Act
        Action acao = () => autor.Atualizar("", "Portuguesa", new DateTime(1951, 6, 6));

        // Assert
        acao.Should().Throw<ArgumentException>();
        autor.Nome.Should().Be("Nome Original");
        autor.Nacionalidade.Should().Be("Brasileira");
    }

    [Fact]
    public void AssociarLivro_LivroValido_DeveIncluirNaColecaoDoRelacionamento()
    {
        // Arrange
        var autor = new Autor("Machado de Assis", "Brasileira", new DateTime(1839, 6, 21));
        var livro = new Livro("Dom Casmurro", "9788535910663", 39.90m, 10, 1899);

        // Act
        autor.AssociarLivro(livro);

        // Assert
        autor.Livros.Should().HaveCount(1);
        autor.Livros.First().Livro.Should().BeSameAs(livro);
        autor.Livros.First().Autor.Should().BeSameAs(autor);
    }
}
