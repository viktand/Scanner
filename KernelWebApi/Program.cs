using KernelWebApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IKernelRepository, KernelRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

IHostApplicationLifetime lifetime = app.Lifetime;

lifetime.ApplicationStarted.Register(() => {
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("��������!!! ����� �� ������� COM-����, ��� ���������� ���� ��������� ������ ����� Cntrl + C");
});

var store = app.Services.GetService<IKernelRepository>();
var config = builder.Configuration.GetValue<string>("KernelCofig:Port");

store.Start(config);

lifetime.ApplicationStopped.Register(() =>
{
    Console.WriteLine("���������� �� COM-�����");
    var  result = store.Stop();
    Console.WriteLine($"��������� �������� �����: {result}");
    Thread.Sleep(3000);
});
    
app.Run();
