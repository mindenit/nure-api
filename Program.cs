using Newtonsoft.Json;
using nure_api;
using nure_api.Models;
using nure_api.Handlers;

var  allowCORS = "_allowCORS";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: allowCORS,
        policy  =>
        {
            policy.WithOrigins("http://api.mindenit.tech",
                "https://api.mindenit.tech", "http://localhost:3000");
        });
});

var app = builder.Build();


using (var context = new Context())
{
    if (!context.Groups.Any())
    {
        GroupsHandler.Init();
        Console.WriteLine("Groups init complete");
    }

    if (!context.Teachers.Any())
    {
        TeachersHandler.Init();
        Console.WriteLine("Teachers init complete");
    }

    if (!context.Auditories.Any())
    {
        AuditoriesHandler.Init();
        Console.WriteLine("Auditories init complete");
    }
}

ScheduleHandler.Init();
Console.WriteLine("Schedule init complete");

app.MapGet("/", async (HttpContext x) => "Main page" );

app.MapGet("/groups", async (HttpContext x) => {
    var json = JsonConvert.SerializeObject(GroupsHandler.Get(), Formatting.Indented);
    return Results.Content(json, "application/json");
});

app.MapGet("/teachers", async (HttpContext x) => {
    var json = JsonConvert.SerializeObject(TeachersHandler.Get(), Formatting.Indented);
    return Results.Content(json, "application/json");
});

app.MapGet("/auditories", async (HttpContext x) => {
    var json = JsonConvert.SerializeObject(AuditoriesHandler.Get(), Formatting.Indented);
    return Results.Content(json, "application/json");
});

app.MapGet("/schedule", async (HttpContext x) => {
    var id = long.Parse(x.Request.Query["id"]);
    var type = x.Request.Query["type"];
    var start_time = long.Parse(x.Request.Query["start_time"]);
    var end_time = long.Parse(x.Request.Query["end_time"]);
    using var reader = new StreamReader(x.Request.Body);
    var body = await reader.ReadToEndAsync();
    var json = JsonConvert.SerializeObject(ScheduleHandler.GetEvents(id, type, start_time, end_time), Formatting.Indented);
    return Results.Content(json, "application/json");
});

app.MapPost("/register", async (HttpContext x) =>
{
    
});

app.UseCors(allowCORS);

app.Run();