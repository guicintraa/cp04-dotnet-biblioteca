// Importa os DTOs da camada de aplicação
using Biblioteca.API.Aplicacao.Dtos;
// Importa os serviços da camada de aplicação
using Biblioteca.API.Aplicacao.Servicos;
// Importa a classe estática de métricas e tracing customizados
using Biblioteca.API.Infraestrutura.Observabilidade;
using System.Diagnostics;

namespace Biblioteca.API.Endpoints;

// Agrupa o mapeamento e os manipuladores das rotas de Livros.
// Assim como em AutoresEndpoints, os manipuladores são métodos nomeados para
// permitir teste unitário direto da camada de endpoint com mocks do serviço.
public static class LivrosEndpoints
{
    // Registra todas as rotas de /api/livros na pipeline
    public static IEndpointRouteBuilder MapLivrosEndpoints(this IEndpointRouteBuilder rotas)
    {
        var grupo = rotas.MapGroup("/api/livros").WithTags("Livros");

        grupo.MapGet("/", ObterTodos).WithName("ObterLivros");
        grupo.MapGet("/{id:int}", ObterPorId).WithName("ObterLivroPorId");
        grupo.MapPost("/", Criar).WithName("CriarLivro");
        grupo.MapPut("/{id:int}", Atualizar).WithName("AtualizarLivro");
        grupo.MapDelete("/{id:int}", Remover).WithName("RemoverLivro");
        grupo.MapPost("/{id:int}/emprestimos", RegistrarEmprestimo).WithName("RegistrarEmprestimo");

        return rotas;
    }

    // GET /api/livros
    public static IResult ObterTodos(ILivroServico servico, ILogger<CategoriaLivros> logger)
    {
        logger.LogInformation("Buscando listagem completa de livros.");

        return Results.Ok(servico.ListarTodos());
    }

    // GET /api/livros/{id}
    public static IResult ObterPorId(int id, ILivroServico servico, ILogger<CategoriaLivros> logger)
    {
        logger.LogInformation("Buscando livro com ID: {LivroId}", id);

        var livro = servico.BuscarPorId(id);

        if (livro is null)
        {
            logger.LogWarning("Livro com ID {LivroId} não foi encontrado.", id);
            return Results.NotFound(new { mensagem = "Livro não encontrado." });
        }

        return Results.Ok(livro);
    }

    // POST /api/livros — endpoint de escrita instrumentado com Span e Métrica
    public static IResult Criar(CriarLivroRequest request, ILivroServico servico, ILogger<CategoriaLivros> logger)
    {
        // Inicia um Span customizado via ActivitySource para rastreamento refinado
        using var activity = AplicacaoMetricas.ActivitySourceAplicacao.StartActivity("CriarLivroEndpoint");
        activity?.SetTag("livro.titulo", request.Titulo);
        activity?.SetTag("livro.isbn", request.Isbn);
        activity?.SetTag("livro.preco", request.Preco);
        activity?.SetTag("livro.autores.quantidade", request.AutoresIds?.Count ?? 0);

        try
        {
            logger.LogInformation("Tentando cadastrar livro: {TituloLivro}", request.Titulo);

            var livroCriado = servico.Criar(request);

            activity?.SetTag("livro.id", livroCriado.Id);

            // Incrementa a métrica customizada de cadastros com tags dimensionais
            AplicacaoMetricas.CadastrosContador.Add(1,
                new KeyValuePair<string, object?>("entidade", "livro"),
                new KeyValuePair<string, object?>("status", "sucesso"));

            logger.LogInformation("Livro {LivroId} cadastrado com {TotalAutores} autor(es).",
                livroCriado.Id, livroCriado.Autores.Count);

            return Results.Created($"/api/livros/{livroCriado.Id}", livroCriado);
        }
        catch (ArgumentException ex)
        {
            // Registra a falha no Span da requisição e incrementa a métrica de erro
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            AplicacaoMetricas.CadastrosContador.Add(1,
                new KeyValuePair<string, object?>("entidade", "livro"),
                new KeyValuePair<string, object?>("status", "erro_validacao"));

            logger.LogError(ex, "Erro de validação ao cadastrar livro: {Mensagem}", ex.Message);

            return Results.BadRequest(new { erro = ex.Message });
        }
    }

    // PUT /api/livros/{id}
    public static IResult Atualizar(int id, AtualizarLivroRequest request, ILivroServico servico, ILogger<CategoriaLivros> logger)
    {
        using var activity = AplicacaoMetricas.ActivitySourceAplicacao.StartActivity("AtualizarLivroEndpoint");
        activity?.SetTag("livro.id", id);

        try
        {
            var livro = servico.Atualizar(id, request);

            if (livro is null)
            {
                logger.LogWarning("Livro com ID {LivroId} não foi encontrado para atualização.", id);
                return Results.NotFound(new { mensagem = "Livro não encontrado." });
            }

            logger.LogInformation("Livro {LivroId} atualizado com sucesso.", id);

            return Results.Ok(livro);
        }
        catch (ArgumentException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "Erro de validação ao atualizar livro: {Mensagem}", ex.Message);

            return Results.BadRequest(new { erro = ex.Message });
        }
    }

    // DELETE /api/livros/{id}
    public static IResult Remover(int id, ILivroServico servico, ILogger<CategoriaLivros> logger)
    {
        using var activity = AplicacaoMetricas.ActivitySourceAplicacao.StartActivity("RemoverLivroEndpoint");
        activity?.SetTag("livro.id", id);

        if (!servico.Remover(id))
        {
            logger.LogWarning("Livro com ID {LivroId} não foi encontrado para exclusão.", id);
            return Results.NotFound(new { mensagem = "Livro não encontrado." });
        }

        logger.LogInformation("Livro {LivroId} removido com sucesso.", id);

        return Results.NoContent();
    }

    // POST /api/livros/{id}/emprestimos — instrumentado com Span e métrica de empréstimos
    public static IResult RegistrarEmprestimo(int id, ILivroServico servico, ILogger<CategoriaLivros> logger)
    {
        using var activity = AplicacaoMetricas.ActivitySourceAplicacao.StartActivity("RegistrarEmprestimoEndpoint");
        activity?.SetTag("livro.id", id);

        try
        {
            var livro = servico.RegistrarEmprestimo(id);

            if (livro is null)
            {
                AplicacaoMetricas.EmprestimosContador.Add(1,
                    new KeyValuePair<string, object?>("status", "nao_encontrado"));

                logger.LogWarning("Livro com ID {LivroId} não foi encontrado para empréstimo.", id);

                return Results.NotFound(new { mensagem = "Livro não encontrado." });
            }

            // Incrementa a métrica customizada de empréstimos
            AplicacaoMetricas.EmprestimosContador.Add(1,
                new KeyValuePair<string, object?>("status", "sucesso"));

            logger.LogInformation("Empréstimo registrado para o livro {LivroId}. Estoque restante: {Estoque}.",
                livro.Id, livro.Estoque);

            return Results.Ok(livro);
        }
        catch (InvalidOperationException ex)
        {
            // Sem exemplares disponíveis: marca o Span como erro e dimensiona a métrica
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            AplicacaoMetricas.EmprestimosContador.Add(1,
                new KeyValuePair<string, object?>("status", "sem_estoque"));

            logger.LogError(ex, "Empréstimo negado para o livro {LivroId}: {Mensagem}", id, ex.Message);

            return Results.BadRequest(new { erro = ex.Message });
        }
    }
}
