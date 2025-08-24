using Grpc.Core;
using InternationalCenter.Shared.Infrastructure;
using InternationalCenter.Shared.Models;
using InternationalCenter.Shared.Proto.Contacts;
using InternationalCenter.Shared.Proto.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using System.ComponentModel.DataAnnotations;

namespace InternationalCenter.Contacts.Api.Services;

public class ContactsGrpcService : ContactsService.ContactsServiceBase
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ContactsGrpcService> _logger;

    public ContactsGrpcService(
        ApplicationDbContext context,
        IDistributedCache cache,
        ILogger<ContactsGrpcService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public override async Task<CreateContactResponse> CreateContact(
        CreateContactRequest request,
        ServerCallContext context)
    {
        try
        {
            // Validation
            var errors = new List<ErrorDetail>();

            if (string.IsNullOrEmpty(request.Name) || request.Name.Length > 100)
                errors.Add(new ErrorDetail { Code = "NAME_INVALID", Message = "Name is required and must be less than 100 characters", Field = "name" });

            if (string.IsNullOrEmpty(request.Email) || !IsValidEmail(request.Email) || request.Email.Length > 150)
                errors.Add(new ErrorDetail { Code = "EMAIL_INVALID", Message = "Valid email address is required and must be less than 150 characters", Field = "email" });

            if (!string.IsNullOrEmpty(request.Phone) && request.Phone.Length > 20)
                errors.Add(new ErrorDetail { Code = "PHONE_INVALID", Message = "Phone number must be less than 20 characters", Field = "phone" });

            if (string.IsNullOrEmpty(request.Subject) || request.Subject.Length > 200)
                errors.Add(new ErrorDetail { Code = "SUBJECT_INVALID", Message = "Subject is required and must be less than 200 characters", Field = "subject" });

            if (string.IsNullOrEmpty(request.Message) || request.Message.Length > 2000)
                errors.Add(new ErrorDetail { Code = "MESSAGE_INVALID", Message = "Message is required and must be less than 2000 characters", Field = "message" });

            if (errors.Any())
            {
                return new CreateContactResponse
                {
                    Status = new OperationStatus
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = { errors }
                    }
                };
            }

            // Create contact record
            var contact = new Shared.Models.Contact
            {
                Id = $"contact_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone ?? "",
                Subject = request.Subject,
                Message = request.Message,
                Status = "new",
                Type = !string.IsNullOrEmpty(request.Type) ? request.Type : "general",
                Source = !string.IsNullOrEmpty(request.Source) ? request.Source : "website",
                IsUrgent = request.IsUrgent,
                ConsentGiven = true,
                ConsentDate = DateTime.UtcNow,
                DataRetentionDate = DateTime.UtcNow.AddYears(2),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();

            return new CreateContactResponse
            {
                Contact = MapToProtoContact(contact),
                Status = new OperationStatus
                {
                    Success = true,
                    Message = "Contact form received successfully"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contact");
            return new CreateContactResponse
            {
                Status = new OperationStatus
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = { new ErrorDetail { Code = "INTERNAL_ERROR", Message = ex.Message } }
                }
            };
        }
    }

    public override async Task<GetContactsResponse> GetContacts(
        GetContactsRequest request,
        ServerCallContext context)
    {
        try
        {
            var page = request.Pagination?.Page ?? 1;
            var pageSize = Math.Min(request.Pagination?.PageSize ?? 20, 100);
            var status = request.Filter?.Status;
            var type = request.Filter?.Type;
            var source = request.Filter?.Source;
            var isUrgent = request.Filter?.IsUrgent;
            var sortBy = request.Sort?.SortBy ?? "date-desc";

            var query = _context.Contacts.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.Status == status);
            }

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(c => c.Type == type);
            }

            if (!string.IsNullOrEmpty(source))
            {
                query = query.Where(c => c.Source == source);
            }

            if (isUrgent.HasValue)
            {
                query = query.Where(c => c.IsUrgent == isUrgent.Value);
            }

            // Apply sorting
            query = sortBy switch
            {
                "name-asc" => query.OrderBy(c => c.Name),
                "name-desc" => query.OrderByDescending(c => c.Name),
                "email-asc" => query.OrderBy(c => c.Email),
                "email-desc" => query.OrderByDescending(c => c.Email),
                "date-asc" => query.OrderBy(c => c.CreatedAt),
                "date-desc" => query.OrderByDescending(c => c.CreatedAt),
                _ => query.OrderByDescending(c => c.CreatedAt)
            };

            var total = await query.CountAsync();
            var contacts = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new GetContactsResponse
            {
                Pagination = new PaginationResponse
                {
                    Page = page,
                    PageSize = pageSize,
                    Total = total,
                    TotalPages = (int)Math.Ceiling((double)total / pageSize)
                }
            };

            response.Contacts.AddRange(contacts.Select(MapToProtoContact));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contacts");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<GetContactResponse> GetContactById(
        GetContactByIdRequest request,
        ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Id))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Contact ID is required"));
            }

            var contact = await _context.Contacts
                .Where(c => c.Id == request.Id)
                .FirstOrDefaultAsync();

            if (contact == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Contact with ID '{request.Id}' not found"));
            }

            return new GetContactResponse
            {
                Contact = MapToProtoContact(contact)
            };
        }
        catch (RpcException)
        {
            throw; // Re-throw RPC exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contact by ID: {Id}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<UpdateContactStatusResponse> UpdateContactStatus(
        UpdateContactStatusRequest request,
        ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Id))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Contact ID is required"));
            }

            if (string.IsNullOrWhiteSpace(request.Status))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Status is required"));
            }

            var contact = await _context.Contacts
                .Where(c => c.Id == request.Id)
                .FirstOrDefaultAsync();

            if (contact == null)
            {
                return new UpdateContactStatusResponse
                {
                    Status = new OperationStatus
                    {
                        Success = false,
                        Message = "Contact not found",
                        Errors = { new ErrorDetail { Code = "NOT_FOUND", Message = $"Contact with ID '{request.Id}' not found" } }
                    }
                };
            }

            contact.Status = request.Status;
            contact.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(request.RespondedBy))
            {
                contact.RespondedBy = request.RespondedBy;
                contact.ResponseSentAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return new UpdateContactStatusResponse
            {
                Contact = MapToProtoContact(contact),
                Status = new OperationStatus
                {
                    Success = true,
                    Message = "Contact status updated successfully"
                }
            };
        }
        catch (RpcException)
        {
            throw; // Re-throw RPC exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contact status: {Id}", request.Id);
            return new UpdateContactStatusResponse
            {
                Status = new OperationStatus
                {
                    Success = false,
                    Message = "Internal server error",
                    Errors = { new ErrorDetail { Code = "INTERNAL_ERROR", Message = ex.Message } }
                }
            };
        }
    }

    public override async Task<GetContactStatsResponse> GetContactStats(
        GetContactStatsRequest request,
        ServerCallContext context)
    {
        try
        {
            var fromDate = request.FromDate?.ToDateTime() ?? DateTime.UtcNow.AddDays(-30);
            var toDate = request.ToDate?.ToDateTime() ?? DateTime.UtcNow;

            var query = _context.Contacts
                .Where(c => c.CreatedAt >= fromDate && c.CreatedAt <= toDate);

            var totalContacts = await query.CountAsync();
            var newContacts = await query.Where(c => c.Status == "new").CountAsync();
            var pendingContacts = await query.Where(c => c.Status == "pending").CountAsync();
            var resolvedContacts = await query.Where(c => c.Status == "resolved").CountAsync();
            var urgentContacts = await query.Where(c => c.IsUrgent).CountAsync();

            var contactsByType = await query
                .GroupBy(c => c.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            var contactsBySource = await query
                .GroupBy(c => c.Source)
                .Select(g => new { Source = g.Key, Count = g.Count() })
                .ToListAsync();

            var stats = new ContactStats
            {
                TotalContacts = totalContacts,
                NewContacts = newContacts,
                PendingContacts = pendingContacts,
                ResolvedContacts = resolvedContacts,
                UrgentContacts = urgentContacts
            };

            foreach (var item in contactsByType)
            {
                stats.ContactsByType[item.Type] = item.Count;
            }

            foreach (var item in contactsBySource)
            {
                stats.ContactsBySource[item.Source] = item.Count;
            }

            return new GetContactStatsResponse
            {
                Stats = stats
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contact statistics");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task StreamNewContacts(
        HealthCheckRequest request,
        IServerStreamWriter<Shared.Proto.Contacts.Contact> responseStream,
        ServerCallContext context)
    {
        try
        {
            // Simple implementation - in production, this would use SignalR or similar for real-time updates
            var newContacts = await _context.Contacts
                .Where(c => c.Status == "new")
                .OrderByDescending(c => c.CreatedAt)
                .Take(10)
                .ToListAsync();

            foreach (var contact in newContacts)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    break;

                await responseStream.WriteAsync(MapToProtoContact(contact));
                await Task.Delay(1000); // Simulate streaming delay
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming new contacts");
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
                ServiceName = "ContactsService",
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
                ServiceName = "ContactsService",
                Version = "1.0.0",
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
            };
        }
    }

    private static Shared.Proto.Contacts.Contact MapToProtoContact(Shared.Models.Contact contact)
    {
        var protoContact = new Shared.Proto.Contacts.Contact
        {
            Id = contact.Id,
            Name = contact.Name,
            Email = contact.Email,
            Phone = contact.Phone,
            Subject = contact.Subject,
            Message = contact.Message,
            Status = contact.Status,
            Type = contact.Type,
            Source = contact.Source,
            IsUrgent = contact.IsUrgent,
            CreatedAt = Timestamp.FromDateTime(contact.CreatedAt.ToUniversalTime()),
            UpdatedAt = Timestamp.FromDateTime(contact.UpdatedAt.ToUniversalTime())
        };

        if (!string.IsNullOrEmpty(contact.RespondedBy))
        {
            protoContact.RespondedBy = contact.RespondedBy;
        }

        if (contact.ResponseSentAt.HasValue)
        {
            protoContact.ResponseSentAt = Timestamp.FromDateTime(contact.ResponseSentAt.Value.ToUniversalTime());
        }

        if (!string.IsNullOrEmpty(contact.Metadata))
        {
            protoContact.Metadata = contact.Metadata;
        }

        return protoContact;
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var emailAttribute = new EmailAddressAttribute();
            return emailAttribute.IsValid(email);
        }
        catch
        {
            return false;
        }
    }
}