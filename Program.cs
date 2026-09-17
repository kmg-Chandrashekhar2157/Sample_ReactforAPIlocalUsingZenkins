using Poc.Api.Services;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "FrontendCorsPolicy";

// Origins the React dev server runs on. Configurable via the "Cors:AllowedOrigins"
// setting (see appsettings.Development.json) so this doesn't need a code change per env.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddControllers();

// The POC keeps todos in process memory; replace with a real store later.
builder.Services.AddSingleton<ITodoStore, InMemoryTodoStore>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Raw OpenAPI document, served at /openapi/v1.json
    app.MapOpenApi();

    // Swagger UI at /swagger, reading the document that MapOpenApi generates above.
    // Only the UI package is referenced — the document itself comes from
    // Microsoft.AspNetCore.OpenApi (builder.Services.AddOpenApi()).
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Poc.Api v1");
        options.DocumentTitle = "Poc.Api — Swagger";
    });
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors(FrontendCorsPolicy);

app.MapControllers();

// Cheap liveness probe the frontend uses to show backend status.
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    service = "Poc.Api",
    timestamp = DateTimeOffset.UtcNow,
}));
app.MapGet("/api/debug/routes", (EndpointDataSource dataSource) =>
{
    return Results.Ok(dataSource.Endpoints
        .OfType<RouteEndpoint>()
        .Select(e => e.RoutePattern.RawText)
        .OrderBy(x => x));
});

app.Run();
