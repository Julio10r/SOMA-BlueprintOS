var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddOpenApi();

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Health Endpoint
app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        Status = "Healthy",
        Application = "BlueprintOS",
        Environment = app.Environment.EnvironmentName,
        Version = "1.0.0"
    });
});

app.Run();