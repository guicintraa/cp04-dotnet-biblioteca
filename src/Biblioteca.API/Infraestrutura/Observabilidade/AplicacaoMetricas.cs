// Importa utilitários de diagnósticos nativos do .NET para métricas e tracing
using System.Diagnostics;
using System.Diagnostics.Metrics;

// Declara o namespace para os componentes de observabilidade da infraestrutura
namespace Biblioteca.API.Infraestrutura.Observabilidade;

// Classe centralizadora de Métricas Customizadas e Traces da aplicação
public static class AplicacaoMetricas
{
    // Define o nome do serviço utilizado para identificação no OpenTelemetry
    public const string NomeServico = "Biblioteca.API";

    // Inicializa o Meter nativo do .NET para registro de métricas customizadas
    public static readonly Meter MeterAplicacao = new(NomeServico, "1.0.0");

    // Contador para registrar o total de cadastros efetuados na biblioteca
    public static readonly Counter<long> CadastrosContador =
        MeterAplicacao.CreateCounter<long>(
            name: "cadastros_total",
            unit: "{cadastros}",
            description: "Contagem total de cadastros (autores e livros) efetuados na API");

    // Contador para registrar o total de empréstimos efetuados na biblioteca
    public static readonly Counter<long> EmprestimosContador =
        MeterAplicacao.CreateCounter<long>(
            name: "emprestimos_total",
            unit: "{emprestimos}",
            description: "Contagem total de empréstimos registrados na API");

    // Inicializa o ActivitySource para criação de Spans manuais de Tracing Distribuído
    public static readonly ActivitySource ActivitySourceAplicacao = new(NomeServico);
}
