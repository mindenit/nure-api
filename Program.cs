using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using nure_api;
using nure_api.Models;
using nure_api.Handlers;
using nure_api.Services;
using Microsoft.OpenApi.Models;
using Serilog;

var allowCORS = "_allowCORS";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: allowCORS,
        policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});
builder.Host.UseSerilog();
builder.Services.AddEndpointsApiExplorer();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File(
        $"Logs/{DateTime.Now.Day}_{DateTime.Now.Month}_{DateTime.Now.Year}-{DateTime.Now.TimeOfDay}.txt",
        rollingInterval: RollingInterval.Infinite)
    .CreateLogger();

Log.Information("Application started!");

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v0.9.0",
        Title = "Mindenit API",
        Description = "The NURE schedule API",
        License = new OpenApiLicense
        {
            Name = "License",
            Url = new Uri("https://www.gnu.org/licenses/gpl-3.0.uk.html")
        }
    });

    //set the comments path for the swagger json and ui
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

builder.Services.AddDbContext<Context>(options =>
    options.UseNpgsql(File.ReadAllText("dbConnection")));

builder.Services.AddAuthorization();

builder.Services.AddIdentityApiEndpoints<AuthUser>()
    .AddErrorDescriber<IdentityErrors>()
    .AddEntityFrameworkStores<Context>();

builder.Services.AddScoped<UserManager<AuthUser>>();

builder.Services.AddSingleton<IEmailSender<AuthUser>, DummyEmailSender>();

var app = builder.Build();


using (var context = new Context())
{
    if (!context.Groups.Any())
    {
        GroupsHandler.Init();
        Log.Information("Groups init complete");
    }

    if (!context.Teachers.Any())
    {
        TeachersHandler.Init();
        Log.Information("Teachers init complete");
    }

    if (!context.Auditories.Any())
    {
        AuditoriesHandler.Init();
        Log.Information("Auditories init complete");
    }
}

//ScheduleHandler.Init();
Log.Information("Schedule init complete");

/// <summary>
/// List all groups
/// </summary>

app.MapGet("/groups", async (HttpContext x) =>
{
    var json = JsonConvert.SerializeObject(GroupsHandler.Get(), Formatting.Indented);
    return Results.Content(json, "application/json");
}).Produces<IList<Group>>()
.WithOpenApi(generatedOperation =>
{
    generatedOperation.Description = "List all groups";
    generatedOperation.Summary = "List all groups";
    return generatedOperation;
});


/// <summary>
/// List all teachers
/// </summary>
app.MapGet("/teachers", async (HttpContext x) =>
{
    var json = JsonConvert.SerializeObject(TeachersHandler.Get(), Formatting.Indented);
    return Results.Content(json, "application/json");
}).WithOpenApi(generatedOperation =>
{
    generatedOperation.Description = "List all teachers";
    generatedOperation.Summary = "List all teachers";
    return generatedOperation;
})
.Produces<IList<Teacher>>();

/// <summary>
/// List all auditories
/// </summary>
app.MapGet("/auditories", async (HttpContext x) =>
{
    var json = JsonConvert.SerializeObject(AuditoriesHandler.Get(), Formatting.Indented);
    return Results.Content(json, "application/json");
}).WithOpenApi(generatedOperation =>
{
    generatedOperation.Description = "List all auditories";
    generatedOperation.Summary = "List all auditories";
    return generatedOperation;
})
.Produces<IList<Auditory>>();

/// <summary>
/// Get schedule for group
/// </summary>
/// <param name="id">Group id</param>
/// <param name="type">Schedule type</param>
/// <param name="start_time">Start time</param>
/// <param name="end_time">End time</param>
/// <returns></returns>
app.MapGet("/schedule", async (HttpContext x) =>
{
    var id = long.Parse(x.Request.Query["id"]);
    var type = x.Request.Query["type"];
    var start_time = long.Parse(x.Request.Query["start_time"]);
    var end_time = long.Parse(x.Request.Query["end_time"]);
    using var reader = new StreamReader(x.Request.Body);
    var body = await reader.ReadToEndAsync();
    var json = JsonConvert.SerializeObject(ScheduleHandler.GetEvents(id, type, start_time, end_time), Formatting.Indented);
    return Results.Content(json, "application/json");
}).WithOpenApi(genOp =>
{
    genOp.Description = "Get schedule for group";
    genOp.Summary = "Get schedule for group";
    genOp.Parameters.Add(new OpenApiParameter
    {
        Name = "id",
        In = ParameterLocation.Query,
        Required = true,
        Schema = new OpenApiSchema
        {
            Type = "integer"
        }
    });
    genOp.Parameters.Add(new OpenApiParameter
    {
        Name = "type",
        In = ParameterLocation.Query,
        Required = true,
        Schema = new OpenApiSchema
        {
            Type = "string"
        }
    });
    genOp.Parameters.Add(new OpenApiParameter
    {
        Name = "start_time",
        In = ParameterLocation.Query,
        Required = true,
        Schema = new OpenApiSchema
        {
            Type = "integer"
        }
    });
    genOp.Parameters.Add(new OpenApiParameter
    {
        Name = "end_time",
        In = ParameterLocation.Query,
        Required = true,
        Schema = new OpenApiSchema
        {
            Type = "integer"
        }
    });
    return genOp;
})
.Produces<IList<Event>>();

/// <summary>
/// Get user info, requires authorization with Bearer token
/// </summary>
app.MapGet("/user", [Authorize] async (HttpContext x, UserManager<AuthUser> userManager) =>
{
    var user = await userManager.GetUserAsync(x.User);
    if (user == null)
    {
        return Results.NotFound("User not found");
    }
    var json = JsonConvert.SerializeObject(user, Formatting.Indented);
    return Results.Content(json, "application/json");
}).WithOpenApi(genOp =>
{
    genOp.Description = "Get user info, Example: ```curl -X GET -H \"Content - Type: application/json\" -H \"Authorization: Bearer your_token\" http://api.mindenit.tech/user```";
    genOp.Summary = "Get user info";
    return genOp;
})
.Produces<AuthUser>();

/// <summary>
/// Add group to user, requires authorization with Bearer token
/// </summary>
/// <remarks>
/// Sample request:
///
///     curl -X POST 'http://api.mindenit.tech/user/addgroup' \
///     -H 'Authorization: Bearer your_auth_token' \
///     -H 'Content-Type: application/json' \
///     -d '{
///         "id": "group_id",
///         "name": "group_name"
///     }'
///
/// </remarks>
app.MapPost("/user/add", [Authorize] async (HttpContext x, UserManager<AuthUser> userManager) =>
{
    var user = await userManager.GetUserAsync(x.User);
    if (user == null)
    {
        return Results.NotFound("User not found");
    }
    using var reader = new StreamReader(x.Request.Body);
    var body = await reader.ReadToEndAsync();
    var schedule = JsonConvert.DeserializeObject<Schedule>(body);
    if (!DbUtil.CheckExists(schedule))
    {
         return Results.BadRequest("This group, or teacher or auditory not found");
    }

    if (user.Schedules == null)
    {
        user.Schedules = new List<string>();
    }

    user.Schedules.Add(body);

    await userManager.UpdateAsync(user);
    return Results.Ok("Schedule added");
}).WithOpenApi(genOp =>
{
    genOp.Description = "Add schedule to user, requires authorization with Bearer token. Example: ```curl -X POST 'http://api.mindenit.tech/user/add' \" +\n" +
    "-H 'Authorization: Bearer your_auth_token' \" +\n" +
    "-H 'Content-Type: application/json' \" +\n" +
    "   -d '{" + "\n" +
    "       \"id\": \"id\"," + "\n" +
    "       \"name\": \"name" + "\n" +
    "   }'```";
    genOp.Summary = "Add schedule to user";
    return genOp;
})
.Produces<string>();
/// <summary>
/// Remove group from user, requires authorization with Bearer token
/// </summary>
/// <remarks>
/// Sample request:
///
///     curl -X POST 'http://api.mindenit.tech/user/removegroup' \
///     -H 'Authorization: Bearer your_auth_token' \
///     -H 'Content-Type: application/json' \
///     -d '{
///         "id": "group_id",
///         "name": "group_name"
///     }'
///
/// </remarks>
app.MapDelete("/user/remove", [Authorize] async (HttpContext x, UserManager<AuthUser> userManager) =>
{
    var user = await userManager.GetUserAsync(x.User);
    if (user == null)
    {
        return Results.NotFound("User not found");
    }
    using var reader = new StreamReader(x.Request.Body);
    var body = await reader.ReadToEndAsync();
    var schedule = JsonConvert.DeserializeObject<Schedule>(body);
    if (!DbUtil.CheckExists(schedule))
    {
        return Results.BadRequest("This group, or teacher or auditory not found");
    }

    if (user.Schedules == null)
    {
        user.Schedules = new List<string>();
    }

    // delete schedule from user

    try
    {
        user.Schedules.Remove(body);
    }
    catch (Exception e)
    {
        return Results.NotFound("Schedule not found");
    }

    await userManager.UpdateAsync(user);
    return Results.Ok("Schedule removed");
}).WithOpenApi(genOp =>
{
    genOp.Description = "Remove schedule from user, requires authorization with Bearer token. Example: ```curl -X POST 'http://api.mindenit.tech/user/remove' \" +\n" +
    "-H 'Authorization: Bearer your_auth_token' \" +\n" +
    "-H 'Content-Type: application/json' \" +\n" +
    "   -d '{" + "\n" +
    "       \"id\": \"id\"," + "\n" +
    "       \"name\": \"name" + "\n" +
    "       \"type\": \"type" + "\n" +
    "   }'```";
    genOp.Summary = "Remove schedule from user";
    return genOp;
})
.Produces<string>();

app.MapDelete("/user/destroy", [Authorize] async (HttpContext x, UserManager<AuthUser> userManager) =>
{
    var user = await userManager.GetUserAsync(x.User);
    if (user == null)
    {
        return Results.NotFound("User not found");
    }
    await userManager.DeleteAsync(user);
    return Results.Ok("User deleted");
}).WithOpenApi(genOp =>
{
    genOp.Description = "Delete user, requires authorization with Bearer token. Example: <code>curl -X POST -H \"Content - Type: application/json\" -H \"Authorization: Bearer your_token\" http://api.mindenit.tech/user/destroy<code>";
    genOp.Summary = "Delete user";
    return genOp;
});


app.UseCors(allowCORS);

app.UseSwagger(options =>
{
    options.SerializeAsV2 = true;
});

// change swagger endpoint to /
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mindenit API");
    c.RoutePrefix = "";
});

app.MapIdentityApi<AuthUser>();
app.UseAuthorization();

app.Run();
