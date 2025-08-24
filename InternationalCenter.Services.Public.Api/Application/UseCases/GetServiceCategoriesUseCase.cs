using InternationalCenter.Services.Domain.Repositories;
using InternationalCenter.Services.Domain.Models;
using InternationalCenter.Services.Domain.Entities;

namespace InternationalCenter.Services.Public.Api.Application.UseCases;

public sealed class GetServiceCategoriesUseCase
{
    private readonly IServiceCategoryRepository _categoryRepository;
    private readonly ILogger<GetServiceCategoriesUseCase> _logger;

    public GetServiceCategoriesUseCase(
        IServiceCategoryRepository categoryRepository,
        ILogger<GetServiceCategoriesUseCase> logger)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<IEnumerable<ServiceCategory>>> ExecuteAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Query categories using repository
            var categories = activeOnly 
                ? await _categoryRepository.GetActiveOrderedAsync(cancellationToken)
                : await _categoryRepository.GetAllAsync(false, cancellationToken);

            _logger.LogInformation("Successfully retrieved {CategoryCount} service categories (activeOnly: {ActiveOnly})", 
                categories.Count, activeOnly);
                
            return Result<IEnumerable<ServiceCategory>>.Success(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve service categories");
            return Result<IEnumerable<ServiceCategory>>.Failure(
                new ServiceQueryError("Failed to retrieve service categories", ex));
        }
    }
}