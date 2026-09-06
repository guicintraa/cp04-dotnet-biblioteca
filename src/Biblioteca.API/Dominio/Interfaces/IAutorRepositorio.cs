// Importa o namespace que contém as entidades de domínio
using Biblioteca.API.Dominio.Entidades;

// Declara o namespace das interfaces de abstração do domínio
namespace Biblioteca.API.Dominio.Interfaces;

// Declara a interface que define os contratos para acesso e persistência de autores
public interface IAutorRepositorio
{
    // Retorna todos os autores já com os livros associados carregados
    IEnumerable<Autor> ObterTodos();

    // Busca um autor por ID (pode retornar nulo se não achar)
    Autor? ObterPorId(int id);

    // Busca vários autores de uma vez, usado para montar o vínculo N:N
    IEnumerable<Autor> ObterPorIds(IEnumerable<int> ids);

    // Adiciona um novo autor e persiste a alteração
    void Adicionar(Autor autor);

    // Atualiza um autor existente e persiste a alteração
    void Atualizar(Autor autor);

    // Remove um autor e persiste a alteração
    void Remover(Autor autor);
}
