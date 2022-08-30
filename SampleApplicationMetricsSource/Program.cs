using MetricsSource.MonitoringV2;
using SampleApplicationMetricsSource.ExampleSubsystem;

var builder = WebApplication.CreateBuilder(args);
 
// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 1. ���������������� � �������� ��������� ���������� ������.
// !!! ��������� ����� � AppJson � ������ OpenTelemetryMetrics
builder.Services.AddMetricsServices(builder.Configuration,
    "TestService", 
    "Tests", 
    "Instance1", 
    "1.0.0.0",
    // !!! ����� MyCompany.MyProduct.* - ��� ��� ����������� � �������� � ��� ����� ����� ���������� �������
    providerBuilder => providerBuilder.AddMeter("MyCompany.MyProduct.*"));

// 2. ��� ������ �������� ����� ������ ������� ��� ���-�� ������.
// �� ������������ ����� ����� ������. � �� �������� �������� ���� �������.
builder.Services.AddHostedService<MetricsBackgroundService>();

var app = builder.Build();

// Todo: Prometheus �� �������� � ������ 1.3.0-rc.2
// 3. ��������� Endpoint ������� Prometheus (������ ���������� ��� ��� ��������������) ����� ���������� � Swagger.
//app.UseMetricsServices();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

// 4. � ������� �� ���. ������ � ��������.
app.Run();
