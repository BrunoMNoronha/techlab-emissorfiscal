using System.Net;
using System.Text.Json;

var inContainer = Environment.GetEnvironmentVariable("MONITOR_IN_CONTAINER") == "1";
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot")
});
builder.WebHost.UseUrls(inContainer ? "http://0.0.0.0:5088" : "http://127.0.0.1:5088");
var app = builder.Build();
var monitor = new ExecutionMonitor();

app.Use(async (context, next) =>
{
    // No Docker, a porta é publicada apenas no loopback do host pelo Compose.
    // O IP visto pelo contêiner é o da ponte Docker, não o loopback do host.
    if ((!inContainer && !IPAddress.IsLoopback(context.Connection.RemoteIpAddress ?? IPAddress.None)) ||
        context.Request.Host.Value != "127.0.0.1:5088" ||
        (HttpMethods.IsPost(context.Request.Method) && context.Request.Headers.Origin != "http://127.0.0.1:5088"))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }
    await next();
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/api/execution", () => Results.Ok(monitor.Snapshot()));
app.MapPost("/api/execution/{kind}", (string kind) => monitor.Start(kind) switch
{
    StartResult.Started => Results.Accepted(),
    StartResult.Busy => Results.Conflict(new { message = "Já existe uma execução em andamento." }),
    _ => Results.BadRequest(new { message = "Fluxo desconhecido." })
});
app.Run();

enum StartResult { Started, Busy, Invalid }
record Step(DateTimeOffset At, string Message);
record Snapshot(string Status, string? Kind, DateTimeOffset? StartedAt, DateTimeOffset? FinishedAt,
    Step[] Steps, string? Result, string? Error, string? OutputDirectory);

sealed class ExecutionMonitor
{
    private static readonly Dictionary<string, (string Sale, string Emitter)> Cases = new(StringComparer.Ordinal)
    {
        ["crt1"] = ("sale-crt1.json", "emitter-crt1.json"),
        ["crt3"] = ("sale-crt3.json", "emitter-crt3.json"),
        ["st-crt1"] = ("sale-st-crt1.json", "emitter-crt1.json"),
        ["exempt-crt1"] = ("sale-exempt-crt1.json", "emitter-crt1.json"),
        ["red-crt3"] = ("sale-red-crt3.json", "emitter-crt3.json"),
        ["basket-crt3"] = ("sale-basket-crt3.json", "emitter-crt3.json")
    };
    private readonly object gate = new();
    private string status = "idle";
    private string? kind, result, error, outputDirectory;
    private DateTimeOffset? startedAt, finishedAt;
    private readonly List<Step> steps = [];

    public Snapshot Snapshot()
    {
        lock (gate) return new(status, kind, startedAt, finishedAt, [.. steps], result, error, outputDirectory);
    }

    public StartResult Start(string requested)
    {
        if (requested is not ("schemas" or "demo") && !Cases.ContainsKey(requested)) return StartResult.Invalid;
        lock (gate)
        {
            if (status == "running") return StartResult.Busy;
            kind = requested; status = "running"; startedAt = DateTimeOffset.UtcNow; finishedAt = null;
            result = error = outputDirectory = null; steps.Clear();
        }
        _ = Task.Run(() => Execute(requested));
        return StartResult.Started;
    }

    private void Execute(string requested)
    {
        try
        {
            AddStep("Execução iniciada");
            var output = new List<JsonElement>();
            string? cliError = null;
            void Capture(string json) => output.Add(JsonSerializer.Deserialize<JsonElement>(json));
            void CaptureError(string message) => cliError = message;
            int exit;
            if (Cases.TryGetValue(requested, out var input))
            {
                var examples = Path.Combine(AppContext.BaseDirectory, "samples", "preparation");
                var root = Path.Combine(Directory.GetCurrentDirectory(), ".validation", "monitor", Guid.NewGuid().ToString("N"));
                var prepared = Path.Combine(root, "preparacao");
                var danfe = Path.Combine(root, "danfe");
                lock (gate) outputDirectory = root;
                exit = CliRunner.Run(["prepare", Path.Combine(examples, input.Sale), Path.Combine(examples, input.Emitter),
                    Path.Combine(examples, "test-context.json"), prepared], AddStep, Capture, CaptureError);
                if (exit == 0)
                {
                    AddStep("Preparação concluída; iniciando DANFE sintético");
                    exit = CliRunner.Run(["danfe", Path.Combine(prepared, "signed.xml"), danfe], AddStep, Capture, CaptureError);
                }
            }
            else exit = CliRunner.Run([requested], AddStep, Capture, CaptureError);
            lock (gate)
            {
                result = output.Count == 0 ? null : JsonSerializer.Serialize(output.Count == 1 ? (object)output[0] : output,
                    new JsonSerializerOptions { WriteIndented = true });
                status = exit == 0 ? "succeeded" : "failed";
                error = exit == 0 ? null : cliError ?? $"A execução terminou com código {exit}.";
                steps.Add(new(DateTimeOffset.UtcNow, exit == 0 ? "Execução concluída" : "Execução interrompida por erro"));
                finishedAt = DateTimeOffset.UtcNow;
            }
        }
        catch (Exception exception)
        {
            lock (gate)
            {
                status = "failed"; error = $"{exception.GetType().Name}: {exception.Message}";
                steps.Add(new(DateTimeOffset.UtcNow, "Execução interrompida por erro"));
                finishedAt = DateTimeOffset.UtcNow;
            }
        }
    }

    private void AddStep(string message) { lock (gate) steps.Add(new(DateTimeOffset.UtcNow, message)); }
}
