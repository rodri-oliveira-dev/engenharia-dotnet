using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);

// Em stdio, stdout pertence ao protocolo. Logs devem ir para stderr.
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

[McpServerToolType]
public static class FerramentasArquitetura
{
    private static readonly IReadOnlyDictionary<string, string> Decisoes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["workflow"] = "Prefira fluxo explícito quando etapas e regras são conhecidas.",
            ["agente"] = "Considere agente quando a próxima ação depende de evidência descoberta em execução.",
            ["rag"] = "RAG recupera fontes; não garante que a resposta gerada seja verdadeira."
        };

    [McpServerTool(Name = "consultar_decisao")]
    [Description("Consulta uma orientação local permitida: workflow, agente ou rag.")]
    public static string ConsultarDecisao(
        [Description("Tópico permitido: workflow, agente ou rag.")] string topico)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topico);
        return Decisoes.TryGetValue(topico.Trim(), out var decisao)
            ? decisao
            : "Tópico não encontrado. Valores permitidos: workflow, agente, rag.";
    }
}
