using System.Diagnostics.Metrics;
using MetricsSource.MonitoringV2;

namespace SampleApplicationMetricsSource.ExampleSubsystem
{
    public class MetricsBackgroundService : BackgroundService
    {
        private readonly IMetricsSourceV2 m_metricsSourceV2;

        public MetricsBackgroundService(IMetricsSourceV2 metricsSource)
        {
            m_metricsSourceV2 = metricsSource;
        }

        // !!! Важно MyCompany.MyProduct.MyLibrary - надо зарегистрировать в Setup
        public Meter MyMeter = new("MyCompany.MyProduct.MyLibrary", "1.0");
        public Meter MyMeterExclude1 = new("MyCompany.MyProduct.Exclude", "1.0");
        public Meter MyMeterExclude2 = new("MyCompany.MyProduct.OtherExclude", "1.0");

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Синхронный Gauge которому можно присваивать значение в любое время.
            var manualGaugeValueSetter = m_metricsSourceV2.RegisterGauge(MyMeter, new TestMetricDto
            {
                FieldKey1 = "Param1",
                PropKey2 = "Param2"
            }, 100);

            // Асинхронная метрика Gauge, которая сама себя обновляет вызовом Callback 
            // Все отличие от
            // MyMeter.CreateObservableGauge("SomeMetricName", () => new List<Measurement<double>>() { new (10050, new List<KeyValuePair<string, object>>{ new("key1", "Name1"), new("key2", "Name2") }!) });
            // В структурированности тэгов метрики представленной типом TestMetricDto. 
            var registeredGaugeObserv = m_metricsSourceV2.RegisterGaugeObservable(MyMeter, new TestMetricDto
            {
                FieldKey1 = "Observable",
                PropKey2 = "Param2"
            }, () => DateTime.Now.Second);

            // Пример регистрации двух метрик которые в итоге просумируются и превратяться в одну.
            // Эти метрики могут быть только асинхронные, т.е. обновлятсья через Callback.
            var registeredGaugeCum1 = m_metricsSourceV2.RegisterGaugeCumulative(MyMeter, new TestMetricDto
            {
                FieldKey1 = "Cumul1",
                PropKey2 = "Cumul2"
            }, () => DateTime.Now.Second);
            var registeredGaugeCum2 = m_metricsSourceV2.RegisterGaugeCumulative(MyMeter, new TestMetricDto
            {
                FieldKey1 = "Cumul1",
                PropKey2 = "Cumul2"
            }, () => DateTime.Now.Second);


            m_metricsSourceV2.RegisterGaugeObservable(MyMeterExclude1, new TestMetricDto
            {
                FieldKey1 = "lhlkherthert",
                PropKey2 = "lgkertlkherterht"
            }, () => DateTime.Now.Second);

            m_metricsSourceV2.RegisterGaugeObservable(MyMeterExclude2, new TestMetricDto
            {
                FieldKey1 = "495835-9345-098345",
                PropKey2 = "098525098230498234"
            }, () => DateTime.Now.Second);
            
            // Пример регистрации некоего Граббера который сам зарегистрирует метрики, сам будет грабить потенциального Owner-а с целью обновить значения синхронных Gauge.
            m_metricsSourceV2.RegisterMetricsGrabber(MyMeter, new TestMetricsGrabber());

            while (stoppingToken.IsCancellationRequested == false)
            {
                await Task.Delay(15000, stoppingToken);
                manualGaugeValueSetter.Value = 233;
                registeredGaugeObserv.Dispose();
                registeredGaugeCum1.Dispose();
                Console.WriteLine("Disposed");
            }
            
            MyMeter.Dispose();
        }
    }
}
