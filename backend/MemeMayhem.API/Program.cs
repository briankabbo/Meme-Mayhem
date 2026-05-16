using MemeMayhem.API.Hubs;
using MemeMayhem.Core.Interfaces;
using MemeMayhem.Infrastructure.Data;
using MemeMayhem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MemeMayhemDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Services
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IGameService, GameService>();

builder.Services.AddHttpClient<IAIPromptService, AIPromptService>();

builder.Services.AddHttpClient<IGiphyService, GiphyService>();
builder.Services.AddHttpClient<IMemeCardService, MemeCardService>();
builder.Services.AddHostedService<StartupSyncService>();

// SignalR
builder.Services.AddSignalR();

// CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("ReactApp");
app.MapHub<GameHub>("/hubs/game");

app.Run();