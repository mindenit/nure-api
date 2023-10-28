using nure_api;
using nure_api.Models;
using nure_api.Handlers;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", async (HttpContext x) => {
    GroupsHandler.Init();
    return Results.Ok("Init complete");
});

app.MapGet("/schedule", async (HttpContext x) => {
    var query = x.Request.Query["id"];
    using var reader = new StreamReader(x.Request.Body);
    var body = await reader.ReadToEndAsync();
    return Results.Ok($"Id is {query}");
});

app.Run();