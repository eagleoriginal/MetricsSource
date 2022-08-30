using MetricsSource.MonitoringV2;
using SampleApplicationMetricsSource.ExampleSubsystem;

var builder = WebApplication.CreateBuilder(args);
 
// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 1. Зарегистрировать и провести настроить подсистемы метрик.
// !!! Настройки лежат в AppJson в секции OpenTelemetryMetrics
builder.Services.AddMetricsServices(builder.Configuration,
    "TestService", 
    "Tests", 
    "Instance1", 
    "1.0.0.0",
    // !!! Важно MyCompany.MyProduct.* - это имя инструмента с которого в том числе будут забираться метрики
    providerBuilder => providerBuilder.AddMeter("MyCompany.MyProduct.*"));

// 2. Для тестов приведен некий сервис который там что-то делает.
// Он регистрирует некий набор метрик. И по таймауту изменяет одну метрику.
builder.Services.AddHostedService<MetricsBackgroundService>();

var app = builder.Build();

// Todo: Prometheus не работает в версии 1.3.0-rc.2
// 3. Включение Endpoint метрики Prometheus (удобно обозревать что там экспортируется) Можно посмотреть в Swagger.
//app.UseMetricsServices();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

// 4. И вообщем то все. Запуск и работает.
app.Run();
