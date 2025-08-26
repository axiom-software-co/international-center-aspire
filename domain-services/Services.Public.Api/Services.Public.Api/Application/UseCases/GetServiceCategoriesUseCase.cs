using InternationalCenter.Services.Public.Api.Infrastructure.Repositories.Interfaces;
using Services.Shared.Models;
using Services.Shared.Entities;

namespace InternationalCenter.Services.Public.Api.Application.UseCases;

public sealed class GetServiceCategoriesUseCase
{
    private readonly IServiceCategoryReadRepository _categoryRepository;
    private readonly ILogger<GetServiceCategoriesUseCase> _logger;

    public GetServiceCategoriesUseCase(
        IServiceCategoryReadRepository categoryRepository,
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
            // Query categories using Dapper repository for high performance
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