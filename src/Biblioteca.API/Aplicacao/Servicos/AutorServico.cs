// Importa os DTOs da camada de aplicação
using Biblioteca.API.Aplicacao.Dtos;
// Importa as entidades de domínio
using Biblioteca.API.Dominio.Entidades;
// Importa os contratos de repositório do domínio
using Biblioteca.API.Dominio.Interfaces;

// Declara o namespace dos serviços da camada de aplicação
namespace Biblioteca.API.Aplicacao.Servicos;

// Implementa a lógica e orquestração dos casos de uso de Autores
public class AutorServico : IAutorServico
{
    // Declara o campo privado e somente leitura para armazenar o repositório
    private readonly IAutorRepositorio _repositorio;

    // Recebe a implementação do repositório via Injeção de Dependência
    public AutorServico(IAutorRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    // Repassa a listagem ao repositório e converte as entidades em DTOs
    public IEnumerable<AutorResponse> ListarTodos() =>
        _repositorio.ObterTodos().Select(Mapear).ToList();

    // Busca por ID e converte em DTO quando o autor existir
    public AutorResponse? BuscarPorId(int id)
    {
        var autor = _repositorio.ObterPorId(id);

        return autor is null ? null : Mapear(autor);
    }

    // Caso de uso de criação: instancia a entidade (que valida) e salva via repositório
    public AutorResponse Criar(string nome, string nacionalidade, DateTime dataNascimento)
    {
        // Instancia a entidade Autor (se as regras falharem, lança exceção aqui)
        var autor = new Autor(nome, nacionalidade, dataNascimento);

        // Envia a entidade validada para ser gravada pelo repositório
        _repositorio.Adicionar(autor);

        return Mapear(autor);
    }

    // Caso de uso de atualização: carrega, aplica as regras e persiste
    public AutorResponse? Atualizar(int id, string nome, string nacionalidade, DateTime dataNascimento)
    {
        var autor = _repositorio.ObterPorId(id);

        if (autor is null)
            return null;

        autor.Atualizar(nome, nacionalidade, dataNascimento);
        _repositorio.Atualizar(autor);

        return Mapear(autor);
    }

    // Caso de uso de exclusão: retorna false quando o autor não existe
    public bool Remover(int id)
    {
        var autor = _repositorio.ObterPorId(id);

        if (autor is null)
            return false;

        _repositorio.Remover(autor);

        return true;
    }

    // Converte a entidade de domínio no DTO de saída, incluindo os livros do N:N
    private static AutorResponse Mapear(Autor autor) => new(
        autor.Id,
        autor.Nome,
        autor.Nacionalidade,
        autor.DataNascimento,
        autor.Livros
             .Where(la => la.Livro is not null)
             .Select(la => new LivroResumoResponse(
                 la.Livro!.Id, la.Livro.Titulo, la.Livro.Isbn, la.Livro.Preco, la.Livro.Estoque))
             .ToList());
}
