using Domain.Entities;

namespace Application.Interfaces.Services;

public interface IRadioSourceService
{
    Task<IReadOnlyCollection<RadioSource>> GetAllRadioSourcesAsync(CancellationToken cancellationToken);
    Task<RadioSource> GetRadioSourceByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateRadioSourceUrlAsync(Guid id, string name, string newSourceUrl, bool isActive, CancellationToken cancellationToken);
    Task<Guid> AddRadioSourceAsync(string name, string sourceUrl, CancellationToken cancellationToken);
    Task<int> DeleteRadioSourceAsync(Guid id, CancellationToken cancellationToken);
}