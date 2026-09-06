// Declara o namespace dos Objetos de Transferência de Dados
namespace Biblioteca.API.Aplicacao.Dtos;

// Record imutável responsável por receber o payload JSON do POST de autores
public record CriarAutorRequest(string Nome, string Nacionalidade, DateTime DataNascimento);

// Record imutável responsável por receber o payload JSON do PUT de autores
public record AtualizarAutorRequest(string Nome, string Nacionalidade, DateTime DataNascimento);

// Resumo de um livro exibido dentro da listagem de autores
public record LivroResumoResponse(int Id, string Titulo, string Isbn, decimal Preco, int Estoque);

// Representação de saída de um autor com os livros associados (relacionamento N:N)
public record AutorResponse(
    int Id,
    string Nome,
    string Nacionalidade,
    DateTime DataNascimento,
    IReadOnlyCollection<LivroResumoResponse> Livros);
