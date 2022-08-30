using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MetricsSource.MonitoringV2
{
    public static class MetricsExtensions
    {
        public const string MetricEndpoint = "/metrics";
        public record ExportToPrometheusConfig(bool Enable, int? ExploredOnPort = null);
        public class OpenTelemetryMetricsConfig
        {
            public static string ConfigSectionName = "OpenTelemetryMetrics";

            public bool Enable { get; set; }

            public bool ExportToConsole { get; set; } = true;
            public ExportToPrometheusConfig ExportToPrometheus { get; set; } = new(false, null);

            public bool EnableRuntimeInstrument { get; set; } = true;
            public bool EnableAspCoreInstrument { get; set; } = true;
            public List<string> Exclude { get; set; } = new();
        }

        // TODO:Prometheus в версии 1.3.0-rc.2 не работает
        public static void UseMetricsServices(this IApplicationBuilder appBuilder)
       {
           var prometheusConfig = appBuilder.ApplicationServices.GetService<IOptions<OpenTelemetryMetricsConfig>>()?.Value
               ?.ExportToPrometheus;
           if (prometheusConfig is { Enable: true })
           {
               if (prometheusConfig.ExploredOnPort.HasValue)
               {
                   appBuilder.UseOpenTelemetryPrometheusScrapingEndpoint(                    
                       context =>
                           context.Request.Path == MetricEndpoint &&
                               context.Connection.LocalPort == prometheusConfig.ExploredOnPort
                       );
               }
               else
               {
                   appBuilder.UseOpenTelemetryPrometheusScrapingEndpoint(
                       context => context.Request.Path == MetricEndpoint);
               }

           }
       }

        public static IServiceCollection AddMetricsServices(this IServiceCollection services, IConfiguration configuration,
            string serviceName,
            string serviceNameSpace,
            string appInstanceName,
            string serviceVersion, Action<MeterProviderBuilder>? configure = null)
        {
            services.TryAddSingleton<MetricsSourceV2>();
            services.TryAddSingleton<IMetricsSourceV2>(provider => provider.GetRequiredService<MetricsSourceV2>());

            var metricsConfig = configuration.GetSection(OpenTelemetryMetricsConfig.ConfigSectionName).Get<OpenTelemetryMetricsConfig>();
            if (metricsConfig == null || metricsConfig.Enable == false)
            {
                return services;
            }
            services.Configure<OpenTelemetryMetricsConfig>(configuration.GetSection(OpenTelemetryMetricsConfig.ConfigSectionName));
            services.AddOpenTelemetryMetrics(
                (builder) =>
                {

                    if (metricsConfig.EnableRuntimeInstrument)
                    {
                        builder.AddRuntimeInstrumentation();
                    }

                    if (metricsConfig.EnableAspCoreInstrument)
                    {
                        builder.AddAspNetCoreInstrumentation();
                    }
                    
                    configure?.Invoke(builder);

                    if (metricsConfig.ExportToConsole)
                    {
                        builder.AddConsoleExporter((options, readerOptions) =>
                        {
                            readerOptions.TemporalityPreference = MetricReaderTemporalityPreference.Delta;
                        });
                    }

                    builder.AddOtlpExporter((options, readerOptions) =>
                            {
                                var section = configuration.GetSection(OpenTelemetryMetricsConfig.ConfigSectionName)
                                    .GetSection(nameof(OtlpExporterOptions));
                                section.Bind(options);
                                readerOptions.TemporalityPreference = MetricReaderTemporalityPreference.Delta;
                            });

                    builder.SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(serviceName: serviceName,
                            serviceNamespace: serviceNameSpace,
                            serviceInstanceId: appInstanceName,
                            serviceVersion: serviceVersion));

                    if (metricsConfig.ExportToPrometheus.Enable)
                    {
                        // TODO: Prometheus в версии 1.3.0-rc.2 не работает
                        /*builder.AddPrometheusExporter(options =>
                        {
                            options.StartHttpListener = true;
                            options.HttpListenerPrefixes = new string[] { $"http://localhost:9184/" };
                            options.ScrapeResponseCacheDurationMilliseconds = 0;
                        });*/
                    }
                    if (metricsConfig.Exclude.Any())
                    {
                        builder.AddView(instrument =>
                        {
                            foreach (var exclude in metricsConfig.Exclude)
                            {
                                if (exclude.EndsWith("*"))
                                {
                                    if (instrument.Meter.Name.StartsWith(exclude.TrimEnd('*'),
                                            StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        return MetricStreamConfiguration.Drop;
                                    }
                                }
                                else
                                {
                                    if (instrument.Meter.Name.Equals(exclude,
                                            StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        return MetricStreamConfiguration.Drop;
                                    }
                                }
                            }

                            return new MetricStreamConfiguration
                            {
                                Name = instrument.Name,
                                Description = instrument.Description
                            };
                        });
                    }
                    
                });

            services.AddOptions<SwaggerGenOptions>().Configure(options =>
                options.DocumentFilter<MetricEndpointSwaggerIncludeDocumentFilter>());

            return services;
        }
    }
}
