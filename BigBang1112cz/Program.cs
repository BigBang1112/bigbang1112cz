using BigBang1112cz.Configuration;
using BigBang1112cz.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Services.Configure<TrackmaniaOptions>(builder.Configuration.GetSection("Trackmania"));

// Add services to the container.
builder.Services.AddDomainServices();
builder.Services.AddWebServices();
builder.Services.AddDataServices(builder.Configuration);
builder.Services.AddTelemetryServices(builder.Configuration, builder.Environment);

var app = builder.Build();

app.MigrateDatabase();

// Configure the HTTP request pipeline.
app.UseMiddleware();

app.Run();