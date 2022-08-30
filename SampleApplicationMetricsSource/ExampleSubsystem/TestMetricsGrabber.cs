using System.Diagnostics.Metrics;
using MetricsSource.MonitoringV2;

namespace SampleApplicationMetricsSource.ExampleSubsystem
{
    public class TestMetricsGrabber : IMetricsGrabberV2 
    {
        private IGaugeMetricUnitValue? m_metric1;
        private IGaugeMetricUnitValue? m_metric2;
        private IGaugeMetricUnitValue m_metricAdded;
        private IGaugeMetricUnitValue m_metricRemoved;
        private IGaugeMetricUnitValue m_metricTotalCurrent;

        public void GrabMonitor(Meter meter, IMetricsSourceV2 metricSource)
        {
            m_metric1!.Value = DateTime.Now.Millisecond;
            m_metric2!.Value = DateTime.Now.Millisecond / 3400.0;
            m_metricAdded.Value = 300;
            m_metricRemoved.Value = 30;
            m_metricTotalCurrent.Value = m_metricAdded.Value - m_metricRemoved.Value;
        }

        public void InitializeGrabber(Meter meter, IMetricsSourceV2 metricSource)
        {
            m_metric1 =  metricSource.RegisterGauge(meter, new TestMetricDto
            {
                FieldKey1 = "Grb_Manual",
                PropKey2 = "Grb_Manual",
            });

            m_metric2 = metricSource.RegisterGauge(meter, new TestMetricDto
            {
                FieldKey1 = "Grb_Manual2",
                PropKey2 = "Grb_Manual2",
            }, -1);

            m_metricAdded = metricSource.RegisterGauge(meter, new QueueParamMetricDto
            {
                QueueName = "ServiceController",
                ParamName = "Added"
            });

            m_metricRemoved = metricSource.RegisterGauge(meter, new QueueParamMetricDto
            {
                QueueName = "ServiceController",
                ParamName = "Removed"
            });

            m_metricTotalCurrent = metricSource.RegisterGauge(meter, new QueueParamMetricDto
            {
                QueueName = "ServiceController",
                ParamName = "TotalCurrent"
            });
        }
    }
}