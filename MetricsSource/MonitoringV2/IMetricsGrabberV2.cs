using System.Diagnostics.Metrics;

namespace MetricsSource.MonitoringV2
{
    public interface IMetricsGrabberV2 
    {
        void GrabMonitor(Meter meter, IMetricsSourceV2 metricSource);
        void InitializeGrabber(Meter meter, IMetricsSourceV2 metricSource);
    }
}