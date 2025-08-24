using Grpc.Core;
using InternationalCenter.Shared.Infrastructure;
using InternationalCenter.Shared.Models;
using InternationalCenter.Shared.Proto.News;
using InternationalCenter.Shared.Proto.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Google.Protobuf.WellKnownTypes;

namespace InternationalCenter.News.Api.Services;

public class NewsGrpcService : NewsService.NewsServiceBase
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<NewsGrpcService> _logger;

    public NewsGrpcService(
        ApplicationDbContext context,
        IDistributedCache cache,
        ILogger<NewsGrpcService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public override async Task<GetNewsArticlesResponse> GetNewsArticles(
        GetNewsArticlesRequest request,
        ServerCallContext context)
    {
        try
        {
            var page = request.Pagination?.Page ?? 1;
            var pageSize = Math.Min(request.Pagination?.PageSize ?? 20, 100);
            var category = request.Filter?.Category;
            var featured = request.Filter?.Featured;
            var sortBy = request.Sort?.SortBy ?? "date-desc";

            var cacheKey = $"ic:news:page_{page}:size_{pageSize}:category_{category}:featured_{featured}:sort_{sortBy}";

            // Try cache first
            var cachedResult = await _cache.GetStringAsync(cacheKey);
            if (cachedResult != null)
            {
                var cached = JsonSerializer.Deserialize<GetNewsArticlesResponse>(cachedResult);
                if (cached != null) return cached;
            }

            var query = _context.NewsArticles
                .Where(n => n.Status == "published");

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(n => EF.Functions.ILike(n.Category, category));
            }

            if (featured.HasValue)
            {
                query = query.Where(n => n.Featured == featured.Value);
            }

            // Apply sorting
            query = sortBy switch
            {
                "date-asc" => query.OrderBy(n => n.PublishedAt),
                "date-desc" => query.OrderByDescending(n => n.PublishedAt),
                "title-asc" => query.OrderBy(n => n.Title),
                "title-desc" => query.OrderByDescending(n => n.Title),
                _ => query.OrderByDescending(n => n.PublishedAt)
            };

            var total = await query.CountAsync();
            var articles = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new GetNewsArticlesResponse
            {
                Pagination = new PaginationResponse
                {
                    Page = page,
                    PageSize = pageSize,
                    Total = total,
                    TotalPages = (int)Math.Ceiling((double)total / pageSize)
                }
            };

            response.Articles.AddRange(articles.Select(MapToProtoNewsArticle));

            // Cache the response
            var serializedResult = JsonSerializer.Serialize(response);
            await _cache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving news articles");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<GetNewsArticleResponse> GetNewsArticleBySlug(
        GetNewsArticleBySlugRequest request,
        ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Slug))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Slug is required"));
            }

            var cacheKey = $"ic:news:slug:{request.Slug}";

            // Try cache first
            var cachedResult = await _cache.GetStringAsync(cacheKey);
            if (cachedResult != null)
            {
                var cached = JsonSerializer.Deserialize<GetNewsArticleResponse>(cachedResult);
                if (cached != null) return cached;
            }

            var article = await _context.NewsArticles
                .Where(n => n.Slug == request.Slug && n.Status == "published")
                .FirstOrDefaultAsync();

            if (article == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"News article with slug '{request.Slug}' not found"));
            }

            var response = new GetNewsArticleResponse
            {
                Article = MapToProtoNewsArticle(article)
            };

            // Cache the response
            var serializedResult = JsonSerializer.Serialize(response);
            await _cache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });

            return response;
        }
        catch (RpcException)
        {
            throw; // Re-throw RPC exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving news article by slug: {Slug}", request.Slug);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<SearchNewsArticlesResponse> SearchNewsArticles(
        SearchNewsArticlesRequest request,
        ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Search query is required"));
            }

            var page = request.Pagination?.Page ?? 1;
            var pageSize = Math.Min(request.Pagination?.PageSize ?? 20, 100);
            var category = request.Filter?.Category;
            var sortBy = request.Sort?.SortBy ?? "date-desc";

            var query = _context.NewsArticles
                .Where(n => n.Status == "published");

            // Full-text search
            var searchTerm = $"%{request.Query}%";
            query = query.Where(n =>
                EF.Functions.ILike(n.Title, searchTerm) ||
                EF.Functions.ILike(n.Excerpt, searchTerm) ||
                EF.Functions.ILike(n.Content, searchTerm));

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(n => EF.Functions.ILike(n.Category, category));
            }

            // Apply sorting
            query = sortBy switch
            {
                "title-asc" => query.OrderBy(n => n.Title),
                "title-desc" => query.OrderByDescending(n => n.Title),
                "date-asc" => query.OrderBy(n => n.PublishedAt),
                "date-desc" => query.OrderByDescending(n => n.PublishedAt),
                _ => query.OrderByDescending(n => n.PublishedAt)
            };

            var total = await query.CountAsync();
            var articles = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new SearchNewsArticlesResponse
            {
                Query = request.Query,
                Pagination = new PaginationResponse
                {
                    Page = page,
                    PageSize = pageSize,
                    Total = total,
                    TotalPages = (int)Math.Ceiling((double)total / pageSize)
                }
            };

            response.Articles.AddRange(articles.Select(MapToProtoNewsArticle));

            return response;
        }
        catch (RpcException)
        {
            throw; // Re-throw RPC exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching news articles with query: {Query}", request.Query);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<GetNewsCategoriesResponse> GetNewsCategories(
        GetNewsCategoriesRequest request,
        ServerCallContext context)
    {
        try
        {
            var cacheKey = "ic:news_categories:active";

            // Try cache first
            var cachedResult = await _cache.GetStringAsync(cacheKey);
            if (cachedResult != null)
            {
                var cached = JsonSerializer.Deserialize<GetNewsCategoriesResponse>(cachedResult);
                if (cached != null) return cached;
            }

            var query = _context.NewsCategories.AsQueryable();

            if (request.ActiveOnly)
            {
                query = query.Where(c => c.Active);
            }

            var categories = await query
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var response = new GetNewsCategoriesResponse();
            response.Categories.AddRange(categories.Select(MapToProtoNewsCategory));

            // Cache the response
            var serializedResult = JsonSerializer.Serialize(response);
            await _cache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving news categories");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<GetFeaturedNewsResponse> GetFeaturedNews(
        GetFeaturedNewsRequest request,
        ServerCallContext context)
    {
        try
        {
            var limit = request.Limit ?? 5;

            var articles = await _context.NewsArticles
                .Where(n => n.Status == "published" && n.Featured)
                .OrderByDescending(n => n.PublishedAt)
                .Take(limit)
                .ToListAsync();

            var response = new GetFeaturedNewsResponse();
            response.Articles.AddRange(articles.Select(MapToProtoNewsArticle));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving featured news");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<GetRecentNewsResponse> GetRecentNews(
        GetRecentNewsRequest request,
        ServerCallContext context)
    {
        try
        {
            var limit = Math.Min(request.Limit, 50); // Cap at 50

            var articles = await _context.NewsArticles
                .Where(n => n.Status == "published")
                .OrderByDescending(n => n.PublishedAt)
                .Take(limit)
                .ToListAsync();

            var response = new GetRecentNewsResponse();
            response.Articles.AddRange(articles.Select(MapToProtoNewsArticle));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent news");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task StreamNewsArticles(
        GetNewsArticlesRequest request,
        IServerStreamWriter<Shared.Proto.News.NewsArticle> responseStream,
        ServerCallContext context)
    {
        try
        {
            // Get initial articles
            var response = await GetNewsArticles(request, context);

            // Stream each article
            foreach (var article in response.Articles)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    break;

                await responseStream.WriteAsync(article);
                await Task.Delay(100); // Small delay for demonstration
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming news articles");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<HealthCheckResponse> HealthCheck(
        HealthCheckRequest request,
        ServerCallContext context)
    {
        try
        {
            // Test database connectivity
            await _context.Database.CanConnectAsync();

            return new HealthCheckResponse
            {
                Status = HealthStatus.Serving,
                ServiceName = "NewsService",
                Version = "1.0.0",
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return new HealthCheckResponse
            {
                Status = HealthStatus.NotServing,
                ServiceName = "NewsService",
                Version = "1.0.0",
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
            };
        }
    }

    private static Shared.Proto.News.NewsArticle MapToProtoNewsArticle(Shared.Models.NewsArticle article)
    {
        var protoArticle = new Shared.Proto.News.NewsArticle
        {
            Id = article.Id,
            Title = article.Title,
            Slug = article.Slug,
            Excerpt = article.Excerpt,
            Content = article.Content,
            AuthorName = article.AuthorName,
            AuthorEmail = article.AuthorEmail,
            Featured = article.Featured,
            Status = article.Status,
            Category = article.Category,
            ImageUrl = article.ImageUrl,
            MetaTitle = article.MetaTitle,
            MetaDescription = article.MetaDescription,
            CreatedAt = Timestamp.FromDateTime(article.CreatedAt.ToUniversalTime()),
            UpdatedAt = Timestamp.FromDateTime(article.UpdatedAt.ToUniversalTime()),
            Metadata = article.Metadata
        };

        if (article.PublishedAt.HasValue)
        {
            protoArticle.PublishedAt = Timestamp.FromDateTime(article.PublishedAt.Value.ToUniversalTime());
        }

        protoArticle.Tags.AddRange(article.Tags);

        if (!string.IsNullOrEmpty(article.SearchVector))
        {
            protoArticle.SearchVector = article.SearchVector;
        }

        return protoArticle;
    }

    private static Shared.Proto.News.NewsCategory MapToProtoNewsCategory(Shared.Models.NewsCategory category)
    {
        return new Shared.Proto.News.NewsCategory
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Slug = category.Slug,
            DisplayOrder = category.DisplayOrder,
            Active = category.Active,
            CreatedAt = Timestamp.FromDateTime(category.CreatedAt.ToUniversalTime()),
            UpdatedAt = Timestamp.FromDateTime(category.UpdatedAt.ToUniversalTime())
        };
    }
}