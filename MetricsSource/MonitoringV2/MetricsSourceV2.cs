using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace MetricsSource.MonitoringV2
{
    public class MetricsSourceV2 : IMetricsSourceV2
    {
        #region Internal Types
        
        public class MetricDefinitionGauge
        {
            public string MetricName { init; get; } = null!;
            public string? MetricHelp;
            public readonly List<KeyValuePair<string, object>> MetricTags = new();
        }

        public class MetricValueObtainer : IDisposable
        {
            private readonly Func<double> m_valueGetter;

            public MetricValueObtainer(Func<double> valueGetter)
            {
                m_valueGetter = valueGetter;
            }

            public double GetMetricValue()
            {
                if (IsDisposed)
                {
                    return 0;
                }

                try
                {
                    return m_valueGetter();
                }
                catch
                {
                    /**/
                    return 0;
                }
            }

            public bool IsDisposed { get; set; }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        public class CumulativeMetric
        {
            public List<KeyValuePair<string, object>> MetricTags { get; init; } = null!;
            public readonly ConcurrentDictionary<CumulativeMetricObtainer, bool> MetricObtainers = new();
        }

        public class CumulativeMetricObtainer : IDisposable
        {
            private readonly Func<double> m_valueGetter;
            private readonly CumulativeMetric m_owner;

            public CumulativeMetricObtainer(Func<double> valueGetter, CumulativeMetric owner)
            {
                m_valueGetter = valueGetter;
                m_owner = owner;
            }

            public double GetMetricValue()
            {
                if (IsDisposed)
                {
                    return 0;
                }

                try
                {
                    return m_valueGetter();
                }
                catch
                {
                    /**/
                    return 0;
                }
            }

            public bool IsDisposed { get; set; }

            public void Dispose()
            {
                IsDisposed = true;
                m_owner.MetricObtainers.TryRemove(this, out _);
            }
        }

        public class GaugeMetricUnitValue : IGaugeMetricUnitValue
        {
            public double Value { get; set; }
            public List<KeyValuePair<string, object>> MetricTags { get; init; } = null!;
        }

        #endregion

        private readonly ConcurrentDictionary<Guid, CumulativeMetric> m_cumulativeGauges = new();
        private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<IMetricsGrabberV2, bool>> m_metricGrabbers = new();
        private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, GaugeMetricUnitValue>> m_gaugesValues = new();

        public IDisposable RegisterGaugeObservable<T>(Meter meter, T gaugeDto, Func<double> valueObtainer) where T : BaseMetricValueDtoV2
        {
            var gaugeDefinition = BuildMetricDefinitionGauge(gaugeDto);
            
            var metricUnit = new MetricValueObtainer(valueObtainer);

            meter.CreateObservableGauge<double>(gaugeDefinition.MetricName, () =>
            {
                if (metricUnit.IsDisposed)
                {
                    return Array.Empty<Measurement<double>>();
                }

                return new[] { new Measurement<double>(metricUnit.GetMetricValue(), gaugeDefinition.MetricTags) };
            }, description: gaugeDefinition.MetricHelp);

            return metricUnit;
        }

        public IGaugeMetricUnitValue RegisterGauge<T>(Meter meter, T gaugeDto, double initialValue = default) where T : BaseMetricValueDtoV2
        {
            var gaugeDefinition = BuildMetricDefinitionGauge(gaugeDto);
            
            return RegisterGauge(meter, gaugeDefinition.MetricName, gaugeDefinition.MetricTags, gaugeDefinition.MetricHelp, initialValue);
        }

        public IGaugeMetricUnitValue RegisterGauge(Meter meter, string metricName,
            List<KeyValuePair<string, object>> gaugeTags,
            string? metricHelp = null, double initialValue = default)
        {
            var metricAggregatedName = meter.Name + meter.Version + meter.GetHashCode() + metricName + gaugeTags
                .Select(pair => pair.Key + pair.Value).Aggregate("StrEmpty", (s, s1) => s + s1 + ";");
            using var md5 = MD5.Create();
            var metricNameBytes = Encoding.UTF8.GetBytes(metricAggregatedName);
            var metricUid = new Guid(md5.ComputeHash(metricNameBytes));

            var metricHashName = meter.Name + meter.Version + meter.GetHashCode() + metricName;
            using var md5Metric = MD5.Create();
            var md5MetricNameBytes = Encoding.UTF8.GetBytes(metricHashName);
            var md5MetricUid = new Guid(md5Metric.ComputeHash(md5MetricNameBytes));

            var metricCollection = m_gaugesValues.GetOrAdd(md5MetricUid, key =>
            {
                meter.CreateObservableGauge<double>(metricName, () =>
                {
                    if (m_gaugesValues.TryGetValue(key, out var allValues))
                    {
                        return allValues.Values.Select(metricUnit =>
                            new Measurement<double>(metricUnit.Value, metricUnit.MetricTags!));
                    }

                    return Array.Empty<Measurement<double>>();
                }, description: metricHelp);
                
                return new ConcurrentDictionary<Guid, GaugeMetricUnitValue>();
            });

            var result = new GaugeMetricUnitValue
            {
                Value = initialValue,
                MetricTags = gaugeTags
            };
            metricCollection[metricUid] = result;

            return result;
        }

        public void RegisterMetricsGrabber(Meter meter, IMetricsGrabberV2 metricsGrabber)
        {
            var meterAggregatedName = meter.Name + meter.Version + meter.GetHashCode();
            using var md5 = MD5.Create();
            var meterHash = new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(meterAggregatedName)));

            var meterMetricsForGrabber = m_metricGrabbers.GetOrAdd(meterHash, key =>
            {
                meter.CreateObservableGauge<double>("ExplicitMetricForGrabbers", () =>
                {
                    if (m_metricGrabbers.TryGetValue(key, out var allGrabbers))
                    {
                        foreach (var grabber in allGrabbers.Keys)
                        {
                            try
                            {
                                grabber.GrabMonitor(meter, this);
                            }
                            catch 
                            {
                                /**/
                            }
                        }
                    }

                    return Array.Empty<Measurement<double>>();
                });

                return new ConcurrentDictionary<IMetricsGrabberV2, bool>();
            });

            metricsGrabber.InitializeGrabber(meter, this);
            meterMetricsForGrabber[metricsGrabber] = true;
        }

        public IDisposable RegisterGaugeCumulative<T>(Meter meter, T gaugeDto, Func<double> valueObtainer) where T : BaseMetricValueDtoV2
        {
            var gaugeDefinition = BuildMetricDefinitionGauge(gaugeDto);
          
            var metricAggregatedName = meter.Name + meter.Version + meter.GetHashCode() +
                                       gaugeDefinition.MetricName + 
                                       gaugeDefinition.MetricTags.Aggregate("StrEmpty", (s, s1) => s + s1.Key + s1.Value + ";");
            using var md5 = MD5.Create();
            var metricNameBytes = Encoding.UTF8.GetBytes(metricAggregatedName);
            var metricUid = new Guid(md5.ComputeHash(metricNameBytes));

            var metricDeclaration = m_cumulativeGauges.GetOrAdd(metricUid, _ =>
            {
                var cmMetric = new CumulativeMetric
                {
                    MetricTags = gaugeDefinition.MetricTags,
                };

                meter.CreateObservableGauge<double>(gaugeDefinition.MetricName, () =>
                {
                    var cumulativeValue = cmMetric.MetricObtainers.Aggregate(0d, (d, pair) => d + pair.Key.GetMetricValue());
                    if (cmMetric.MetricObtainers.Count == 0)
                    {
                        return Array.Empty<Measurement<double>>();
                    }

                    return new[] { new Measurement<double>(cumulativeValue, cmMetric.MetricTags!) };
                }, description: gaugeDefinition.MetricHelp);

                return cmMetric;
            });

            var result = new CumulativeMetricObtainer(valueObtainer, metricDeclaration);
            metricDeclaration.MetricObtainers[result] = true;

            return result;
        }

        private MetricDefinitionGauge BuildMetricDefinitionGauge(object gaugeDto)
        {
            var gaugeType = gaugeDto.GetType();
            var metricAttribute = gaugeType.GetCustomAttribute<MetricDescriptionAttributeV2>(true);
            var fields = gaugeType.GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(info => info.DeclaringType != typeof(BaseMetricValueDtoV2))
                .ToList();
            var props = gaugeType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(info => info.DeclaringType != typeof(BaseMetricValueDtoV2))
                .Where(info => info.GetGetMethod()?.IsPublic == true).ToArray();

            CheckForStringFieldsOnly(gaugeType, fields, props);

            var gaugeDefinition = new MetricDefinitionGauge()
            {
                MetricName = metricAttribute?.MetricName ?? gaugeType.Name,
                MetricHelp = metricAttribute?.MetricHelp
            };

            gaugeDefinition.MetricTags.AddRange(fields.Select(info => new KeyValuePair<string, object>(info.Name,
                (string?)info.GetValue(gaugeDto) ??
                    throw new InvalidOperationException(
                        $"Поле '{info.Name}' метрики '{gaugeDefinition.MetricName}' равно NULL"))));
            gaugeDefinition.MetricTags.AddRange(props.Select(info => new KeyValuePair<string, object>(info.Name,
                (string?)info.GetValue(gaugeDto) ??
                    throw new InvalidOperationException(
                        $"Поле '{info.Name}' метрики '{gaugeDefinition.MetricName}' равно NULL"))));

            return (gaugeDefinition);
        }

        private void CheckForStringFieldsOnly(Type type, IEnumerable<FieldInfo> fields, IEnumerable<PropertyInfo> props)
        {
            var wrongprops = props.Where(info => info.PropertyType != typeof(string)).Select(info => info.Name).Concat(
                fields.Where(info => info.FieldType != typeof(string)).Select(info => info.Name)).ToList();
            if (wrongprops.Any())
            {
                throw new InvalidOperationException($"Тип '{type}' содержит некорректные типы полей. " +
                                                    $"Разрешены только поля типа '{typeof(string).Name}'. " +
                                                    "Имена некорректный полей " + wrongprops.Aggregate("", (s, s1) => s + s1 + ','));
            }
        }
    }

}
