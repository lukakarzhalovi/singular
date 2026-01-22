using Microsoft.AspNetCore.RateLimiting;
using VirtualRoulette.Configuration;
using VirtualRoulette.Hubs;
using VirtualRoulette.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRepositories();
builder.Services.AddSettings(builder.Configuration);
builder.Services.AddAuthentification();
builder.Services.AddServices();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        // Update this port if you serve the HTML file from a different port
        policy.WithOrigins("http://localhost:3000","http://localhost:63342")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});



builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromSeconds(10);
        limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 2;
    });
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseCors("AllowLocalhost");
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
app.MapHub<JackpotHub>("/jackpot-hub");

app.Run();
