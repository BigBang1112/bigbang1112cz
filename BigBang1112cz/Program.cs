using BigBang1112cz.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddWebServices();
builder.Services.AddDataServices(builder.Configuration);
builder.Services.AddTelemetryServices(builder.Configuration, builder.Environment);

var app = builder.Build();

app.MigrateDatabase();

// Configure the HTTP request pipeline.
app.UseSecurityMiddleware();

app.Run();

// locator options: this, github, dashmap, maniacdn