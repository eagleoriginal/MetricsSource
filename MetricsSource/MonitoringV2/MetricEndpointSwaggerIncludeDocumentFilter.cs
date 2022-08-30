using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MetricsSource.MonitoringV2;

/// <summary>
/// Для генерации Swagger
/// </summary>
public class MetricEndpointSwaggerIncludeDocumentFilter : IDocumentFilter
{
    private readonly IServiceProvider m_services;
    
    public MetricEndpointSwaggerIncludeDocumentFilter(IServiceProvider services)
    {
        m_services = services;
    }

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var metricsConfig = m_services.GetService<IOptions< MetricsExtensions.OpenTelemetryMetricsConfig>>();
        
        if (false == metricsConfig is { Value.ExportToPrometheus.Enable: true })
        {
            return;
        }

        var apiOperation = new OpenApiOperation
        {
            Callbacks = null,
            Deprecated = false,
            Description = "Метрики",
            Extensions = new ConcurrentDictionary<string, IOpenApiExtension>(),
            ExternalDocs = null,
            OperationId = "metrics",
            Parameters = null,
            RequestBody = null,
            Responses = new OpenApiResponses()
                {{"200", new OpenApiResponse {Content = null, Description = "Success"}}},
            Security = null,
            Summary = "Метрики производительности",
            Tags = new List<OpenApiTag>() { new() { Name = "Служебное" } }
        };

        if (metricsConfig.Value.ExportToPrometheus.ExploredOnPort != null)
        {
            apiOperation.Extensions.Add("OnlyManagementPortApiTag", new OpenApiString("Yes"));
        }
        
        swaggerDoc.Paths.TryAdd(MetricsExtensions.MetricEndpoint, new OpenApiPathItem
        {
            Description = "Метрики производительности",
            Extensions = null,
            Operations = new Dictionary<OperationType, OpenApiOperation>()
            {
                {OperationType.Get, apiOperation},
            },
            Parameters = null,
            Servers = null,
        });
    }
}