using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace Biblioteca.API.Endpoints;

// Agrupa o mapeamento dos endpoints nativos de diagnóstico (Health Checks)
// e a serialização detalhada do relatório de saúde em JSON.
public static class DiagnosticosEndpoints
{
    public static IEndpointRouteBuilder MapDiagnosticosEndpoints(this IEndpointRouteBuilder rotas)
    {
        // Relatório completo, com todas as verificações registradas
        rotas.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = EscreverRespostaSaude
        });

        // Sonda de liveness: responde apenas se o processo está no ar
        rotas.MapHealthChecks("/health/vivo", new HealthCheckOptions
        {
            Predicate = registro => registro.Tags.Contains("vivo"),
            ResponseWriter = EscreverRespostaSaude
        });

        // Sonda de readiness: inclui a checagem do banco de dados
        rotas.MapHealthChecks("/health/pronto", new HealthCheckOptions
        {
            Predicate = registro => registro.Tags.Contains("pronto"),
            ResponseWriter = EscreverRespostaSaude
        });

        return rotas;
    }

    // Serializa o relatório de saúde em JSON legível no corpo da resposta
    public static async Task EscreverRespostaSaude(HttpContext contexto, HealthReport relatorio)
    {
        contexto.Response.ContentType = "application/json; charset=utf-8";

        var resposta = new
        {
            status = relatorio.Status.ToString(),
            duracaoTotalMs = relatorio.TotalDuration.TotalMilliseconds,
            verificadoEm = DateTime.UtcNow,
            verificacoes = relatorio.Entries.Select(entrada => new
            {
                nome = entrada.Key,
                status = entrada.Value.Status.ToString(),
                descricao = entrada.Value.Description,
                duracaoMs = entrada.Value.Duration.TotalMilliseconds,
                dados = entrada.Value.Data
            })
        };

        await contexto.Response.WriteAsync(
            JsonSerializer.Serialize(resposta, new JsonSerializerOptions { WriteIndented = true }));
    }
}
