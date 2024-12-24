using System;
using DistributedCachingWithRedisCache;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddStackExchangeRedisCache(redisOptions =>
{
    redisOptions.Configuration = builder.Configuration.GetConnectionString("Redis");
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddDistributedMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapPost("/AddSubscriber", async ([FromBody] string Email, AppDbContext context) =>
{
    var entry = context.Subscribers.Add(new Subscriber
    (
        Guid.NewGuid(),
        Email
    ));
    context.SaveChanges();
    return Results.Ok(entry.Entity.Email);

}).WithName("AddSubscriber")
.WithOpenApi();

app.MapGet("/subscriber", async (AppDbContext context, IDistributedCache distributedCache, Guid guid, CancellationToken cancellationToken) =>
{
    string cachedEntry = await distributedCache.GetStringAsync(guid.ToString(), cancellationToken);
    Subscriber entry = null;
    if (cachedEntry == null)
    {
        entry = context.Subscribers.Where(x => x.subscriberId == guid).FirstOrDefault();
        distributedCache.SetStringAsync(guid.ToString(), JsonConvert.SerializeObject(entry), cancellationToken);
        return Results.Ok(entry.Email);
    }
    if (cachedEntry != null)
    {
        entry = JsonConvert.DeserializeObject<Subscriber>(cachedEntry);
        return Results.Ok(entry.Email);
    }
    return Results.NotFound();
})
.WithName("GetSubscriber")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
