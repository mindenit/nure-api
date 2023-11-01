using Newtonsoft.Json;
using nure_api;
using nure_api.Models;
using nure_api.Handlers;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

GroupsHandler.Init();
Console.WriteLine("Init complete");

app.MapGet("/groups", async (HttpContext x) => {
    var json = JsonConvert.SerializeObject(GroupsHandler.Get(), Formatting.Indented);
    return Results.Content(json, "application/json");
});

app.MapGet("/schedule", async (HttpContext x) => {
    var query = x.Request.Query["id"];
    using var reader = new StreamReader(x.Request.Body);
    var body = await reader.ReadToEndAsync();
    return Results.Ok($"Id is {query}");
});

app.Run();