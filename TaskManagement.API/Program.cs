using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using TaskManagement.API;
using TaskManagement.API.Data;
using TaskManagement.API.EventHandlers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTaskEventNotificationHttpClient(builder.Configuration);
builder.Services.AddSingleton<AsyncTaskEventsPublisher>();
builder.Services.AddSingleton<DateTimeProvider>();

builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.ApplyMigrations();

app.UseAuthorization();

app.MapControllers();

app.Run();
