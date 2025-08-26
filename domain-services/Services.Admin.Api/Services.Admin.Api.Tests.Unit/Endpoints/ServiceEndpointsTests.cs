using InternationalCenter.Services.Admin.Api.Application.UseCases;
using InternationalCenter.Services.Admin.Api.Handlers;
using InternationalCenter.Services.Domain.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace InternationalCenter.Services.Admin.Api.Tests.Unit.Endpoints;

/// <summary>
/// TDD GREEN: Endpoint Layer integration tests with Use Case mocking at DI level
/// Tests HTTP routing, request/response serialization, status code mapping, input validation
/// Focuses on HTTP protocol concerns - Use Cases mocked, real ServiceHandlers used
/// </summary>
public class ServiceEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<ICreateServiceUseCase> _mockCreateUseCase;
    private readonly Mock<IUpdateServiceUseCase> _mockUpdateUseCase;
    private readonly Mock<IDeleteServiceUseCase> _mockDeleteUseCase;

    public ServiceEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _mockCreateUseCase = new Mock<ICreateServiceUseCase>();
        _mockUpdateUseCase = new Mock<IUpdateServiceUseCase>();
        _mockDeleteUseCase = new Mock<IDeleteServiceUseCase>();
    }

    [Fact(DisplayName = "TDD GREEN: POST /api/admin/services Should Route To CreateService Handler", Timeout = 5000)]
    public async Task PostServices_WithValidRequest_ShouldRouteToCreateServiceHandler()
    {
        // ARRANGE: Mock Use Case to return successful result
        var serviceId = Guid.NewGuid();
        _mockCreateUseCase.Setup(uc => uc.ExecuteAsync(
                It.IsAny<CreateServiceRequest>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateServiceResponse>.Success(new CreateServiceResponse 
            { 
                Success = true,
                ServiceId = serviceId.ToString(),
                Slug = "test-service",
                AuditTrail = new List<AdminAuditLogEntry>()
            }));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Replace Use Cases with mocks at DI level
                services.AddScoped(_ => _mockCreateUseCase.Object);
                services.AddScoped(_ => _mockUpdateUseCase.Object);
                services.AddScoped(_ => _mockDeleteUseCase.Object);
            });
        }).CreateClient();

        var request = new CreateServiceHandlerRequest
        {
            Title = "Test Service",
            Slug = "test-service",
            Description = "Test description"
        };

        // ACT: Test actual HTTP endpoint with real ServiceHandlers
        var response = await client.PostAsJsonAsync("/api/admin/services", request);

        // ASSERT: Should route correctly and return Created status
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        // Verify Use Case was called with correct parameters (contract focus)
        _mockCreateUseCase.Verify(uc => uc.ExecuteAsync(
            It.Is<CreateServiceRequest>(req => 
                req.Title == request.Title && 
                req.Slug == request.Slug &&
                req.Description == request.Description),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "TDD GREEN: PUT /api/admin/services/{id} Should Route To UpdateService Handler", Timeout = 5000)]
    public async Task PutService_WithValidRequest_ShouldRouteToUpdateServiceHandler()
    {
        // ARRANGE: Mock Use Case to return successful result
        _mockUpdateUseCase.Setup(uc => uc.ExecuteAsync(
                It.IsAny<UpdateServiceRequest>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UpdateServiceResponse>.Success(new UpdateServiceResponse 
            { 
                Success = true,
                ServiceId = "12345678-1234-1234-1234-123456789abc",
                Changes = new Dictionary<string, (object?, object?)>(),
                AuditTrail = new List<AdminAuditLogEntry>()
            }));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Replace Use Cases with mocks at DI level
                services.AddScoped(_ => _mockCreateUseCase.Object);
                services.AddScoped(_ => _mockUpdateUseCase.Object);
                services.AddScoped(_ => _mockDeleteUseCase.Object);
            });
        }).CreateClient();

        var serviceId = "12345678-1234-1234-1234-123456789abc";
        var request = new UpdateServiceHandlerRequest
        {
            Title = "Updated Service",
            Available = false
        };

        // ACT: Test actual HTTP endpoint with real ServiceHandlers
        var response = await client.PutAsJsonAsync($"/api/admin/services/{serviceId}", request);

        // ASSERT: Should route correctly and return NoContent status
        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);

        // Verify Use Case was called with correct parameters (contract focus)
        _mockUpdateUseCase.Verify(uc => uc.ExecuteAsync(
            It.Is<UpdateServiceRequest>(req => 
                req.ServiceId == serviceId &&
                req.Title == request.Title && 
                req.Available == request.Available),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "TDD GREEN: DELETE /api/admin/services/{id} Should Route To DeleteService Handler", Timeout = 5000)]
    public async Task DeleteService_WithValidId_ShouldRouteToDeleteServiceHandler()
    {
        // ARRANGE: Mock Use Case to return successful result
        _mockDeleteUseCase.Setup(uc => uc.ExecuteAsync(
                It.IsAny<DeleteServiceRequest>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DeleteServiceResponse>.Success(new DeleteServiceResponse 
            { 
                Success = true,
                ServiceId = "12345678-1234-1234-1234-123456789abc",
                DeletedAt = DateTime.UtcNow,
                DeletionType = "SOFT_DELETE",
                AuditTrail = new List<AdminAuditLogEntry>()
            }));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Replace Use Cases with mocks at DI level
                services.AddScoped(_ => _mockCreateUseCase.Object);
                services.AddScoped(_ => _mockUpdateUseCase.Object);
                services.AddScoped(_ => _mockDeleteUseCase.Object);
            });
        }).CreateClient();

        var serviceId = "12345678-1234-1234-1234-123456789abc";

        // ACT: Test actual HTTP endpoint with real ServiceHandlers
        var response = await client.DeleteAsync($"/api/admin/services/{serviceId}");

        // ASSERT: Should route correctly and return NoContent status
        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);

        // Verify Use Case was called with correct parameters (contract focus)
        _mockDeleteUseCase.Verify(uc => uc.ExecuteAsync(
            It.Is<DeleteServiceRequest>(req => req.ServiceId == serviceId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "TDD GREEN: POST /api/admin/services Should Handle Invalid JSON Serialization", Timeout = 5000)]
    public async Task PostServices_WithInvalidJson_ShouldReturnBadRequest()
    {
        // ARRANGE: Client configured for testing environment
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        }).CreateClient();

        // ACT: Send malformed JSON to test HTTP protocol validation
        var content = new StringContent("{invalid json", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/admin/services", content);

        // ASSERT: Should return BadRequest for malformed JSON
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "TDD GREEN: PUT /api/admin/services/{id} Should Validate Service ID Format In Route", Timeout = 5000)]
    public async Task PutService_WithInvalidServiceIdFormat_ShouldReturnNotFound()
    {
        // ARRANGE: Client configured for testing environment
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        }).CreateClient();

        var invalidId = "not-a-guid-format";
        var request = new UpdateServiceHandlerRequest
        {
            Title = "Updated Service"
        };

        // ACT: Test GUID route constraint validation (ASP.NET Core behavior)
        var response = await client.PutAsJsonAsync($"/api/admin/services/{invalidId}", request);

        // ASSERT: ASP.NET Core route constraints return NotFound when they fail to match
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "TDD GREEN: Content-Type application/json Should Be Required For POST/PUT", Timeout = 5000)]
    public async Task PostServices_WithoutJsonContentType_ShouldReturnUnsupportedMediaType()
    {
        // ARRANGE: Client configured for testing environment
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        }).CreateClient();

        // ACT: Test content-type validation
        var content = new StringContent("{\"title\":\"Test\"}", System.Text.Encoding.UTF8, "text/plain");
        var response = await client.PostAsync("/api/admin/services", content);

        // ASSERT: Should return UnsupportedMediaType for non-JSON content
        Assert.Equal(System.Net.HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact(DisplayName = "TDD GREEN: Response Should Include Medical-Grade Audit Headers", Timeout = 5000)]
    public async Task AdminEndpoints_ShouldIncludeMedicalGradeAuditHeaders()
    {
        // ARRANGE: Mock Use Case to return successful result with audit information
        var serviceId = Guid.NewGuid();
        _mockCreateUseCase.Setup(uc => uc.ExecuteAsync(
                It.IsAny<CreateServiceRequest>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreateServiceResponse>.Success(new CreateServiceResponse 
            { 
                Success = true,
                ServiceId = serviceId.ToString(),
                Slug = "test-service",
                AuditTrail = new List<AdminAuditLogEntry>()
            }));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Replace Use Cases with mocks at DI level
                services.AddScoped(_ => _mockCreateUseCase.Object);
                services.AddScoped(_ => _mockUpdateUseCase.Object);
                services.AddScoped(_ => _mockDeleteUseCase.Object);
            });
        }).CreateClient();

        var request = new CreateServiceHandlerRequest
        {
            Title = "Test Service",
            Slug = "test-service",
            Description = "Test description"
        };

        // ACT: Test actual HTTP endpoint for medical-grade audit header implementation
        var response = await client.PostAsJsonAsync("/api/admin/services", request);

        // ASSERT: Should include medical-grade audit headers from endpoint implementation
        Assert.True(response.Headers.Contains("X-Request-ID"), "Missing X-Request-ID audit header");
        Assert.True(response.Headers.Contains("X-Audit-Timestamp"), "Missing X-Audit-Timestamp audit header");
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        Assert.True(jsonResponse.TryGetProperty("auditTrail", out _), "Missing audit trail in response body");
    }
}