using BigBang1112cz.Data;
using BigBang1112cz.Models.Db;
using Microsoft.EntityFrameworkCore;

namespace BigBang1112cz.Services;

public sealed class HornHostedService : IHostedService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IWebHostEnvironment env;

    public HornHostedService(IServiceScopeFactory scopeFactory, IWebHostEnvironment env)
    {
        this.scopeFactory = scopeFactory;
        this.env = env;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var hornFiles = env.WebRootFileProvider.GetDirectoryContents("horns");

        await using var scope = scopeFactory.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hornModels = await db.Horns.ToListAsync(cancellationToken);

        foreach (var hornFile in hornFiles)
        {
            var hornModel = hornModels.FirstOrDefault(x => x.FileName == hornFile.Name);

            var oggFile = TagLib.File.Create(hornFile.PhysicalPath);
            var duration = oggFile.Properties.Duration;

            if (hornModel is null)
            {
                await db.Horns.AddAsync(new HornDbModel
                {
                    FileName = hornFile.Name,
                    Duration = duration,
                    LastModifiedAt = hornFile.LastModified,
                }, cancellationToken);
            }
            else
            {
                hornModel.Duration = duration;
                hornModel.LastModifiedAt = hornFile.LastModified;
            }
        }

        foreach (var hornModel in hornModels)
        {
            hornModel.IsDeleted = !hornFiles.Any(x => x.Name == hornModel.FileName);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
