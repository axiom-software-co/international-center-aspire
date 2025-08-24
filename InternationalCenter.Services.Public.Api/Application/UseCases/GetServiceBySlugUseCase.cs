using InternationalCenter.Services.Domain.Repositories;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Services.Domain.Models;
using InternationalCenter.Services.Domain.Entities;

namespace InternationalCenter.Services.Public.Api.Application.UseCases;

public sealed class GetServiceBySlugUseCase
{
    private readonly IServiceRepository _serviceRepository;
    private readonly ILogger<GetServiceBySlugUseCase> _logger;

    public GetServiceBySlugUseCase(
        IServiceRepository serviceRepository,
        ILogger<GetServiceBySlugUseCase> logger)
    {
        _serviceRepository = serviceRepository ?? throw new ArgumentNullException(nameof(serviceRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Service>> ExecuteAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(slug))
        {
            return Result<Service>.Failure(
                new ValidationError("Slug cannot be empty"));
        }

        try
        {
            // Create slug value object with validation
            var slugValueObject = Slug.Create(slug);
            
            // Query service by slug
            var service = await _serviceRepository.GetBySlugAsync(slugValueObject, cancellationToken);
            
            if (service == null)
            {
                _logger.LogWarning("Service not found for slug: {Slug}", slug);
                return Result<Service>.Failure(new ServiceNotFoundError(slug));
            }

            // Ensure service is active and available
            if (!service.IsActive)
            {
                _logger.LogWarning("Service found but not active for slug: {Slug}", slug);
                return Result<Service>.Failure(new ServiceNotFoundError(slug));
            }

            _logger.LogInformation("Successfully retrieved service by slug: {Slug}", slug);
            return Result<Service>.Success(service);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid slug format: {Slug}", slug);
            return Result<Service>.Failure(
                new ValidationError("Invalid slug format"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve service by slug: {Slug}", slug);
            return Result<Service>.Failure(
                new ServiceQueryError("Failed to retrieve service", ex));
        }
    }
}