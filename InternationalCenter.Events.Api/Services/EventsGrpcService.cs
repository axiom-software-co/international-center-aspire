using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using InternationalCenter.Shared.Infrastructure;
using InternationalCenter.Shared.Proto.Events;
using InternationalCenter.Shared.Proto.Common;
using System.Text.Json;
using Google.Protobuf.WellKnownTypes;

namespace InternationalCenter.Events.Api.Services;

public class EventsGrpcService : EventsService.EventsServiceBase
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<EventsGrpcService> _logger;

    public EventsGrpcService(
        ApplicationDbContext context,
        IDistributedCache cache,
        ILogger<EventsGrpcService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public override async Task<GetEventsResponse> GetEvents(
        GetEventsRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("GetEvents called with request: {@Request}", request);
            
            var page = request.Pagination?.Page ?? 1;
            var pageSize = Math.Min(request.Pagination?.PageSize ?? 20, 100);
            var category = request.Filter?.Category;
            var featured = request.Filter?.Featured;
            var sortBy = request.Sort?.SortBy ?? "start_date";

            _logger.LogInformation("GetEvents parameters: page={Page}, pageSize={PageSize}, category={Category}, featured={Featured}, sortBy={SortBy}", 
                page, pageSize, category, featured, sortBy);

            var cacheKey = $"ic:events:page_{page}:size_{pageSize}:category_{category}:featured_{featured}:sort_{sortBy}";

            var cachedResult = await _cache.GetStringAsync(cacheKey);
            if (cachedResult != null)
            {
                var cached = JsonSerializer.Deserialize<GetEventsResponse>(cachedResult);
                if (cached != null) return cached;
            }

            var query = _context.Events.Where(e => e.Status == "published");

            // Apply filters
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(e => e.Category == category);
            }

            if (featured.HasValue)
            {
                query = query.Where(e => e.Featured == featured.Value);
            }

            // Apply event-specific filters (simplified for initial implementation)
            if (request.EventFilter != null)
            {
                var eventFilter = request.EventFilter;
                
                // For now, skip complex protobuf wrapper filtering
                // This can be enhanced later once basic functionality is working
                if (eventFilter.FromDate != null)
                {
                    var fromDate = eventFilter.FromDate.ToDateTime();
                    query = query.Where(e => e.StartDate >= fromDate);
                }

                if (eventFilter.ToDate != null)
                {
                    var toDate = eventFilter.ToDate.ToDateTime();
                    query = query.Where(e => e.EndDate <= toDate);
                }
            }

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "start_date" => query.OrderBy(e => e.StartDate),
                "title" => query.OrderBy(e => e.Title),
                "created_at" => query.OrderByDescending(e => e.CreatedAt),
                _ => query.OrderBy(e => e.StartDate)
            };

            var total = await query.CountAsync();
            _logger.LogInformation("GetEvents found {Total} events matching query", total);
            
            var events = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _logger.LogInformation("GetEvents retrieved {Count} events for page {Page}", events.Count, page);
            
            if (events.Any())
            {
                _logger.LogInformation("First event: {Title} (ID: {Id})", events.First().Title, events.First().Id);
            }

            var response = new GetEventsResponse
            {
                Pagination = new PaginationResponse
                {
                    Page = page,
                    PageSize = pageSize,
                    Total = total,
                    TotalPages = (int)Math.Ceiling((double)total / pageSize)
                }
            };

            response.Events.AddRange(events.Select(MapToProtoEvent));
            
            var serializedResult = JsonSerializer.Serialize(response);
            await _cache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    public override async Task<GetEventResponse> GetEventBySlug(
        GetEventBySlugRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("GetEventBySlug called with slug: {Slug}", request.Slug);

            var eventEntity = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Slug == request.Slug && e.Status == "published");

            if (eventEntity == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Event with slug '{request.Slug}' not found"));
            }

            _logger.LogInformation("Found event: {Title} (ID: {Id})", eventEntity.Title, eventEntity.Id);

            var response = new GetEventResponse
            {
                Event = MapToProtoEvent(eventEntity)
            };

            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event by slug: {Slug}", request.Slug);
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    public override async Task<GetEventCategoriesResponse> GetEventCategories(
        GetEventCategoriesRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("GetEventCategories called");

            // Get distinct categories from events
            var categories = await _context.Events
                .Where(e => e.Status == "published" && !string.IsNullOrEmpty(e.Category))
                .Select(e => e.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            _logger.LogInformation("Found {Count} event categories", categories.Count);

            var response = new GetEventCategoriesResponse();
            
            // Create category objects
            var categoryObjects = categories.Select((cat, index) => new EventCategory
            {
                Id = $"cat-{index + 1}",
                Name = cat,
                Description = $"Events in {cat} category",
                Slug = cat.ToLower().Replace(" ", "-"),
                DisplayOrder = index + 1,
                Active = true,
                CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow),
                UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
            });

            response.Categories.AddRange(categoryObjects);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event categories");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    public override async Task<GetFeaturedEventsResponse> GetFeaturedEvents(
        GetFeaturedEventsRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("GetFeaturedEvents called");

            var limit = request.Limit != null ? request.Limit.Value : 5;

            var events = await _context.Events
                .Where(e => e.Status == "published" && e.Featured)
                .OrderBy(e => e.StartDate)
                .Take(limit)
                .ToListAsync();

            _logger.LogInformation("Found {Count} featured events", events.Count);

            var response = new GetFeaturedEventsResponse();
            response.Events.AddRange(events.Select(MapToProtoEvent));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving featured events");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    public override async Task<GetUpcomingEventsResponse> GetUpcomingEvents(
        GetUpcomingEventsRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("GetUpcomingEvents called");

            var limit = request.Limit > 0 ? request.Limit : 10;
            var daysAhead = request.DaysAhead > 0 ? request.DaysAhead : 30;
            
            var startDate = DateTime.UtcNow;
            var endDate = startDate.AddDays(daysAhead);

            var events = await _context.Events
                .Where(e => e.Status == "published" && e.StartDate >= startDate && e.StartDate <= endDate)
                .OrderBy(e => e.StartDate)
                .Take(limit)
                .ToListAsync();

            _logger.LogInformation("Found {Count} upcoming events in next {Days} days", events.Count, daysAhead);

            var response = new GetUpcomingEventsResponse();
            response.Events.AddRange(events.Select(MapToProtoEvent));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving upcoming events");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    public override async Task<HealthCheckResponse> HealthCheck(
        HealthCheckRequest request,
        ServerCallContext context)
    {
        try
        {
            // Check database connectivity
            await _context.Database.CanConnectAsync();

            return new HealthCheckResponse
            {
                Status = HealthStatus.Serving,
                ServiceName = "EventsService",
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
                ServiceName = "EventsService",
                Version = "1.0.0",
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
            };
        }
    }

    private InternationalCenter.Shared.Proto.Events.Event MapToProtoEvent(InternationalCenter.Shared.Models.Event eventEntity)
    {
        return new InternationalCenter.Shared.Proto.Events.Event
        {
            Id = eventEntity.Id,
            Title = eventEntity.Title,
            Slug = eventEntity.Slug,
            Description = eventEntity.Description,
            Content = eventEntity.Content,
            StartDate = Timestamp.FromDateTime(eventEntity.StartDate.ToUniversalTime()),
            EndDate = Timestamp.FromDateTime(eventEntity.EndDate.ToUniversalTime()),
            Location = eventEntity.Location,
            Address = eventEntity.Address,
            IsVirtual = eventEntity.IsVirtual,
            VirtualLink = eventEntity.VirtualLink,
            Status = eventEntity.Status,
            Featured = eventEntity.Featured,
            Category = eventEntity.Category,
            Organizer = eventEntity.OrganizerName,
            ContactEmail = eventEntity.OrganizerEmail,
            ContactPhone = eventEntity.OrganizerPhone,
            RegistrationDeadline = eventEntity.RegistrationDeadline.HasValue 
                ? Timestamp.FromDateTime(eventEntity.RegistrationDeadline.Value.ToUniversalTime())
                : null,
            MaxCapacity = eventEntity.MaxAttendees,
            CurrentRegistrations = eventEntity.CurrentAttendees,
            RegistrationRequired = eventEntity.RequiresRegistration,
            IsFree = eventEntity.IsFree,
            PriceInfo = eventEntity.IsFree ? "Free" : $"{eventEntity.Price:C} {eventEntity.Currency}",
            ImageUrl = eventEntity.ImageUrl,
            MetaTitle = eventEntity.MetaTitle,
            MetaDescription = eventEntity.MetaDescription,
            CreatedAt = Timestamp.FromDateTime(eventEntity.CreatedAt.ToUniversalTime()),
            UpdatedAt = Timestamp.FromDateTime(eventEntity.UpdatedAt.ToUniversalTime()),
            Metadata = eventEntity.Metadata
        };
    }
}