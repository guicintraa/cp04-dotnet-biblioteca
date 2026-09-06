// Importa os DTOs da camada de aplicação
using Biblioteca.API.Aplicacao.Dtos;
// Importa as entidades de domínio
using Biblioteca.API.Dominio.Entidades;
// Importa os contratos de repositório do domínio
using Biblioteca.API.Dominio.Interfaces;

// Declara o namespace dos serviços da camada de aplicação
namespace Biblioteca.API.Aplicacao.Servicos;

// Implementa a lógica e orquestração dos casos de uso de Livros
public class LivroServico : ILivroServico
{
    // Repositório de livros injetado via construtor
    private readonly ILivroRepositorio _livroRepositorio;

    // Repositório de autores, necessário para montar o relacionamento N:N
    private readonly IAutorRepositorio _autorRepositorio;

    // Recebe as implementações dos repositórios via Injeção de Dependência
    public LivroServico(ILivroRepositorio livroRepositorio, IAutorRepositorio autorRepositorio)
    {
        _livroRepositorio = livroRepositorio;
        _autorRepositorio = autorRepositorio;
    }

    // Repassa a listagem ao repositório e converte as entidades em DTOs
    public IEnumerable<LivroResponse> ListarTodos() =>
        _livroRepositorio.ObterTodos().Select(Mapear).ToList();

    // Busca por ID e converte em DTO quando o livro existir
    public LivroResponse? BuscarPorId(int id)
    {
        var livro = _livroRepositorio.ObterPorId(id);

        return livro is null ? null : Mapear(livro);
    }

    // Caso de uso de criação: valida o vínculo N:N, instancia a entidade e salva
    public LivroResponse Criar(CriarLivroRequest request)
    {
        // Um livro do acervo precisa de ao menos um autor responsável
        if (request.AutoresIds is null || request.AutoresIds.Count == 0)
            throw new ArgumentException("O livro deve estar associado a pelo menos um autor.", nameof(request));

        // O ISBN é único no acervo
        if (_livroRepositorio.ExisteIsbn(request.Isbn))
            throw new ArgumentException($"Já existe um livro cadastrado com o ISBN {request.Isbn}.", nameof(request));

        // Carrega os autores informados e confere se todos realmente existem
        var autores = _autorRepositorio.ObterPorIds(request.AutoresIds).ToList();

        if (autores.Count != request.AutoresIds.Distinct().Count())
            throw new ArgumentException("Um ou mais autores informados não foram encontrados.", nameof(request));

        // Instancia a entidade Livro (se as regras falharem, lança exceção aqui)
        var livro = new Livro(request.Titulo, request.Isbn, request.Preco, request.Estoque, request.AnoPublicacao);

        // Monta o relacionamento muitos-para-muitos na tabela de junção
        foreach (var autor in autores)
            livro.AssociarAutor(autor);

        // Envia a entidade validada para ser gravada pelo repositório
        _livroRepositorio.Adicionar(livro);

        return Mapear(livro);
    }

    // Caso de uso de atualização: aplica as regras e, se pedido, refaz o vínculo N:N
    public LivroResponse? Atualizar(int id, AtualizarLivroRequest request)
    {
        var livro = _livroRepositorio.ObterPorId(id);

        if (livro is null)
            return null;

        livro.Atualizar(request.Titulo, request.Isbn, request.Preco, request.Estoque, request.AnoPublicacao);

        if (request.AutoresIds is not null && request.AutoresIds.Count > 0)
        {
            var autores = _autorRepositorio.ObterPorIds(request.AutoresIds).ToList();

            if (autores.Count != request.AutoresIds.Distinct().Count())
                throw new ArgumentException("Um ou mais autores informados não foram encontrados.", nameof(request));

            livro.LimparAutores();

            foreach (var autor in autores)
                livro.AssociarAutor(autor);
        }

        _livroRepositorio.Atualizar(livro);

        return Mapear(livro);
    }

    // Caso de uso de exclusão: retorna false quando o livro não existe
    public bool Remover(int id)
    {
        var livro = _livroRepositorio.ObterPorId(id);

        if (livro is null)
            return false;

        _livroRepositorio.Remover(livro);

        return true;
    }

    // Caso de uso de empréstimo: a regra de estoque vive na entidade de domínio
    public LivroResponse? RegistrarEmprestimo(int id)
    {
        var livro = _livroRepositorio.ObterPorId(id);

        if (livro is null)
            return null;

        // Lança InvalidOperationException quando não há exemplares disponíveis
        livro.RegistrarEmprestimo();

        _livroRepositorio.Atualizar(livro);

        return Mapear(livro);
    }

    // Converte a entidade de domínio no DTO de saída, incluindo os autores do N:N
    private static LivroResponse Mapear(Livro livro) => new(
        livro.Id,
        livro.Titulo,
        livro.Isbn,
        livro.Preco,
        livro.Estoque,
        livro.AnoPublicacao,
        livro.Autores
             .Where(la => la.Autor is not null)
             .Select(la => new AutorResumoResponse(la.Autor!.Id, la.Autor.Nome, la.Autor.Nacionalidade))
             .ToList());
}
