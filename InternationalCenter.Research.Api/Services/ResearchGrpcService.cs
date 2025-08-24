using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using InternationalCenter.Shared.Infrastructure;
using InternationalCenter.Shared.Proto.Research;
using InternationalCenter.Shared.Proto.Common;
using Google.Protobuf.WellKnownTypes;
using static InternationalCenter.Shared.Proto.Common.SortDirection;

namespace InternationalCenter.Research.Api.Services;

public class ResearchGrpcService : ResearchService.ResearchServiceBase
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ResearchGrpcService> _logger;

    public ResearchGrpcService(
        ApplicationDbContext context,
        IDistributedCache cache,
        ILogger<ResearchGrpcService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public override async Task<GetResearchArticlesResponse> GetResearchArticles(
        GetResearchArticlesRequest request, 
        ServerCallContext context)
    {
        try
        {
            var query = _context.ResearchArticles.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(request.Filter?.Category))
            {
                query = query.Where(a => a.Category == request.Filter.Category);
            }

            if (request.Filter?.Featured != null)
            {
                query = query.Where(a => a.Featured == request.Filter.Featured.Value);
            }

            // Status filter not available in CategoryFilter

            // Apply research-specific filters
            if (request.ResearchFilter != null)
            {
                if (!string.IsNullOrEmpty(request.ResearchFilter.ResearchType))
                {
                    query = query.Where(a => a.StudyType == request.ResearchFilter.ResearchType);
                }

                if (!string.IsNullOrEmpty(request.ResearchFilter.Journal))
                {
                    query = query.Where(a => a.DOI.Contains(request.ResearchFilter.Journal));
                }

                if (!string.IsNullOrEmpty(request.ResearchFilter.Institution))
                {
                    query = query.Where(a => a.AuthorName.Contains(request.ResearchFilter.Institution));
                }

                if (request.ResearchFilter.Keywords.Count > 0)
                {
                    foreach (var keyword in request.ResearchFilter.Keywords)
                    {
                        query = query.Where(a => a.Keywords.Contains(keyword));
                    }
                }

                if (request.ResearchFilter.FromDate != null)
                {
                    var fromDate = request.ResearchFilter.FromDate.ToDateTime();
                    query = query.Where(a => a.PublishedAt >= fromDate);
                }

                if (request.ResearchFilter.ToDate != null)
                {
                    var toDate = request.ResearchFilter.ToDate.ToDateTime();
                    query = query.Where(a => a.PublishedAt <= toDate);
                }
            }

            // Apply sorting
            if (request.Sort != null)
            {
                var ascending = request.Sort.Direction == SortDirection.Asc;
                query = request.Sort.SortBy switch
                {
                    "title" => ascending ? query.OrderBy(a => a.Title) : query.OrderByDescending(a => a.Title),
                    "published_date" => ascending ? query.OrderBy(a => a.PublishedAt) : query.OrderByDescending(a => a.PublishedAt),
                    "created_at" => ascending ? query.OrderBy(a => a.CreatedAt) : query.OrderByDescending(a => a.CreatedAt),
                    _ => query.OrderByDescending(a => a.PublishedAt)
                };
            }
            else
            {
                query = query.OrderByDescending(a => a.PublishedAt);
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var pageSize = request.Pagination?.PageSize ?? 10;
            var page = request.Pagination?.Page ?? 1;
            var skip = (page - 1) * pageSize;

            var articles = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            var response = new GetResearchArticlesResponse
            {
                Pagination = new PaginationResponse
                {
                    Page = page,
                    PageSize = pageSize,
                    Total = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            };

            foreach (var article in articles)
            {
                response.Articles.Add(MapToProto(article));
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting research articles");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<GetResearchArticleResponse> GetResearchArticleBySlug(
        GetResearchArticleBySlugRequest request, 
        ServerCallContext context)
    {
        try
        {
            var article = await _context.ResearchArticles
                .FirstOrDefaultAsync(a => a.Slug == request.Slug);

            if (article == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Research article not found"));
            }

            return new GetResearchArticleResponse
            {
                Article = MapToProto(article)
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting research article by slug: {Slug}", request.Slug);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<SearchResearchArticlesResponse> SearchResearchArticles(
        SearchResearchArticlesRequest request, 
        ServerCallContext context)
    {
        try
        {
            var query = _context.ResearchArticles.AsQueryable();

            // Apply search
            if (!string.IsNullOrEmpty(request.Query))
            {
                var searchTerm = $"%{request.Query}%";
                query = query.Where(a => 
                    EF.Functions.ILike(a.Title, searchTerm) ||
                    EF.Functions.ILike(a.Excerpt, searchTerm) ||
                    EF.Functions.ILike(a.Content, searchTerm) ||
                    EF.Functions.ILike(a.AuthorName, searchTerm) ||
                    a.Keywords.Any(keyword => EF.Functions.ILike(keyword, searchTerm)));
            }

            // Apply additional filters (reuse logic from GetResearchArticles)
            if (!string.IsNullOrEmpty(request.Filter?.Category))
            {
                query = query.Where(a => a.Category == request.Filter.Category);
            }

            if (request.ResearchFilter != null)
            {
                if (!string.IsNullOrEmpty(request.ResearchFilter.ResearchType))
                {
                    query = query.Where(a => a.StudyType == request.ResearchFilter.ResearchType);
                }
            }

            // Apply sorting
            if (request.Sort != null)
            {
                var ascending = request.Sort.Direction == SortDirection.Asc;
                query = request.Sort.SortBy switch
                {
                    "title" => ascending ? query.OrderBy(a => a.Title) : query.OrderByDescending(a => a.Title),
                    "published_date" => ascending ? query.OrderBy(a => a.PublishedAt) : query.OrderByDescending(a => a.PublishedAt),
                    _ => query.OrderByDescending(a => a.PublishedAt)
                };
            }
            else
            {
                query = query.OrderByDescending(a => a.PublishedAt);
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var pageSize = request.Pagination?.PageSize ?? 10;
            var page = request.Pagination?.Page ?? 1;
            var skip = (page - 1) * pageSize;

            var articles = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            var response = new SearchResearchArticlesResponse
            {
                Query = request.Query,
                Pagination = new PaginationResponse
                {
                    Page = page,
                    PageSize = pageSize,
                    Total = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            };

            foreach (var article in articles)
            {
                response.Articles.Add(MapToProto(article));
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching research articles: {Query}", request.Query);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<GetResearchCategoriesResponse> GetResearchCategories(
        GetResearchCategoriesRequest request, 
        ServerCallContext context)
    {
        try
        {
            // Since ResearchCategories DbSet doesn't exist, we'll return distinct categories from articles
            var distinctCategories = await _context.ResearchArticles
                .Where(a => !string.IsNullOrEmpty(a.Category))
                .Select(a => a.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var response = new GetResearchCategoriesResponse();

            for (int i = 0; i < distinctCategories.Count; i++)
            {
                var categoryName = distinctCategories[i];
                response.Categories.Add(new ResearchCategory
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = categoryName,
                    Description = "",
                    Slug = categoryName.ToLowerInvariant().Replace(" ", "-"),
                    DisplayOrder = i,
                    Active = true,
                    CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow),
                    UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting research categories");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<GetFeaturedResearchResponse> GetFeaturedResearch(
        GetFeaturedResearchRequest request, 
        ServerCallContext context)
    {
        try
        {
            var limit = request.Limit ?? 5;

            var articles = await _context.ResearchArticles
                .Where(a => a.Featured && a.Status == "published")
                .OrderByDescending(a => a.PublishedAt)
                .Take(limit)
                .ToListAsync();

            var response = new GetFeaturedResearchResponse();

            foreach (var article in articles)
            {
                response.Articles.Add(MapToProto(article));
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting featured research");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<GetRecentResearchResponse> GetRecentResearch(
        GetRecentResearchRequest request, 
        ServerCallContext context)
    {
        try
        {
            var articles = await _context.ResearchArticles
                .Where(a => a.Status == "published")
                .OrderByDescending(a => a.PublishedAt)
                .Take(request.Limit)
                .ToListAsync();

            var response = new GetRecentResearchResponse();

            foreach (var article in articles)
            {
                response.Articles.Add(MapToProto(article));
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent research");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<GetResearchByKeywordResponse> GetResearchByKeyword(
        GetResearchByKeywordRequest request, 
        ServerCallContext context)
    {
        try
        {
            var query = _context.ResearchArticles
                .Where(a => a.Keywords.Contains(request.Keyword) && a.Status == "published");

            var totalCount = await query.CountAsync();

            var pageSize = request.Pagination?.PageSize ?? 10;
            var page = request.Pagination?.Page ?? 1;
            var skip = (page - 1) * pageSize;

            var articles = await query
                .OrderByDescending(a => a.PublishedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            var response = new GetResearchByKeywordResponse
            {
                Keyword = request.Keyword,
                Pagination = new PaginationResponse
                {
                    Page = page,
                    PageSize = pageSize,
                    Total = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            };

            foreach (var article in articles)
            {
                response.Articles.Add(MapToProto(article));
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting research by keyword: {Keyword}", request.Keyword);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<GetResearchByAuthorResponse> GetResearchByAuthor(
        GetResearchByAuthorRequest request, 
        ServerCallContext context)
    {
        try
        {
            var query = _context.ResearchArticles
                .Where(a => a.AuthorName.Contains(request.Author) && a.Status == "published");

            var totalCount = await query.CountAsync();

            var pageSize = request.Pagination?.PageSize ?? 10;
            var page = request.Pagination?.Page ?? 1;
            var skip = (page - 1) * pageSize;

            var articles = await query
                .OrderByDescending(a => a.PublishedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            var response = new GetResearchByAuthorResponse
            {
                Author = request.Author,
                Pagination = new PaginationResponse
                {
                    Page = page,
                    PageSize = pageSize,
                    Total = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            };

            foreach (var article in articles)
            {
                response.Articles.Add(MapToProto(article));
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting research by author: {Author}", request.Author);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task StreamResearchArticles(
        GetResearchArticlesRequest request, 
        IServerStreamWriter<ResearchArticle> responseStream, 
        ServerCallContext context)
    {
        try
        {
            var articles = await _context.ResearchArticles
                .Where(a => a.Status == "published")
                .OrderByDescending(a => a.PublishedAt)
                .ToListAsync();

            foreach (var article in articles)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    break;

                await responseStream.WriteAsync(MapToProto(article));
                await Task.Delay(100, context.CancellationToken); // Small delay for demonstration
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming research articles");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<HealthCheckResponse> HealthCheck(
        HealthCheckRequest request, 
        ServerCallContext context)
    {
        try
        {
            // Simple database connectivity check
            await _context.Database.CanConnectAsync();

            return new HealthCheckResponse
            {
                Status = HealthStatus.Serving,
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                ServiceName = "research-api",
                Version = "1.0.0"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return new HealthCheckResponse
            {
                Status = HealthStatus.NotServing,
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                ServiceName = "research-api",
                Version = "1.0.0"
            };
        }
    }

    private static ResearchArticle MapToProto(InternationalCenter.Shared.Models.ResearchArticle entity)
    {
        var proto = new ResearchArticle
        {
            Id = entity.Id,
            Title = entity.Title,
            Slug = entity.Slug,
            Abstract = entity.Excerpt ?? "",
            Content = entity.Content ?? "",
            Institution = entity.AuthorName ?? "",
            PublishedDate = entity.PublishedAt.HasValue ? Timestamp.FromDateTime(entity.PublishedAt.Value) : null,
            Journal = entity.DOI ?? "",
            Doi = entity.DOI ?? "",
            ResearchType = entity.StudyType ?? "",
            Status = entity.Status,
            Featured = entity.Featured,
            Category = entity.Category ?? "",
            PdfUrl = "",
            ImageUrl = entity.ImageUrl ?? "",
            MetaTitle = entity.MetaTitle ?? "",
            MetaDescription = entity.MetaDescription ?? "",
            CreatedAt = Timestamp.FromDateTime(entity.CreatedAt),
            UpdatedAt = Timestamp.FromDateTime(entity.UpdatedAt),
            Metadata = entity.Metadata ?? "{}"
        };

        // Add author name as single entry in authors list
        if (!string.IsNullOrEmpty(entity.AuthorName))
        {
            proto.Authors.Add(entity.AuthorName);
        }

        if (entity.Keywords != null)
        {
            foreach (var keyword in entity.Keywords)
            {
                proto.Keywords.Add(keyword);
            }
        }

        // Category is a string, not a navigation property
        // CategoryData would need to be populated separately if needed

        return proto;
    }
}