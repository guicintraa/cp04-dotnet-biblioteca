// Importa os DTOs da camada de aplicação
using Biblioteca.API.Aplicacao.Dtos;

// Declara o namespace das interfaces dos serviços de aplicação
namespace Biblioteca.API.Aplicacao.Servicos;

// Interface que expõe os casos de uso de Livros
public interface ILivroServico
{
    // Lista todos os livros com seus autores
    IEnumerable<LivroResponse> ListarTodos();

    // Busca um livro pelo seu identificador
    LivroResponse? BuscarPorId(int id);

    // Orquestra a criação do livro e o vínculo N:N com os autores informados
    LivroResponse Criar(CriarLivroRequest request);

    // Orquestra a atualização do livro e refaz o vínculo com os autores
    LivroResponse? Atualizar(int id, AtualizarLivroRequest request);

    // Remove um livro existente
    bool Remover(int id);

    // Registra um empréstimo dando baixa no estoque
    LivroResponse? RegistrarEmprestimo(int id);
}
