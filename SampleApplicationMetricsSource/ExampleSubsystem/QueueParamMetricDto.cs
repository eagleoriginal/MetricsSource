using MetricsSource.MonitoringV2;

namespace SampleApplicationMetricsSource.ExampleSubsystem
{
    [MetricDescriptionAttributeV2("QueueParam")]
    public class QueueParamMetricDto : BaseMetricValueDtoV2
    {
        public string? QueueName { get; set; }
        public string? ParamName { get; set; }
    }
}