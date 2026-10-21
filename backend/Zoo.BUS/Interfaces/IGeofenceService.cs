using Zoo.BUS.DTOs;

namespace Zoo.BUS.Interfaces;

public interface IGeofenceService
{
    Task<EvaluateVisitResponse> EvaluateAsync(
        EvaluateVisitRequest request,
        CancellationToken cancellationToken = default);
}
