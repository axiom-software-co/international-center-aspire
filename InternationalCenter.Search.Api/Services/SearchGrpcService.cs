using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using InternationalCenter.Shared.Infrastructure;
using InternationalCenter.Shared.Proto.Search;
using InternationalCenter.Shared.Proto.Common;
using Google.Protobuf.WellKnownTypes;
using System.Diagnostics;

namespace InternationalCenter.Search.Api.Services;

public class SearchGrpcService : SearchService.SearchServiceBase
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<SearchGrpcService> _logger;

    public SearchGrpcService(
        ApplicationDbContext context,
        IDistributedCache cache,
        ILogger<SearchGrpcService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public override async Task<UnifiedSearchResponse> UnifiedSearch(
        UnifiedSearchRequest request, 
        ServerCallContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var results = new List<SearchResult>();
            var stats = new Dictionary<string, long>();

            // Search News Articles
            if (request.Filters?.ContentTypes?.Contains("news") != false)
            {
                var newsResults = await SearchNewsArticles(request);
                results.AddRange(newsResults);
                stats["news"] = newsResults.Count;
            }

            // Search Research Articles
            if (request.Filters?.ContentTypes?.Contains("research") != false)
            {
                var researchResults = await SearchResearchArticles(request);
                results.AddRange(researchResults);
                stats["research"] = researchResults.Count;
            }

            // Search Services
            if (request.Filters?.ContentTypes?.Contains("service") != false)
            {
                var serviceResults = await SearchServices(request);
                results.AddRange(serviceResults);
                stats["service"] = serviceResults.Count;
            }

            // Search Events
            if (request.Filters?.ContentTypes?.Contains("event") != false)
            {
                var eventResults = await SearchEvents(request);
                results.AddRange(eventResults);
                stats["event"] = eventResults.Count;
            }

            // Sort combined results by relevance and date
            results = results.OrderByDescending(r => r.RelevanceScore)
                           .ThenByDescending(r => r.PublishedAt?.ToDateTime())
                           .ToList();

            // Apply pagination
            var pageSize = request.Pagination?.PageSize ?? 10;
            var page = request.Pagination?.Page ?? 1;
            var skip = (page - 1) * pageSize;
            var totalCount = results.Count;

            var pagedResults = results.Skip(skip).Take(pageSize).ToList();

            stopwatch.Stop();

            var response = new UnifiedSearchResponse
            {
                Query = request.Query,
                TotalTimeMs = stopwatch.ElapsedMilliseconds,
                Pagination = new PaginationResponse
                {
                    Page = page,
                    PageSize = pageSize,
                    Total = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                },
                Stats = new SearchStats
                {
                    AvgRelevanceScore = results.Count > 0 ? (float)results.Average(r => r.RelevanceScore) : 0
                }
            };

            foreach (var result in pagedResults)
            {
                response.Results.Add(result);
            }

            foreach (var stat in stats)
            {
                response.Stats.ResultsByType[stat.Key] = stat.Value;
            }

            // Add basic suggestions if requested
            if (request.IncludeSuggestions && !string.IsNullOrEmpty(request.Query))
            {
                var suggestions = await GenerateSearchSuggestions(request.Query);
                foreach (var suggestion in suggestions)
                {
                    response.Suggestions.Add(suggestion);
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing unified search: {Query}", request.Query);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<GetSearchSuggestionsResponse> GetSearchSuggestions(
        GetSearchSuggestionsRequest request, 
        ServerCallContext context)
    {
        try
        {
            var suggestions = await GenerateSearchSuggestions(request.Query, request.Limit);
            
            var response = new GetSearchSuggestionsResponse();
            foreach (var suggestion in suggestions)
            {
                response.Suggestions.Add(suggestion);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions: {Query}", request.Query);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<GetPopularSearchesResponse> GetPopularSearches(
        GetPopularSearchesRequest request, 
        ServerCallContext context)
    {
        try
        {
            // This would typically come from search analytics data
            // For now, return some static popular searches
            var popularSearches = new List<PopularSearch>
            {
                new PopularSearch
                {
                    Query = "artificial intelligence",
                    SearchCount = 1250,
                    ResultCount = 45,
                    LastSearched = Timestamp.FromDateTime(DateTime.UtcNow.AddHours(-2))
                },
                new PopularSearch
                {
                    Query = "international cooperation",
                    SearchCount = 980,
                    ResultCount = 67,
                    LastSearched = Timestamp.FromDateTime(DateTime.UtcNow.AddHours(-1))
                },
                new PopularSearch
                {
                    Query = "research funding",
                    SearchCount = 850,
                    ResultCount = 32,
                    LastSearched = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-30))
                }
            };

            var response = new GetPopularSearchesResponse();
            foreach (var search in popularSearches.Take(request.Limit))
            {
                response.Searches.Add(search);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular searches");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task StreamSearchResults(
        UnifiedSearchRequest request, 
        IServerStreamWriter<SearchResult> responseStream, 
        ServerCallContext context)
    {
        try
        {
            // Get unified search results
            var searchResponse = await UnifiedSearch(request, context);

            foreach (var result in searchResponse.Results)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    break;

                await responseStream.WriteAsync(result);
                await Task.Delay(50, context.CancellationToken); // Small delay for demonstration
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming search results: {Query}", request.Query);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task StreamSearchAnalytics(
        HealthCheckRequest request, 
        IServerStreamWriter<SearchAnalyticsEvent> responseStream, 
        ServerCallContext context)
    {
        try
        {
            // This would typically stream real search analytics events
            // For demonstration, send some mock events
            var random = new Random();
            var queries = new[] { "AI", "research", "international cooperation", "technology transfer" };

            while (!context.CancellationToken.IsCancellationRequested)
            {
                var analyticsEvent = new SearchAnalyticsEvent
                {
                    SessionId = Guid.NewGuid().ToString(),
                    Query = queries[random.Next(queries.Length)],
                    ResultCount = random.Next(10, 100),
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                    UserAgent = "Mozilla/5.0 (compatible; SearchBot/1.0)",
                    Source = "web",
                    SearchTimeMs = random.Next(50, 500)
                };

                await responseStream.WriteAsync(analyticsEvent);
                await Task.Delay(5000, context.CancellationToken); // Send event every 5 seconds
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming search analytics");
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
                ServiceName = "SearchService",
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
                ServiceName = "SearchService",
                Version = "1.0.0",
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
            };
        }
    }

    private async Task<List<SearchResult>> SearchNewsArticles(UnifiedSearchRequest request)
    {
        var query = _context.NewsArticles.AsQueryable();

        if (!string.IsNullOrEmpty(request.Query))
        {
            var searchTerm = $"%{request.Query}%";
            query = query.Where(a => 
                EF.Functions.ILike(a.Title, searchTerm) ||
                EF.Functions.ILike(a.Content, searchTerm) ||
                EF.Functions.ILike(a.Excerpt, searchTerm));
        }

        var articles = await query
            .Where(a => a.Status == "published")
            .OrderByDescending(a => a.PublishedAt)
            .Take(20) // Limit per content type
            .Include(a => a.Category)
            .ToListAsync();

        return articles.Select(a => new SearchResult
        {
            Id = a.Id,
            Title = a.Title,
            Slug = a.Slug,
            Excerpt = a.Excerpt ?? "",
            ContentType = "news",
            Category = a.Category ?? "",
            ImageUrl = a.ImageUrl ?? "",
            PublishedAt = Timestamp.FromDateTime(a.PublishedAt ?? DateTime.UtcNow),
            CreatedAt = Timestamp.FromDateTime(a.CreatedAt),
            RelevanceScore = CalculateRelevanceScore(request.Query, a.Title, a.Content),
            Author = a.AuthorName ?? "",
            Url = $"/news/{a.Slug}"
        }).ToList();
    }

    private async Task<List<SearchResult>> SearchResearchArticles(UnifiedSearchRequest request)
    {
        var query = _context.ResearchArticles.AsQueryable();

        if (!string.IsNullOrEmpty(request.Query))
        {
            var searchTerm = $"%{request.Query}%";
            query = query.Where(a => 
                EF.Functions.ILike(a.Title, searchTerm) ||
                EF.Functions.ILike(a.Excerpt, searchTerm) ||
                EF.Functions.ILike(a.Content, searchTerm) ||
                EF.Functions.ILike(a.AuthorName, searchTerm));
        }

        var articles = await query
            .Where(a => a.Status == "published")
            .OrderByDescending(a => a.PublishedAt)
            .Take(20)
            .Include(a => a.Category)
            .ToListAsync();

        return articles.Select(a => new SearchResult
        {
            Id = a.Id,
            Title = a.Title,
            Slug = a.Slug,
            Excerpt = a.Excerpt ?? "",
            ContentType = "research",
            Category = a.Category ?? "",
            ImageUrl = a.ImageUrl ?? "",
            PublishedAt = Timestamp.FromDateTime(a.PublishedAt ?? DateTime.UtcNow),
            CreatedAt = Timestamp.FromDateTime(a.CreatedAt),
            RelevanceScore = CalculateRelevanceScore(request.Query, a.Title, a.Excerpt),
            Author = a.AuthorName ?? "",
            Url = $"/research/{a.Slug}"
        }).ToList();
    }

    private async Task<List<SearchResult>> SearchServices(UnifiedSearchRequest request)
    {
        var query = _context.Services.AsQueryable();

        if (!string.IsNullOrEmpty(request.Query))
        {
            var searchTerm = $"%{request.Query}%";
            query = query.Where(s => 
                EF.Functions.ILike(s.Title, searchTerm) ||
                EF.Functions.ILike(s.Description, searchTerm) ||
                s.Technologies.Any(tech => EF.Functions.ILike(tech, searchTerm)));
        }

        var services = await query
            .Where(s => s.Available)
            .OrderByDescending(s => s.Featured)
            .ThenBy(s => s.SortOrder)
            .Take(20)
            .Include(s => s.Category)
            .ToListAsync();

        return services.Select(s => new SearchResult
        {
            Id = s.Id,
            Title = s.Title,
            Slug = s.Slug,
            Excerpt = s.Description ?? "",
            ContentType = "service",
            Category = s.Category ?? "",
            ImageUrl = s.Image ?? "",
            CreatedAt = Timestamp.FromDateTime(s.CreatedAt),
            RelevanceScore = CalculateRelevanceScore(request.Query, s.Title, s.Description),
            Url = $"/services/{s.Slug}"
        }).ToList();
    }

    private async Task<List<SearchResult>> SearchEvents(UnifiedSearchRequest request)
    {
        var query = _context.Events.AsQueryable();

        if (!string.IsNullOrEmpty(request.Query))
        {
            var searchTerm = $"%{request.Query}%";
            query = query.Where(e => 
                EF.Functions.ILike(e.Title, searchTerm) ||
                EF.Functions.ILike(e.Description, searchTerm) ||
                EF.Functions.ILike(e.Location, searchTerm));
        }

        var events = await query
            .Where(e => e.Status == "published")
            .OrderByDescending(e => e.StartDate)
            .Take(20)
            .Include(e => e.Category)
            .ToListAsync();

        return events.Select(e => new SearchResult
        {
            Id = e.Id,
            Title = e.Title,
            Slug = e.Slug,
            Excerpt = e.Description ?? "",
            ContentType = "event",
            Category = e.Category ?? "",
            ImageUrl = e.ImageUrl ?? "",
            PublishedAt = Timestamp.FromDateTime(e.StartDate),
            CreatedAt = Timestamp.FromDateTime(e.CreatedAt),
            RelevanceScore = CalculateRelevanceScore(request.Query, e.Title, e.Description),
            Url = $"/events/{e.Slug}"
        }).ToList();
    }

    private async Task<List<SearchSuggestion>> GenerateSearchSuggestions(string query, int limit = 5)
    {
        var suggestions = new List<SearchSuggestion>();

        if (string.IsNullOrEmpty(query) || query.Length < 2)
            return suggestions;

        // Get suggestions from existing content titles
        var newsTitles = await _context.NewsArticles
            .Where(a => EF.Functions.ILike(a.Title, $"%{query}%"))
            .Select(a => a.Title)
            .Take(limit)
            .ToListAsync();

        var researchTitles = await _context.ResearchArticles
            .Where(a => EF.Functions.ILike(a.Title, $"%{query}%"))
            .Select(a => a.Title)
            .Take(limit)
            .ToListAsync();

        foreach (var title in newsTitles.Concat(researchTitles).Distinct().Take(limit))
        {
            suggestions.Add(new SearchSuggestion
            {
                Query = title,
                Highlighted = title.Replace(query, $"<mark>{query}</mark>", StringComparison.OrdinalIgnoreCase),
                ResultCount = 1,
                Type = "query_completion"
            });
        }

        return suggestions;
    }

    private static float CalculateRelevanceScore(string? searchQuery, string? title, string? content)
    {
        if (string.IsNullOrEmpty(searchQuery))
            return 0.5f;

        var score = 0.0f;
        var query = searchQuery.ToLowerInvariant();

        if (!string.IsNullOrEmpty(title))
        {
            var titleLower = title.ToLowerInvariant();
            if (titleLower.Contains(query))
                score += titleLower.StartsWith(query) ? 1.0f : 0.8f;
        }

        if (!string.IsNullOrEmpty(content))
        {
            var contentLower = content.ToLowerInvariant();
            if (contentLower.Contains(query))
                score += 0.3f;
        }

        return Math.Min(score, 1.0f);
    }
}