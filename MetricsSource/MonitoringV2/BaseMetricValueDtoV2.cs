namespace MetricsSource.MonitoringV2
{
    /// <summary>
    /// Имя метрики определяется либо с помощью <see cref="MetricDescriptionAttributeV2"/>, либо используется имя класса наследника. <br/>
    /// Тэги метрики формируются из полей/свойств наследника. Допускаются только типа String.
    /// Каждое поле это тэг где - имя поля Key, значение поля Value.
    /// </summary>
    public abstract class BaseMetricValueDtoV2
    {
    }
}