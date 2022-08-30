namespace MetricsSource.MonitoringV2
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class MetricDescriptionAttributeV2 : Attribute
    {
        public string MetricName { get; }
        public string? MetricHelp { get; set; }

        public MetricDescriptionAttributeV2(string metricName)
        {
            MetricName = metricName;
        }
    }
}