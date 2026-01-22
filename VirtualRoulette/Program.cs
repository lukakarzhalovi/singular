using VirtualRoulette.Infrastructure.SignalR.Hubs;
using VirtualRoulette.Presentation.Configuration;
using VirtualRoulette.Presentation.Configuration.Settings;
using VirtualRoulette.Presentation.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRepositories();
builder.Services.AddSettings(builder.Configuration);
builder.Services.AddAuthentification(builder.Configuration);
builder.Services.AddServices();
builder.Services.AddRateLimiter(builder.Configuration);
builder.Services.AddCors(builder.Configuration);


builder.Services.AddControllers();

var app = builder.Build();

var corsSettingsResolved = builder.Configuration.GetSection("Cors").Get<CorsSettings>() 
    ?? throw new InvalidOperationException("CORS settings are required");

app.UseCors(corsSettingsResolved.PolicyName);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ActivityTrackingMiddleware>();
app.UseSession();

app.MapControllers();

// Map SignalR Hub for real-time jackpot updates
var signalRSettingsResolved = builder.Configuration.GetSection("SignalR").Get<SignalRSettings>() 
    ?? throw new InvalidOperationException("SignalR settings are required");

app.MapHub<JackpotHub>(signalRSettingsResolved.HubPath);

app.Run();
