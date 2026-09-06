// Importa os DTOs da camada de aplicação
using Biblioteca.API.Aplicacao.Dtos;
// Importa os serviços da camada de aplicação
using Biblioteca.API.Aplicacao.Servicos;
// Importa a classe estática de métricas e tracing customizados
using Biblioteca.API.Infraestrutura.Observabilidade;
using System.Diagnostics;

namespace Biblioteca.API.Endpoints;

// Agrupa o mapeamento e os manipuladores das rotas de Autores.
// Os manipuladores são métodos nomeados (e não lambdas anônimas dentro do
// Program.cs) justamente para que possam ser chamados diretamente pelos
// testes unitários, com os serviços substituídos por mocks.
public static class AutoresEndpoints
{
    // Registra todas as rotas de /api/autores na pipeline
    public static IEndpointRouteBuilder MapAutoresEndpoints(this IEndpointRouteBuilder rotas)
    {
        var grupo = rotas.MapGroup("/api/autores").WithTags("Autores");

        grupo.MapGet("/", ObterTodos).WithName("ObterAutores");
        grupo.MapGet("/{id:int}", ObterPorId).WithName("ObterAutorPorId");
        grupo.MapPost("/", Criar).WithName("CriarAutor");
        grupo.MapPut("/{id:int}", Atualizar).WithName("AtualizarAutor");
        grupo.MapDelete("/{id:int}", Remover).WithName("RemoverAutor");

        return rotas;
    }

    // GET /api/autores
    public static IResult ObterTodos(IAutorServico servico, ILogger<CategoriaAutores> logger)
    {
        // Grava log de informação antes de processar a consulta
        logger.LogInformation("Buscando listagem completa de autores.");

        return Results.Ok(servico.ListarTodos());
    }

    // GET /api/autores/{id}
    public static IResult ObterPorId(int id, IAutorServico servico, ILogger<CategoriaAutores> logger)
    {
        // Grava log estruturado contendo o parâmetro da busca
        logger.LogInformation("Buscando autor com ID: {AutorId}", id);

        var autor = servico.BuscarPorId(id);

        if (autor is null)
        {
            // Grava log de aviso indicando recurso não encontrado
            logger.LogWarning("Autor com ID {AutorId} não foi encontrado.", id);
            return Results.NotFound(new { mensagem = "Autor não encontrado." });
        }

        return Results.Ok(autor);
    }

    // POST /api/autores — endpoint de escrita instrumentado com Span e Métrica
    public static IResult Criar(CriarAutorRequest request, IAutorServico servico, ILogger<CategoriaAutores> logger)
    {
        // Inicia um Span customizado via ActivitySource para rastreamento refinado
        using var activity = AplicacaoMetricas.ActivitySourceAplicacao.StartActivity("CriarAutorEndpoint");
        activity?.SetTag("autor.nome", request.Nome);
        activity?.SetTag("autor.nacionalidade", request.Nacionalidade);

        try
        {
            logger.LogInformation("Tentando cadastrar autor: {NomeAutor}", request.Nome);

            var autorCriado = servico.Criar(request.Nome, request.Nacionalidade, request.DataNascimento);

            activity?.SetTag("autor.id", autorCriado.Id);

            // Incrementa a métrica customizada de cadastros com tags dimensionais
            AplicacaoMetricas.CadastrosContador.Add(1,
                new KeyValuePair<string, object?>("entidade", "autor"),
                new KeyValuePair<string, object?>("status", "sucesso"));

            logger.LogInformation("Autor {AutorId} cadastrado com sucesso.", autorCriado.Id);

            return Results.Created($"/api/autores/{autorCriado.Id}", autorCriado);
        }
        catch (ArgumentException ex)
        {
            // Registra a falha no Span da requisição e incrementa a métrica de erro
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            AplicacaoMetricas.CadastrosContador.Add(1,
                new KeyValuePair<string, object?>("entidade", "autor"),
                new KeyValuePair<string, object?>("status", "erro_validacao"));

            logger.LogError(ex, "Erro de validação ao cadastrar autor: {Mensagem}", ex.Message);

            return Results.BadRequest(new { erro = ex.Message });
        }
    }

    // PUT /api/autores/{id}
    public static IResult Atualizar(int id, AtualizarAutorRequest request, IAutorServico servico, ILogger<CategoriaAutores> logger)
    {
        using var activity = AplicacaoMetricas.ActivitySourceAplicacao.StartActivity("AtualizarAutorEndpoint");
        activity?.SetTag("autor.id", id);

        try
        {
            var autor = servico.Atualizar(id, request.Nome, request.Nacionalidade, request.DataNascimento);

            if (autor is null)
            {
                logger.LogWarning("Autor com ID {AutorId} não foi encontrado para atualização.", id);
                return Results.NotFound(new { mensagem = "Autor não encontrado." });
            }

            logger.LogInformation("Autor {AutorId} atualizado com sucesso.", id);

            return Results.Ok(autor);
        }
        catch (ArgumentException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "Erro de validação ao atualizar autor: {Mensagem}", ex.Message);

            return Results.BadRequest(new { erro = ex.Message });
        }
    }

    // DELETE /api/autores/{id}
    public static IResult Remover(int id, IAutorServico servico, ILogger<CategoriaAutores> logger)
    {
        using var activity = AplicacaoMetricas.ActivitySourceAplicacao.StartActivity("RemoverAutorEndpoint");
        activity?.SetTag("autor.id", id);

        if (!servico.Remover(id))
        {
            logger.LogWarning("Autor com ID {AutorId} não foi encontrado para exclusão.", id);
            return Results.NotFound(new { mensagem = "Autor não encontrado." });
        }

        logger.LogInformation("Autor {AutorId} removido com sucesso.", id);

        return Results.NoContent();
    }
}
