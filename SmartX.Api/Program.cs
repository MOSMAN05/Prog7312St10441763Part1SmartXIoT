
using SmartX.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDataProtection();

// Register services used by the application
builder.Services.AddSingleton<TelemetryService>();
builder.Services.AddSingleton<DeploymentService>();
builder.Services.AddSingleton<AttachmentService>();
builder.Services.AddSingleton<RecommendationService>();
builder.Services.AddSingleton<AlertRoutingService>();
builder.Services.AddSingleton<SensorRegistryService>();
builder.Services.AddSingleton<DeviceCommandService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("ClientPolicy");

app.UseAuthorization();

app.MapControllers();

app.Run();
