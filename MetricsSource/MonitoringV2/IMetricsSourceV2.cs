using System.Diagnostics.Metrics;

namespace MetricsSource.MonitoringV2;

public interface IMetricsSourceV2
{
    /// <summary>
    /// Зарегистрировать Метрику(Gauge) для типа с заранее предустановленным набором значений Labels.
    /// Значение будет получаться через вызов Callback
    /// </summary>
    /// <typeparam name="T">Тип измерения</typeparam>
    /// <param name="meter">Измерение к которому добавляется метрика</param>
    /// <param name="gaugeDto">Значение с действительсным значениями Lables. Которому будет назначен идентификатор.</param>
    /// <param name="valueObtainer">Кoллбэк путем которого будут поулчаться значения</param>
    /// <returns>Идентификатор Измерения(Gauge) для типа с заранее предустановленным набором значений Labels по которому можно устанавливать новое значение <see langword="double"/></returns>
    IDisposable RegisterGaugeObservable<T>(Meter meter, T gaugeDto, Func<double> valueObtainer) where T : BaseMetricValueDtoV2;

    /// <summary>
    /// Зарегистрировать Метрику(Gauge) с помощью типизированного ДТО представляющего собой набор тэгов и название метрики.
    /// </summary>
    /// <typeparam name="T">Тип определяющий метрику <see cref="BaseMetricValueDtoV2"/></typeparam>
    /// <param name="meter">Измерение к которому добавляется метрика</param>
    /// <param name="gaugeDto">Значение с действительным значениями Labels определяющее имя метрики. Которому будет назначен идентификатор.</param>
    /// <param name="initialValue">Значение метрики по умолчанию</param>
    /// <returns>Сеттер Измерения(Gauge)</returns>
    IGaugeMetricUnitValue RegisterGauge<T>(Meter meter, T gaugeDto, double initialValue = default) where T : BaseMetricValueDtoV2;

    /// <summary>
    /// Зарегистрировать Метрику(Gauge) с помощью явно определенных тэгов и имени метрики.
    /// </summary>
    /// <param name="meter">Измерение к которому добавляется метрика</param>
    /// <param name="metricName">Имя метрики</param>
    /// <param name="gaugeTags">Тэги метрики.</param>
    /// <param name="metricHelp">Описание метрики</param>
    /// <param name="initialValue">Значение метрики по умолчанию</param>
    /// <returns>Сеттер Измерения(Gauge)</returns>
    IGaugeMetricUnitValue RegisterGauge(Meter meter, string metricName,
        List<KeyValuePair<string, object>> gaugeTags,
        string? metricHelp = null, double initialValue = default);

    /// <summary>
    /// Зарегистрировать Метрику(Gauge) которое не уникально, и общее в данном пространстве <see cref="Meter"/>.
    /// Т.е. в разное время разными экземплярами некой службы регистрируется метрика с одинаковым названием и значениями тэгов,
    /// при этом в результирующем наборе экспортируемых метрик будет одна метрика в которой значением является сумма всех метрик.
    /// </summary>
    /// <typeparam name="T">Тип определяющий метрику <see cref="BaseMetricValueDtoV2"/></typeparam>
    /// <param name="meter">Измерение к которому добавляется метрика</param>
    /// <param name="gaugeDto">Значение с действительсным значениями Lables. Которому будет назначен идентификатор.</param>
    /// <param name="valueObtainer">Кoллбэк путем которого будут поулчаться значения</param>
    /// <returns>Идентификатор Измерения(Gauge) для типа с заранее предустановленным набором значений Labels по которому можно устанавливать новое значение <see langword="double"/></returns>
    IDisposable RegisterGaugeCumulative<T>(Meter meter, T gaugeDto, Func<double> valueObtainer) where T : BaseMetricValueDtoV2;

    /// <summary>
    /// Зарегистрировать Сборщик метрик, который сам регистрирует свои метрики <see cref="IMetricsSourceV2.RegisterGauge{T}"/>,
    /// а затем в нужный момент присваивает им новые значения.
    /// </summary>
    void RegisterMetricsGrabber(Meter meter, IMetricsGrabberV2 metricsGrabber);
}