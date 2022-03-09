using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using APIRegioes.Data;
using APIRegioes.Tracing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<RegioesRepository>();

builder.Services.AddDbContext<RegioesContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("BaseDadosGeograficos"));
    if (builder.Environment.IsDevelopment())
        options.EnableSensitiveDataLogging();
});

// Documentacao do OpenTelemetry:
// https://opentelemetry.io/docs/instrumentation/net/getting-started/

// Integracao do OpenTelemetry com Jaeger:
// https://opentelemetry.io/docs/instrumentation/net/exporters/

// SqlClient Instrumentation for OpenTelemetry
// https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Instrumentation.SqlClient/README.md

// Documentacaoo do Jaeger:
// https://www.jaegertracing.io/docs/1.28/

builder.Services.AddOpenTelemetryTracing(traceProvider =>
{
    traceProvider
        .AddSource(OpenTelemetryExtensions.ServiceName)
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService(serviceName: OpenTelemetryExtensions.ServiceName,
                    serviceVersion: OpenTelemetryExtensions.ServiceVersion))
        .AddAspNetCoreInstrumentation()
        .AddSqlClientInstrumentation(
            options => options.SetDbStatementForText = true)
        .AddJaegerExporter(exporter =>
        {
            exporter.AgentHost = builder.Configuration["Jaeger:AgentHost"];
            exporter.AgentPort = Convert.ToInt32(builder.Configuration["Jaeger:AgentPort"]);
        });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();