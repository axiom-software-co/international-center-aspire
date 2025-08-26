using InternationalCenter.Services.Admin.Api.Tests.Integration.Infrastructure;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Tests.Shared.TestData;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace InternationalCenter.Services.Admin.Api.Tests.Integration;

/// <summary>
/// TDD RED-GREEN-REFACTOR: End-to-end Admin API workflow tests using Aspire Testing Framework
/// Tests complete Create/Update/Delete workflows with medical-grade audit trail verification
/// Uses real Aspire-orchestrated PostgreSQL and Redis - NO MOCKS
/// </summary>
public class AdminWorkflowIntegrationTests : AspireAdminIntegrationTestBase
{
    [Fact(DisplayName = "TDD RED: CreateService Should Persist To Real Database And Create Medical-Grade Audit Trail", Timeout = 30000)]
    public async Task CreateService_ShouldPersistToRealDatabaseAndCreateMedicalGradeAuditTrail()
    {
        // ARRANGE: Clear any existing test data for isolation
        await ClearTestDataAsync();

        // Create test service category first
        var testCategory = ServiceTestDataGenerator.GenerateCategory();
        await SeedServiceCategoryAsync(testCategory);

        // Prepare create service request payload
        var createRequest = new
        {
            Title = "Medical Equipment Consultation",
            Slug = "medical-equipment-consultation",
            ShortDescription = "Professional consultation for medical equipment",
            DetailedDescription = "Comprehensive consultation services for medical equipment selection and implementation",
            CategoryId = testCategory.Id.Value,
            Available = true,
            SortOrder = 1,
            Featured = true,
            Metadata = new
            {
                Icon = "fas fa-stethoscope",
                Image = "https://example.com/medical-equipment.jpg",
                MetaTitle = "Medical Equipment Consultation | International Center",
                MetaDescription = "Professional medical equipment consultation services",
                Technologies = new[] { "Medical Devices", "Consultation", "Healthcare" },
                Features = new[] { "Expert Advice", "Equipment Selection", "Implementation Support" },
                DeliveryModes = new[] { "In-Person", "Online" }
            }
        };

        var jsonContent = JsonContent.Create(createRequest);

        // ACT: TDD RED - Make HTTP POST request to Admin API through Aspire service discovery
        var response = await AdminApiClient.PostAsync("/api/admin/services", jsonContent);

        // ASSERT: TDD RED - This will initially fail because Admin API endpoints don't exist yet
        
        // Response should be successful
        Assert.True(response.IsSuccessStatusCode, $"Expected successful response but got {response.StatusCode}");

        // Parse response to get created service ID
        var responseContent = await response.Content.ReadAsStringAsync();
        var responseJson = JsonDocument.Parse(responseContent);
        var serviceId = responseJson.RootElement.GetProperty("id").GetString();
        
        Assert.NotNull(serviceId);
        Assert.NotEmpty(serviceId);

        // TDD GREEN: Verify service was persisted to real PostgreSQL database
        var persistedService = await GetServiceFromDatabaseAsync(ServiceId.Create(serviceId));
        Assert.NotNull(persistedService);
        Assert.Equal("Medical Equipment Consultation", persistedService.Title);
        Assert.Equal("medical-equipment-consultation", persistedService.Slug.Value);
        Assert.True(persistedService.Available);
        Assert.True(persistedService.Featured);

        // TDD RED: Medical-grade audit verification - This will fail until audit logging is implemented
        var auditLogExists = await VerifyMedicalGradeAuditLogExistsAsync(
            entityType: "Service",
            operation: "CREATE",
            entityId: serviceId);

        Assert.True(auditLogExists, 
            "Medical-grade audit log must be created for Service CREATE operation. " +
            "Admin API requires comprehensive audit trail for compliance.");
    }

    [Fact(DisplayName = "TDD RED: UpdateService Should Modify Real Database And Create Medical-Grade Audit Trail", Timeout = 30000)]
    public async Task UpdateService_ShouldModifyRealDatabaseAndCreateMedicalGradeAuditTrail()
    {
        // ARRANGE: Clear any existing test data and seed initial data
        await ClearTestDataAsync();

        var testCategory = ServiceTestDataGenerator.GenerateCategory();
        await SeedServiceCategoryAsync(testCategory);

        var existingService = ServiceTestDataGenerator.GenerateService();
        existingService.SetCategory(testCategory.Id);
        await SeedServiceAsync(existingService);

        // Prepare update service request
        var updateRequest = new
        {
            Title = "Updated Medical Consultation Service",
            ShortDescription = "Updated comprehensive medical consultation",
            Available = false,
            Featured = false
        };

        var jsonContent = JsonContent.Create(updateRequest);

        // ACT: TDD RED - Make HTTP PUT request to Admin API
        var response = await AdminApiClient.PutAsync($"/api/admin/services/{existingService.Id}", jsonContent);

        // ASSERT: TDD RED - This will fail because Admin API update endpoint doesn't exist
        
        // Response should be successful
        Assert.True(response.IsSuccessStatusCode, $"Expected successful response but got {response.StatusCode}");

        // TDD GREEN: Verify service was updated in real PostgreSQL database
        var updatedService = await GetServiceFromDatabaseAsync(existingService.Id);
        Assert.NotNull(updatedService);
        Assert.Equal("Updated Medical Consultation Service", updatedService.Title);
        Assert.False(updatedService.Available);
        Assert.False(updatedService.Featured);

        // TDD RED: Medical-grade audit verification for UPDATE operation
        var auditLogExists = await VerifyMedicalGradeAuditLogExistsAsync(
            entityType: "Service",
            operation: "UPDATE", 
            entityId: existingService.Id.ToString());

        Assert.True(auditLogExists,
            "Medical-grade audit log must be created for Service UPDATE operation. " +
            "Admin API requires complete audit trail for medical-grade compliance.");
    }

    [Fact(DisplayName = "TDD RED: DeleteService Should Remove From Real Database And Create Medical-Grade Audit Trail", Timeout = 30000)]
    public async Task DeleteService_ShouldRemoveFromRealDatabaseAndCreateMedicalGradeAuditTrail()
    {
        // ARRANGE: Clear any existing test data and seed service to delete
        await ClearTestDataAsync();

        var testCategory = ServiceTestDataGenerator.GenerateCategory();
        await SeedServiceCategoryAsync(testCategory);

        var serviceToDelete = ServiceTestDataGenerator.GenerateService();
        serviceToDelete.SetCategory(testCategory.Id);
        await SeedServiceAsync(serviceToDelete);

        // Verify service exists before deletion
        var existingService = await GetServiceFromDatabaseAsync(serviceToDelete.Id);
        Assert.NotNull(existingService);

        // ACT: TDD RED - Make HTTP DELETE request to Admin API
        var response = await AdminApiClient.DeleteAsync($"/api/admin/services/{serviceToDelete.Id}");

        // ASSERT: TDD RED - This will fail because Admin API delete endpoint doesn't exist
        
        // Response should be successful
        Assert.True(response.IsSuccessStatusCode, $"Expected successful response but got {response.StatusCode}");

        // TDD GREEN: Verify service was deleted from real PostgreSQL database
        var deletedService = await GetServiceFromDatabaseAsync(serviceToDelete.Id);
        Assert.Null(deletedService);

        // TDD RED: Medical-grade audit verification for DELETE operation
        var auditLogExists = await VerifyMedicalGradeAuditLogExistsAsync(
            entityType: "Service", 
            operation: "DELETE",
            entityId: serviceToDelete.Id.ToString());

        Assert.True(auditLogExists,
            "Medical-grade audit log must be created for Service DELETE operation. " +
            "Admin API requires complete audit trail for medical-grade compliance and data governance.");
    }

    [Fact(DisplayName = "TDD RED: AdminAPI Should Be Accessible Through Aspire Service Discovery", Timeout = 30000)]
    public async Task AdminApi_ShouldBeAccessibleThroughAspireServiceDiscovery()
    {
        // ACT: TDD RED - Make health check request to Admin API through Aspire
        var response = await AdminApiClient.GetAsync("/health");

        // ASSERT: Admin API should be reachable through Aspire orchestration
        Assert.True(response.IsSuccessStatusCode, 
            "Admin API should be accessible through Aspire service discovery. " +
            "Check AppHost configuration and Admin API startup.");
    }

    [Fact(DisplayName = "TDD GREEN: Real PostgreSQL Database Should Be Available Through Aspire", Timeout = 30000)]
    public async Task RealPostgreSqlDatabase_ShouldBeAvailableThroughAspire()
    {
        // ACT: Test database connectivity through Aspire-orchestrated PostgreSQL
        var canConnect = await DbContext.Database.CanConnectAsync();

        // ASSERT: Database should be accessible through Aspire orchestration
        Assert.True(canConnect, 
            "Real PostgreSQL database should be accessible through Aspire orchestration. " +
            "Check AppHost PostgreSQL configuration and connection strings.");
    }
}