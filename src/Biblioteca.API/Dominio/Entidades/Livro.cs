// Declara o namespace correspondente à estrutura de pastas do domínio
namespace Biblioteca.API.Dominio.Entidades;

// Entidade de domínio Livro. Lado direito do relacionamento N:N com Autor.
public class Livro
{
    // Coleção interna das associações do livro com seus autores
    private readonly List<LivroAutor> _autores = new();

    // Construtor sem parâmetros exigido pelo Entity Framework Core
    protected Livro()
    {
        Titulo = string.Empty;
        Isbn = string.Empty;
    }

    // Construtor público para criar novas instâncias válidas de um Livro
    public Livro(string titulo, string isbn, decimal preco, int estoque, int anoPublicacao)
    {
        // Executa a validação das regras de negócio antes de preencher as propriedades
        Validar(titulo, isbn, preco, estoque, anoPublicacao);

        Titulo = titulo.Trim();
        Isbn = isbn.Trim();
        Preco = preco;
        Estoque = estoque;
        AnoPublicacao = anoPublicacao;
    }

    // Identificador gerado pela sequência (IDENTITY) da tabela TB_LIVROS
    public int Id { get; private set; }

    // Título da obra
    public string Titulo { get; private set; }

    // Código ISBN, único no acervo
    public string Isbn { get; private set; }

    // Preço unitário do exemplar
    public decimal Preco { get; private set; }

    // Quantidade de exemplares disponíveis para empréstimo
    public int Estoque { get; private set; }

    // Ano em que a obra foi publicada
    public int AnoPublicacao { get; private set; }

    // Navegação somente leitura para os autores associados (relacionamento N:N)
    public IReadOnlyCollection<LivroAutor> Autores => _autores.AsReadOnly();

    // Atualiza os dados do livro reaproveitando as mesmas validações
    public void Atualizar(string titulo, string isbn, decimal preco, int estoque, int anoPublicacao)
    {
        Validar(titulo, isbn, preco, estoque, anoPublicacao);

        Titulo = titulo.Trim();
        Isbn = isbn.Trim();
        Preco = preco;
        Estoque = estoque;
        AnoPublicacao = anoPublicacao;
    }

    // Cria a associação entre este livro e um autor na tabela de junção
    public void AssociarAutor(Autor autor)
    {
        if (autor is null)
            throw new ArgumentException("O autor informado é obrigatório.", nameof(autor));

        _autores.Add(new LivroAutor(this, autor));
    }

    // Remove todas as associações de autoria (usado ao refazer os vínculos na edição)
    public void LimparAutores() => _autores.Clear();

    // Regra de negócio do empréstimo: dá baixa de um exemplar no estoque
    public void RegistrarEmprestimo()
    {
        if (Estoque <= 0)
            throw new InvalidOperationException($"Não há exemplares disponíveis do livro '{Titulo}'.");

        Estoque--;
    }

    // Método estático privado para centralizar a validação dos atributos do domínio
    private static void Validar(string titulo, string isbn, decimal preco, int estoque, int anoPublicacao)
    {
        // Verifica se o título está em branco, nulo ou contém apenas espaços
        if (string.IsNullOrWhiteSpace(titulo))
            throw new ArgumentException("O título do livro é obrigatório.", nameof(titulo));

        // Verifica se o ISBN foi informado
        if (string.IsNullOrWhiteSpace(isbn))
            throw new ArgumentException("O ISBN do livro é obrigatório.", nameof(isbn));

        // Verifica se o preço é menor ou igual a zero
        if (preco <= 0)
            throw new ArgumentException("O preço do livro deve ser maior que zero.", nameof(preco));

        // Verifica se a quantidade de estoque é um valor negativo
        if (estoque < 0)
            throw new ArgumentException("O estoque não pode ser negativo.", nameof(estoque));

        // Verifica se o ano de publicação é plausível
        if (anoPublicacao < 1450 || anoPublicacao > DateTime.Today.Year + 1)
            throw new ArgumentException("O ano de publicação informado é inválido.", nameof(anoPublicacao));
    }
}
