using Application.Interfaces.Services;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class RadioSourceService(DiscordBotContext context): IRadioSourceService
{
    public async Task<IReadOnlyCollection<RadioSource>> GetAllRadioSourcesAsync(CancellationToken cancellationToken = default)
    {
        return await context.RadioSources.OrderBy(r => r.Name).ToListAsync(cancellationToken);
    }

    public async Task<RadioSource> GetRadioSourceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.RadioSources.FirstOrDefaultAsync(rs => rs.Id == id, cancellationToken) ?? 
               throw new KeyNotFoundException($"Radio source with ID {id} not found.");
    }

    public async Task UpdateRadioSourceUrlAsync(Guid id, string newSourceUrl, bool isActive, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(newSourceUrl))
        {
            throw new ArgumentException("Source URL cannot be null or empty.", nameof(newSourceUrl));
        }

        var radioSource = context.RadioSources.FirstOrDefault(rs => rs.Id == id);
        if (radioSource == null)
        {
            throw new KeyNotFoundException($"Radio source with ID {id} not found.");
        }
        
        RadioSource.UpdateSourceUrl(radioSource, newSourceUrl, isActive);
        
        context.RadioSources.Update(radioSource);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> AddRadioSourceAsync(string name, string sourceUrl, CancellationToken cancellationToken = default)
    {
        var radioSource = RadioSource.Create(name, sourceUrl);
        context.RadioSources.Add(radioSource);
        await context.SaveChangesAsync(cancellationToken);
        
        return radioSource.Id;
    }

    public async Task<int> DeleteRadioSourceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var radioSource = await context.RadioSources.FirstOrDefaultAsync(rs => rs.Id == id, cancellationToken: cancellationToken);
        if (radioSource == null)
        {
            throw new KeyNotFoundException($"Radio source with ID {id} not found.");
        }

        context.RadioSources.Remove(radioSource);
        return await context.SaveChangesAsync(cancellationToken);
    }
}