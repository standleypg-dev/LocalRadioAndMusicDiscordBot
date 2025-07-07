using Domain.Entities;

namespace Application.Interfaces.Services;

public interface IRadioSourceService
{
    Task<IReadOnlyCollection<RadioSource>> GetAllRadioSourcesAsync(CancellationToken cancellationToken = default);
    Task<RadioSource> GetRadioSourceByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateRadioSourceUrlAsync(Guid id, string newSourceUrl, CancellationToken cancellationToken = default);
}