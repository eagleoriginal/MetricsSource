using MetricsSource.MonitoringV2;

namespace SampleApplicationMetricsSource.ExampleSubsystem
{
    [MetricDescriptionAttributeV2("SomeTestMetric")]
    public class TestMetricDto : BaseMetricValueDtoV2
    {
        public string? FieldKey1;
        public string? PropKey2 { get; set; }
    }
}