using Domain.Entities;

namespace Application.Interfaces.Services;

public interface IRadioSourceService
{
    Task<IReadOnlyCollection<RadioSource>> GetAllRadioSourcesAsync(CancellationToken cancellationToken = default);
    Task<RadioSource> GetRadioSourceByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateRadioSourceUrlAsync(Guid id, string newSourceUrl, bool isActive, CancellationToken cancellationToken = default);
    Task<Guid> AddRadioSourceAsync(string name, string sourceUrl, CancellationToken cancellationToken = default);
    Task<int> DeleteRadioSourceAsync(Guid id, CancellationToken cancellationToken = default);
}