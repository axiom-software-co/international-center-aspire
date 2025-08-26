using InternationalCenter.Services.Admin.Api.Infrastructure.Repositories;
using InternationalCenter.Services.Domain.Entities;
using InternationalCenter.Services.Domain.Infrastructure.Interfaces;
using InternationalCenter.Services.Domain.Repositories;
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
public class AdminServiceCategoryRepositoryTests
{
    private readonly Mock<IServicesDbContext> _mockContext;
    private readonly Mock<DbSet<ServiceCategory>> _mockCategoryDbSet;
    private readonly Mock<ILogger<AdminServiceCategoryRepository>> _mockLogger;
    private readonly AdminServiceCategoryRepository _repository;

    public AdminServiceCategoryRepositoryTests()
    {
        _mockContext = new Mock<IServicesDbContext>();
        _mockCategoryDbSet = new Mock<DbSet<ServiceCategory>>();
        _mockLogger = new Mock<ILogger<AdminServiceCategoryRepository>>();

        // Setup DbContext interface to return mocked DbSet
        _mockContext.Setup(c => c.ServiceCategories).Returns(_mockCategoryDbSet.Object);
        
        _repository = new AdminServiceCategoryRepository(_mockContext.Object, _mockLogger.Object);
    }

    [Fact(DisplayName = "TDD RED: AdminServiceCategoryRepository Should Require Non-Null DbContext Interface", Timeout = 5000)]
    public void Constructor_WithNullDbContext_ShouldThrowArgumentNullException()
    {
        // ARRANGE: Null DbContext interface
        IServicesDbContext? nullContext = null;
        var logger = new Mock<ILogger<AdminServiceCategoryRepository>>().Object;

        // ACT & ASSERT: Should throw ArgumentNullException for null context
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new AdminServiceCategoryRepository(nullContext!, logger));
        Assert.Equal("context", exception.ParamName);
    }

    [Fact(DisplayName = "TDD RED: AdminServiceCategoryRepository Should Require Non-Null Logger", Timeout = 5000)]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // ARRANGE: Null Logger
        var context = new Mock<IServicesDbContext>().Object;
        ILogger<AdminServiceCategoryRepository>? nullLogger = null;

        // ACT & ASSERT: Should throw ArgumentNullException for null logger
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new AdminServiceCategoryRepository(context, nullLogger!));
        Assert.Equal("logger", exception.ParamName);
    }

    [Fact(DisplayName = "TDD GREEN: GetByIdAsync Should Call Medical-Grade Audit Logging", Timeout = 5000)]
    public async Task GetByIdAsync_ShouldCallMedicalGradeAuditLogging()
    {
        // ARRANGE: Category ID and mock category
        var categoryId = ServiceCategoryId.Create(1);
        var category = ServiceTestDataGenerator.GenerateCategory();
        
        _mockCategoryDbSet.Setup(s => s.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ServiceCategory, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Setup audit logging expectation
        _mockContext.Setup(c => c.AuditReadOperationAsync(
            "ServiceCategory",
            "GetById", 
            1, 
            "admin"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // ACT: Execute repository method
        var result = await _repository.GetByIdAsync(categoryId);

        // ASSERT: Verify medical-grade audit logging contract compliance
        _mockContext.Verify(c => c.AuditReadOperationAsync(
            "ServiceCategory",
            "GetById",
            1, // Found category = 1 record
            "admin"), Times.Once);

        Assert.Equal(category, result);
    }

    [Fact(DisplayName = "TDD GREEN: GetBySlugAsync Should Call Medical-Grade Audit Trail", Timeout = 5000)]
    public async Task GetBySlugAsync_ShouldCallMedicalGradeAuditTrail()
    {
        // ARRANGE: Slug and mock category
        var slug = Slug.Create("test-category");
        var category = ServiceTestDataGenerator.GenerateCategory();
        
        _mockCategoryDbSet.Setup(s => s.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ServiceCategory, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Setup audit logging expectation
        _mockContext.Setup(c => c.AuditReadOperationAsync(
            "ServiceCategory",
            "GetBySlug", 
            1, 
            "admin"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // ACT: Execute repository method
        var result = await _repository.GetBySlugAsync(slug);

        // ASSERT: Verify medical-grade audit logging occurred
        _mockContext.Verify(c => c.AuditReadOperationAsync(
            "ServiceCategory",
            "GetBySlug",
            1, // Found category = 1 record
            "admin"), Times.Once);

        Assert.Equal(category, result);
    }

    [Fact(DisplayName = "TDD GREEN: GetAllAsync Should Log Medical-Grade Audit With ActiveOnly Parameter", Timeout = 5000)]
    public async Task GetAllAsync_WithActiveOnlyParameter_ShouldLogMedicalGradeAuditWithFilter()
    {
        // ARRANGE: Mock category collection
        var categories = new List<ServiceCategory>
        {
            ServiceTestDataGenerator.GenerateCategory(),
            ServiceTestDataGenerator.GenerateCategory()
        };

        _mockCategoryDbSet.Setup(s => s.ToListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        // Setup audit logging expectation
        _mockContext.Setup(c => c.AuditReadOperationAsync(
            "ServiceCategory",
            "GetAll",
            2, // Two categories found
            "admin"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // ACT: Execute repository method with activeOnly filter
        var result = await _repository.GetAllAsync(activeOnly: true);

        // ASSERT: Verify medical-grade audit logging with correct count
        _mockContext.Verify(c => c.AuditReadOperationAsync(
            "ServiceCategory",
            "GetAll",
            2, // Correct record count
            "admin"), Times.Once);

        Assert.Equal(2, result.Count);
    }

    [Fact(DisplayName = "TDD GREEN: GetActiveOrderedAsync Should Log Medical-Grade Audit For Ordered Results", Timeout = 5000)]
    public async Task GetActiveOrderedAsync_ShouldLogMedicalGradeAuditForOrderedResults()
    {
        // ARRANGE: Mock ordered categories
        var categories = new List<ServiceCategory>
        {
            ServiceTestDataGenerator.GenerateCategory(),
            ServiceTestDataGenerator.GenerateCategory(),
            ServiceTestDataGenerator.GenerateCategory()
        };

        _mockCategoryDbSet.Setup(s => s.ToListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        // Setup audit logging expectation
        _mockContext.Setup(c => c.AuditReadOperationAsync(
            "ServiceCategory",
            "GetActiveOrdered",
            3, // Three categories found
            "admin"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // ACT: Execute repository method
        var result = await _repository.GetActiveOrderedAsync();

        // ASSERT: Verify medical-grade audit logging occurred
        _mockContext.Verify(c => c.AuditReadOperationAsync(
            "ServiceCategory",
            "GetActiveOrdered",
            3, // Correct record count
            "admin"), Times.Once);

        Assert.Equal(3, result.Count);
    }

    [Fact(DisplayName = "TDD GREEN: AddAsync Should Call Medical-Grade Audit Write Operation", Timeout = 5000)]
    public async Task AddAsync_ShouldCallMedicalGradeAuditWriteOperation()
    {
        // ARRANGE: Category to add
        var category = ServiceTestDataGenerator.GenerateCategory();

        // Setup write audit logging expectation
        _mockContext.Setup(c => c.AuditWriteOperationAsync(
            "ServiceCategory",
            "ADD",
            category.Id.ToString(),
            "admin"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // ACT: Execute repository method
        await _repository.AddAsync(category);

        // ASSERT: Verify medical-grade write audit logging occurred
        _mockContext.Verify(c => c.AuditWriteOperationAsync(
            "ServiceCategory",
            "ADD",
            category.Id.ToString(),
            "admin"), Times.Once);

        // Verify DbSet.AddAsync was called through interface
        _mockCategoryDbSet.Verify(s => s.AddAsync(category, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "TDD GREEN: UpdateAsync Should Call Medical-Grade Audit Update Operation", Timeout = 5000)]
    public async Task UpdateAsync_ShouldCallMedicalGradeAuditUpdateOperation()
    {
        // ARRANGE: Category to update
        var category = ServiceTestDataGenerator.GenerateCategory();

        // Setup update audit logging expectation
        _mockContext.Setup(c => c.AuditWriteOperationAsync(
            "ServiceCategory",
            "UPDATE",
            category.Id.ToString(),
            "admin"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // ACT: Execute repository method
        await _repository.UpdateAsync(category);

        // ASSERT: Verify medical-grade update audit logging occurred
        _mockContext.Verify(c => c.AuditWriteOperationAsync(
            "ServiceCategory",
            "UPDATE",
            category.Id.ToString(),
            "admin"), Times.Once);

        // Verify DbSet.Update was called through interface
        _mockCategoryDbSet.Verify(s => s.Update(category), Times.Once);
    }

    [Fact(DisplayName = "TDD GREEN: DeleteAsync Should Call Medical-Grade Audit Delete Operation", Timeout = 5000)]
    public async Task DeleteAsync_ShouldCallMedicalGradeAuditDeleteOperation()
    {
        // ARRANGE: Category to delete
        var category = ServiceTestDataGenerator.GenerateCategory();

        // Setup delete audit logging expectation
        _mockContext.Setup(c => c.AuditWriteOperationAsync(
            "ServiceCategory",
            "DELETE",
            category.Id.ToString(),
            "admin"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // ACT: Execute repository method
        await _repository.DeleteAsync(category);

        // ASSERT: Verify medical-grade delete audit logging occurred
        _mockContext.Verify(c => c.AuditWriteOperationAsync(
            "ServiceCategory",
            "DELETE",
            category.Id.ToString(),
            "admin"), Times.Once);

        // Verify DbSet.Remove was called through interface
        _mockCategoryDbSet.Verify(s => s.Remove(category), Times.Once);
    }

    [Fact(DisplayName = "TDD GREEN: ExistsAsync Should Call Medical-Grade Audit With Boolean Result", Timeout = 5000)]
    public async Task ExistsAsync_ShouldCallMedicalGradeAuditWithBooleanResult()
    {
        // ARRANGE: Category ID that exists
        var categoryId = ServiceCategoryId.Create(1);
        
        _mockCategoryDbSet.Setup(s => s.AnyAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ServiceCategory, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Setup audit logging expectation for exists check
        _mockContext.Setup(c => c.AuditReadOperationAsync(
            "ServiceCategory",
            "Exists",
            1, // Exists = 1
            "admin"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // ACT: Execute exists check
        var result = await _repository.ExistsAsync(categoryId);

        // ASSERT: Verify medical-grade audit logging occurred
        _mockContext.Verify(c => c.AuditReadOperationAsync(
            "ServiceCategory",
            "Exists",
            1, // Exists translates to 1 for audit
            "admin"), Times.Once);

        Assert.True(result);
    }

    [Fact(DisplayName = "TDD GREEN: SlugExistsAsync Should Handle Exclude ID Parameter", Timeout = 5000)]
    public async Task SlugExistsAsync_WithExcludeId_ShouldApplyExclusionFilter()
    {
        // ARRANGE: Slug and category ID to exclude
        var slug = Slug.Create("test-category");
        var excludeId = ServiceCategoryId.Create(999);
        
        _mockCategoryDbSet.Setup(s => s.AnyAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ServiceCategory, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Setup audit logging expectation
        _mockContext.Setup(c => c.AuditReadOperationAsync(
            "ServiceCategory",
            "SlugExists",
            0, // Does not exist = 0
            "admin"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // ACT: Execute slug exists check with exclusion
        var result = await _repository.SlugExistsAsync(slug, excludeId);

        // ASSERT: Verify medical-grade audit logging occurred
        _mockContext.Verify(c => c.AuditReadOperationAsync(
            "ServiceCategory",
            "SlugExists",
            0, // Does not exist
            "admin"), Times.Once);

        Assert.False(result);
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
}