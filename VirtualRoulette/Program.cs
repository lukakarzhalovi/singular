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
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseCors();

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
