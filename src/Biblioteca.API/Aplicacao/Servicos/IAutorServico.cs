// Importa os DTOs da camada de aplicação
using Biblioteca.API.Aplicacao.Dtos;

// Declara o namespace das interfaces dos serviços de aplicação
namespace Biblioteca.API.Aplicacao.Servicos;

// Interface que expõe os casos de uso de Autores
public interface IAutorServico
{
    // Lista todos os autores com seus livros
    IEnumerable<AutorResponse> ListarTodos();

    // Busca um autor pelo seu identificador
    AutorResponse? BuscarPorId(int id);

    // Orquestra a criação e validação de um novo autor
    AutorResponse Criar(string nome, string nacionalidade, DateTime dataNascimento);

    // Orquestra a atualização de um autor existente
    AutorResponse? Atualizar(int id, string nome, string nacionalidade, DateTime dataNascimento);

    // Remove um autor existente
    bool Remover(int id);
}
