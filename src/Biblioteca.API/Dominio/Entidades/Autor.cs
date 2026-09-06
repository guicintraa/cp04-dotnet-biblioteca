// Declara o namespace correspondente à estrutura de pastas do domínio
namespace Biblioteca.API.Dominio.Entidades;

// Entidade de domínio Autor. Lado esquerdo do relacionamento N:N com Livro.
public class Autor
{
    // Coleção interna das associações do autor com seus livros
    private readonly List<LivroAutor> _livros = new();

    // Construtor sem parâmetros exigido pelo Entity Framework Core
    protected Autor()
    {
        Nome = string.Empty;
        Nacionalidade = string.Empty;
    }

    // Construtor público para criar novas instâncias válidas de um Autor
    public Autor(string nome, string nacionalidade, DateTime dataNascimento)
    {
        // Executa a validação das regras de negócio antes de preencher as propriedades
        Validar(nome, nacionalidade, dataNascimento);

        Nome = nome.Trim();
        Nacionalidade = nacionalidade.Trim();
        DataNascimento = dataNascimento.Date;
    }

    // Identificador gerado pela sequência (IDENTITY) da tabela TB_AUTORES
    public int Id { get; private set; }

    // Nome completo do autor
    public string Nome { get; private set; }

    // Nacionalidade declarada do autor
    public string Nacionalidade { get; private set; }

    // Data de nascimento do autor
    public DateTime DataNascimento { get; private set; }

    // Navegação somente leitura para os livros associados (relacionamento N:N)
    public IReadOnlyCollection<LivroAutor> Livros => _livros.AsReadOnly();

    // Atualiza os dados cadastrais do autor reaproveitando as mesmas validações
    public void Atualizar(string nome, string nacionalidade, DateTime dataNascimento)
    {
        Validar(nome, nacionalidade, dataNascimento);

        Nome = nome.Trim();
        Nacionalidade = nacionalidade.Trim();
        DataNascimento = dataNascimento.Date;
    }

    // Cria a associação entre este autor e um livro na tabela de junção
    public void AssociarLivro(Livro livro)
    {
        if (livro is null)
            throw new ArgumentException("O livro informado é obrigatório.", nameof(livro));

        _livros.Add(new LivroAutor(livro, this));
    }

    // Método estático privado para centralizar a validação dos atributos do domínio
    private static void Validar(string nome, string nacionalidade, DateTime dataNascimento)
    {
        // Verifica se o nome está em branco, nulo ou contém apenas espaços
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome do autor é obrigatório.", nameof(nome));

        // Verifica se a nacionalidade foi informada
        if (string.IsNullOrWhiteSpace(nacionalidade))
            throw new ArgumentException("A nacionalidade do autor é obrigatória.", nameof(nacionalidade));

        // Impede o cadastro de autores com data de nascimento no futuro
        if (dataNascimento.Date > DateTime.Today)
            throw new ArgumentException("A data de nascimento não pode estar no futuro.", nameof(dataNascimento));
    }
}
