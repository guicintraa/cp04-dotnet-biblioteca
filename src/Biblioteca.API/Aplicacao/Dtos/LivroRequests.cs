// Declara o namespace dos Objetos de Transferência de Dados
namespace Biblioteca.API.Aplicacao.Dtos;

// Record imutável responsável por receber o payload JSON do POST de livros
public record CriarLivroRequest(
    string Titulo,
    string Isbn,
    decimal Preco,
    int Estoque,
    int AnoPublicacao,
    IReadOnlyCollection<int> AutoresIds);

// Record imutável responsável por receber o payload JSON do PUT de livros
public record AtualizarLivroRequest(
    string Titulo,
    string Isbn,
    decimal Preco,
    int Estoque,
    int AnoPublicacao,
    IReadOnlyCollection<int> AutoresIds);

// Resumo de um autor exibido dentro da listagem de livros
public record AutorResumoResponse(int Id, string Nome, string Nacionalidade);

// Representação de saída de um livro com os autores associados (relacionamento N:N)
public record LivroResponse(
    int Id,
    string Titulo,
    string Isbn,
    decimal Preco,
    int Estoque,
    int AnoPublicacao,
    IReadOnlyCollection<AutorResumoResponse> Autores);
