// Importa o namespace que contém as entidades de domínio
using Biblioteca.API.Dominio.Entidades;

// Declara o namespace das interfaces de abstração do domínio
namespace Biblioteca.API.Dominio.Interfaces;

// Declara a interface que define os contratos para acesso e persistência de livros
public interface ILivroRepositorio
{
    // Retorna todos os livros já com os autores associados carregados
    IEnumerable<Livro> ObterTodos();

    // Busca um livro por ID (pode retornar nulo se não achar)
    Livro? ObterPorId(int id);

    // Informa se já existe um livro cadastrado com o ISBN informado
    bool ExisteIsbn(string isbn);

    // Adiciona um novo livro e persiste a alteração
    void Adicionar(Livro livro);

    // Atualiza um livro existente e persiste a alteração
    void Atualizar(Livro livro);

    // Remove um livro e persiste a alteração
    void Remover(Livro livro);
}
