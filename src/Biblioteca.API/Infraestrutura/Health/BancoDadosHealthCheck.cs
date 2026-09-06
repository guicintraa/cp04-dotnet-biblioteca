// Importa o namespace nativo de diagnósticos da Microsoft
using Microsoft.Extensions.Diagnostics.HealthChecks;
// Importa o contexto do Entity Framework para validar a conexão real
using Biblioteca.API.Infraestrutura.Contexto;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

// Declara o namespace para os componentes de infraestrutura de saúde
namespace Biblioteca.API.Infraestrutura.Health;

// Implementa IHealthCheck para validar a conexão com o banco de dados Oracle
public class BancoDadosHealthCheck : IHealthCheck
{
    // Acima deste tempo de resposta o banco é considerado degradado
    private const int LimiteDegradadoMs = 1500;

    // Contexto do EF Core utilizado para sondar o banco
    private readonly BibliotecaContexto _contexto;

    // Configuração usada para permitir a simulação de falha durante a demonstração
    private readonly IConfiguration _configuracao;

    public BancoDadosHealthCheck(BibliotecaContexto contexto, IConfiguration configuracao)
    {
        _contexto = contexto;
        _configuracao = configuracao;
    }

    // Executa assincronamente a verificação da saúde da dependência
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Permite forçar a falha pela chave Biblioteca:SimularFalhaBanco (demonstração em vídeo)
        if (_configuracao.GetValue<bool>("Biblioteca:SimularFalhaBanco"))
        {
            return HealthCheckResult.Unhealthy(
                "Falha ao conectar no Banco de Dados (Oracle). [falha simulada]");
        }

        // Cronometra a sondagem para reportar a latência junto do status
        var cronometro = Stopwatch.StartNew();

        try
        {
            // Tenta efetivamente abrir a conexão com o banco configurado
            bool conexaoOk = await _contexto.Database.CanConnectAsync(cancellationToken);
            cronometro.Stop();

            // Metadados adicionais devolvidos no corpo do /health
            var dados = new Dictionary<string, object>
            {
                { "Provedor", _contexto.Database.ProviderName ?? "desconhecido" },
                { "LatenciaMs", cronometro.ElapsedMilliseconds }
            };

            // Retorna status Unhealthy (Insaudável) alertando falha na infraestrutura
            if (!conexaoOk)
            {
                return HealthCheckResult.Unhealthy(
                    "Falha ao conectar no Banco de Dados (Oracle).", data: dados);
            }

            // Retorna status Degraded (Degradado) quando o banco responde lentamente
            if (cronometro.ElapsedMilliseconds > LimiteDegradadoMs)
            {
                return HealthCheckResult.Degraded(
                    $"Banco de Dados respondeu de forma lenta ({cronometro.ElapsedMilliseconds} ms).",
                    data: dados);
            }

            // Retorna status Healthy (Saudável) com metadados adicionais de latência
            return HealthCheckResult.Healthy(
                "Conexão com o Banco de Dados estabelecida com sucesso.", data: dados);
        }
        catch (Exception excecao)
        {
            cronometro.Stop();

            // Qualquer exceção na sondagem também caracteriza indisponibilidade
            return HealthCheckResult.Unhealthy(
                "Exceção ao validar a conexão com o Banco de Dados (Oracle).",
                exception: excecao,
                data: new Dictionary<string, object>
                {
                    { "Erro", excecao.Message },
                    { "LatenciaMs", cronometro.ElapsedMilliseconds }
                });
        }
    }
}
