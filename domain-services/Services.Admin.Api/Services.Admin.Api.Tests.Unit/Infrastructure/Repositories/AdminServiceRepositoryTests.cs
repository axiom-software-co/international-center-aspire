using InternationalCenter.Services.Admin.Api.Infrastructure.Repositories;
using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.Infrastructure.Interfaces;
using InternationalCenter.Services.Domain.Repositories;
using InternationalCenter.Services.Domain.Specifications;
using InternationalCenter.Services.Domain.ValueObjects;
using InternationalCenter.Tests.Shared.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InternationalCenter.Services.Admin.Api.Tests.Unit.Infrastructure.Repositories;

/// <summary>
/// Fast repository unit tests using interface mocking for TDD contract verification
/// Tests repository contract compliance, medical-grade audit logging, and business logic
/// Does NOT test infrastructure/database concerns - focuses on repository responsibilities
/// </summary>
public class AdminServiceRepositoryTests
{
    private readonly Mock<IServicesDbContext> _mockContext;
    private readonly Mock<DbSet<Service>> _mockServiceDbSet;
    private readonly Mock<ILogger<AdminServiceRepository>> _mockLogger;
    private readonly AdminServiceRepository _repository;

    public AdminServiceRepositoryTests()
    {
        _mockContext = new Mock<IServicesDbContext>();
        _mockServiceDbSet = new Mock<DbSet<Service>>();
        _mockLogger = new Mock<ILogger<AdminServiceRepository>>();

        // Setup DbContext interface to return mocked DbSet
        _mockContext.Setup(c => c.Services).Returns(_mockServiceDbSet.Object);
        
        _repository = new AdminServiceRepository(_mockContext.Object, _mockLogger.Object);
    }

    [Fact(DisplayName = "TDD RED: AdminServiceRepository Should Require Non-Null DbContext Interface", Timeout = 5000)]
    public void Constructor_WithNullDbContext_ShouldThrowArgumentNullException()
    {
        // ARRANGE: Null DbContext interface
        IServicesDbContext? nullContext = null;
        var logger = new Mock<ILogger<AdminServiceRepository>>().Object;

        // ACT & ASSERT: Should throw ArgumentNullException for null context
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new AdminServiceRepository(nullContext!, logger));
        Assert.Equal("context", exception.ParamName);
    }

    [Fact(DisplayName = "TDD RED: AdminServiceRepository Should Require Non-Null Logger", Timeout = 5000)]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // ARRANGE: Null Logger
        var context = new Mock<IServicesDbContext>().Object;
        ILogger<AdminServiceRepository>? nullLogger = null;

        // ACT & ASSERT: Should throw ArgumentNullException for null logger
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new AdminServiceRepository(context, nullLogger!));
        Assert.Equal("logger", exception.ParamName);
    }

    [Fact(DisplayName = "TDD GREEN: GetByIdAsync Should Call Medical-Grade Audit Logging", Timeout = 5000)]
    public async Task GetByIdAsync_ShouldCallMedicalGradeAuditLogging()
    {
        // ARRANGE: Service ID and mock service
        var serviceId = ServiceId.Create();
        var service = ServiceTestDataGenerator.GenerateService();
        
        _mockServiceDbSet.Setup(s => s.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Service, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        // Setup audit logging expectation
        _mockContext.Setup(c => c.AuditReadOperationAsync(
            "Service",
            "GetById", 
            1, 
            "admin"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // ACT: Execute repository method
        var result = await _repository.GetByIdAsync(serviceId);

        // ASSERT: Verify medical-grade audit logging contract compliance
        _mockContext.Verify(c => c.AuditReadOperationAsync(
            "Service",
            "GetById",
            1, // Found service = 1 record
            "admin"), Times.Once);

        Assert.Equal(service, result);
    }

    [Fact(DisplayName = "TDD GREEN: GetByIdAsync Should Audit Zero Records When Service Not Found", Timeout = 5000)]
    public async Task GetByIdAsync_WithNonExistentService_ShouldAuditZeroRecords()
    {
        // ARRANGE: Non-existent service ID
        var serviceId = ServiceId.Create();
        
        _mockServiceDbSet.Setup(s => s.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Service, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((Service?)null);

        // Setup audit logging expectation for not found scenario
        _mockContext.Setup(c => c.AuditReadOperationAsync(
            "Service",
            "GetById", 
            0, // Not found = 0 records
            "admin"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // ACT: Execute repository method
        var result = await _repository.GetByIdAsync(serviceId);

        // ASSERT: Verify audit logging for not found scenario
        _mockContext.Verify(c => c.AuditReadOperationAsync(
            "Service",
            "GetById",
            0, // Zero records found
            "admin"), Times.Once);

        Assert.Null(result);
    }

    [Fact(DisplayName = "TDD GREEN: GetAllAsync Should Log Medical-Grade Audit With Correct Count", Timeout = 5000)]
    public async Task GetAllAsync_ShouldLogMedicalGradeAuditWithCorrectCount()
    {
        // ARRANGE: Mock service collection
        var services = new List<Service>
        {
            ServiceTestDataGenerator.GenerateService(),
            ServiceTestDataGenerator.GenerateService(),
            ServiceTestDataGenerator.GenerateService()
        };

        _mockServiceDbSet.Setup(s => s.ToListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(services);

        // Setup audit logging expectation
        _mockContext.Setup(c => c.AuditReadOperationAsync(
            "Service",
            "GetAll",
            3, // Three services found
            "admin"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // ACT: Execute repository method
        var result = await _repository.GetAllAsync();

        // ASSERT: Verify medical-grade audit logging with correct count
        _mockContext.Verify(c => c.AuditReadOperationAsync(
            "Service",
            "GetAll",
            3, // Correct record count
            "admin"), Times.Once);

        Assert.Equal(3, result.Count);
    }

    [Fact(DisplayName = "TDD GREEN: AddAsync Should Call Medical-Grade Audit Write Operation", Timeout = 5000)]
    public async Task AddAsync_ShouldCallMedicalGradeAuditWriteOperation()
    {
        // ARRANGE: Service to add
        var service = ServiceTestDataGenerator.GenerateService();

        // Setup write audit logging expectation
        _mockContext.Setup(c => c.AuditWriteOperationAsync(
            "Service",
            "ADD",
            service.Id.ToString(),
            "admin"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // ACT: Execute repository method
        await _repository.AddAsync(service);

        // ASSERT: Verify medical-grade write audit logging occurred
        _mockContext.Verify(c => c.AuditWriteOperationAsync(
            "Service",
            "ADD",
            service.Id.ToString(),
            "admin"), Times.Once);

        // Verify DbSet.AddAsync was called through interface
        _mockServiceDbSet.Verify(s => s.AddAsync(service, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "TDD GREEN: UpdateAsync Should Call Medical-Grade Audit Update Operation", Timeout = 5000)]
    public async Task UpdateAsync_ShouldCallMedicalGradeAuditUpdateOperation()
    {
        // ARRANGE: Service to update
        var service = ServiceTestDataGenerator.GenerateService();

        // Setup update audit logging expectation
        _mockContext.Setup(c => c.AuditWriteOperationAsync(
            "Service",
            "UPDATE",
            service.Id.ToString(),
            "admin"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // ACT: Execute repository method
        await _repository.UpdateAsync(service);

        // ASSERT: Verify medical-grade update audit logging occurred
        _mockContext.Verify(c => c.AuditWriteOperationAsync(
            "Service",
            "UPDATE",
            service.Id.ToString(),
            "admin"), Times.Once);

        // Verify DbSet.Update was called through interface
        _mockServiceDbSet.Verify(s => s.Update(service), Times.Once);
    }

    [Fact(DisplayName = "TDD GREEN: DeleteAsync Should Call Medical-Grade Audit Delete Operation", Timeout = 5000)]
    public async Task DeleteAsync_ShouldCallMedicalGradeAuditDeleteOperation()
    {
        // ARRANGE: Service to delete
        var service = ServiceTestDataGenerator.GenerateService();

        // Setup delete audit logging expectation
        _mockContext.Setup(c => c.AuditWriteOperationAsync(
            "Service",
            "DELETE",
            service.Id.ToString(),
            "admin"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // ACT: Execute repository method
        await _repository.DeleteAsync(service);

        // ASSERT: Verify medical-grade delete audit logging occurred
        _mockContext.Verify(c => c.AuditWriteOperationAsync(
            "Service",
            "DELETE",
            service.Id.ToString(),
            "admin"), Times.Once);

        // Verify DbSet.Remove was called through interface
        _mockServiceDbSet.Verify(s => s.Remove(service), Times.Once);
    }

    [Fact(DisplayName = "TDD GREEN: SaveChangesAsync Should Call Interface SaveChanges", Timeout = 5000)]
    public async Task SaveChangesAsync_ShouldCallInterfaceSaveChanges()
    {
        // ARRANGE: Setup SaveChangesAsync expectation
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2)
            .Verifiable();

        // ACT: Execute save changes
        await _repository.SaveChangesAsync();

        // ASSERT: Verify SaveChangesAsync was called through interface
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "TDD GREEN: ExistsAsync Should Call Medical-Grade Audit With Boolean Result", Timeout = 5000)]
    public async Task ExistsAsync_ShouldCallMedicalGradeAuditWithBooleanResult()
    {
        // ARRANGE: Service ID that exists
        var serviceId = ServiceId.Create();
        
        _mockServiceDbSet.Setup(s => s.AnyAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Service, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Setup audit logging expectation for exists check
        _mockContext.Setup(c => c.AuditReadOperationAsync(
            "Service",
            "Exists",
            1, // Exists = 1
            "admin"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // ACT: Execute exists check
        var result = await _repository.ExistsAsync(serviceId);

        // ASSERT: Verify medical-grade audit logging occurred
        _mockContext.Verify(c => c.AuditReadOperationAsync(
            "Service",
            "Exists",
            1, // Exists translates to 1 for audit
            "admin"), Times.Once);

        Assert.True(result);
    }

    [Fact(DisplayName = "TDD GREEN: SlugExistsAsync Should Handle Exclude ID Parameter", Timeout = 5000)]
    public async Task SlugExistsAsync_WithExcludeId_ShouldApplyExclusionFilter()
    {
        // ARRANGE: Slug and service ID to exclude
        var slug = Slug.Create("test-slug");
        var excludeId = ServiceId.Create();
        
        _mockServiceDbSet.Setup(s => s.AnyAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Service, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Setup audit logging expectation
        _mockContext.Setup(c => c.AuditReadOperationAsync(
            "Service",
            "SlugExists",
            0, // Does not exist = 0
            "admin"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // ACT: Execute slug exists check with exclusion
        var result = await _repository.SlugExistsAsync(slug, excludeId);

        // ASSERT: Verify medical-grade audit logging occurred
        _mockContext.Verify(c => c.AuditReadOperationAsync(
            "Service",
            "SlugExists",
            0, // Does not exist
            "admin"), Times.Once);

        Assert.False(result);
    }

    [Fact(DisplayName = "TDD GREEN: GetBySpecificationAsync Should Apply Specification Contract", Timeout = 5000)]
    public async Task GetBySpecificationAsync_WithSpecification_ShouldApplySpecificationContract()
    {
        // ARRANGE: Mock specification
        var mockSpecification = new Mock<ISpecification<Service>>();
        mockSpecification.Setup(s => s.Criteria).Returns(svc => svc.Available == true);
        mockSpecification.Setup(s => s.Includes).Returns(new List<System.Linq.Expressions.Expression<Func<Service, object>>>());
        mockSpecification.Setup(s => s.IncludeStrings).Returns(new List<string>());
        mockSpecification.Setup(s => s.OrderBy).Returns((System.Linq.Expressions.Expression<Func<Service, object>>?)null);
        mockSpecification.Setup(s => s.OrderByDescending).Returns((System.Linq.Expressions.Expression<Func<Service, object>>?)null);
        mockSpecification.Setup(s => s.ThenByList).Returns(new List<System.Linq.Expressions.Expression<Func<Service, object>>>());
        mockSpecification.Setup(s => s.ThenByDescendingList).Returns(new List<System.Linq.Expressions.Expression<Func<Service, object>>>());
        mockSpecification.Setup(s => s.IsPagingEnabled).Returns(false);

        var services = new List<Service> { ServiceTestDataGenerator.GenerateService() };
        
        _mockServiceDbSet.Setup(s => s.ToListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(services);

        // Setup audit logging expectation
        _mockContext.Setup(c => c.AuditReadOperationAsync(
            "Service",
            "GetBySpecification",
            1,
            "admin"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // ACT: Execute repository method with specification
        var result = await _repository.GetBySpecificationAsync(mockSpecification.Object);

        // ASSERT: Verify medical-grade audit logging occurred
        _mockContext.Verify(c => c.AuditReadOperationAsync(
            "Service",
            "GetBySpecification",
            1, // One service returned
            "admin"), Times.Once);

        Assert.Single(result);
    }
}