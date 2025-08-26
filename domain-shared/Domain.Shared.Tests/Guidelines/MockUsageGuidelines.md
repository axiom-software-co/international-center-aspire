# Services API Mock Usage Guidelines

## Contract-First Testing Principles

This document establishes clear guidelines for mock usage across all Services API test projects to ensure consistent contract-first testing practices.

## Core Principle: Test Type Determines Mock Usage

```
Unit Tests      → Use Mocks for ALL dependencies
Integration Tests → Use Real implementations (NO mocks)
E2E Tests       → Use Real implementations (NO mocks)
```

## Detailed Guidelines

### 1. Unit Tests (.Tests.Unit projects)

**ALWAYS use mocks for:**
- ✅ Repository interfaces (`IServiceRepository`, `IServiceCategoryRepository`)
- ✅ External services (`IAuditService`, `ILogger<T>`)
- ✅ Database contexts (`IServicesDbContext`)
- ✅ HTTP clients and external APIs
- ✅ File system operations
- ✅ Time providers and system dependencies

**Example - Correct Unit Test:**
```csharp
public class CreateServiceUseCaseTests
{
    private readonly Mock<IServiceRepository> _mockRepository;
    private readonly Mock<ILogger<CreateServiceUseCase>> _mockLogger;
    private readonly CreateServiceUseCase _useCase;

    public CreateServiceUseCaseTests()
    {
        _mockRepository = new Mock<IServiceRepository>();
        _mockLogger = new Mock<ILogger<CreateServiceUseCase>>();
        _useCase = new CreateServiceUseCase(_mockRepository.Object, _mockLogger.Object);
    }
}
```

### 2. Integration Tests (.Tests.Integration projects)

**NEVER use mocks for:**
- ❌ Database operations (use real PostgreSQL through Aspire)
- ❌ Repository implementations (use real EF Core/Dapper)
- ❌ HTTP clients (use real Aspire-provided clients)
- ❌ Caching services (use real Redis through Aspire)
- ❌ Business logic (use real use case implementations)

**Example - Correct Integration Test:**
```csharp
public class AdminApiAspireIntegrationTests : AspireIntegrationTestBase
{
    // Uses real HttpClient from Aspire
    // Uses real PostgreSQL database
    // Uses real EF Core repositories
    // No mocks anywhere
}
```

### 3. E2E Tests (.Tests.EndToEnd projects)

**NEVER use mocks for:**
- ❌ Browser automation (use real Playwright)
- ❌ HTTP requests (use real API endpoints)
- ❌ Database state (use real database through Aspire)
- ❌ Authentication (simulate real auth flows)

## Mock Usage Anti-Patterns

### ❌ WRONG: Mixed Patterns
```csharp
public class BadIntegrationTest
{
    private readonly Mock<IServiceRepository> _mockRepo; // ❌ Mock in integration test
    private readonly HttpClient _realHttpClient;         // ❌ Mixed pattern
}
```

### ❌ WRONG: Testing Infrastructure in Unit Tests
```csharp
[Fact]
public async Task Repository_ShouldConnectToDatabase() // ❌ Infrastructure concern in unit test
{
    var repo = new ServiceRepository(_mockContext.Object);
    await repo.GetByIdAsync(serviceId); // ❌ Testing EF Core, not business logic
}
```

### ✅ CORRECT: Pure Business Logic in Unit Tests
```csharp
[Fact]
public async Task CreateService_WithValidRequest_ShouldReturnSuccessResult()
{
    // ARRANGE - Mock external dependencies
    _mockRepository.Setup(r => r.SlugExistsAsync(It.IsAny<Slug>(), null, CancellationToken.None))
                   .ReturnsAsync(false);
    
    // ACT - Test business logic only
    var result = await _useCase.ExecuteAsync(validRequest);
    
    // ASSERT - Verify business rules, not infrastructure
    Assert.True(result.IsSuccess);
}
```

## Project-Specific Guidelines

### Services.Admin.Api Tests

#### Unit Tests
- ✅ Mock `IServiceRepository`, `IServiceCategoryRepository`
- ✅ Mock `IAuditService` for medical-grade audit testing
- ✅ Mock `ILogger<T>` for all components
- ✅ Test business logic, validation, error handling

#### Integration Tests  
- ✅ Use `AspireAdminIntegrationTestBase`
- ✅ Real EF Core with PostgreSQL
- ✅ Real audit logging through database
- ✅ Real HTTP client for API testing

#### E2E Tests
- ✅ Use `AdminApiEndToEndTestBase`
- ✅ Real Playwright browser automation
- ✅ Real Entra External ID authentication simulation
- ✅ Real database state verification

### Services.Public.Api Tests

#### Unit Tests
- ✅ Mock `IServiceReadRepository`, `IServiceCategoryReadRepository`
- ✅ Mock `IDistributedCache` for caching logic
- ✅ Test business logic without infrastructure

#### Integration Tests
- ✅ Use `AspirePublicIntegrationTestBase`
- ✅ Real Dapper with PostgreSQL
- ✅ Real Redis for caching
- ✅ Real HTTP client for API testing

#### E2E Tests
- ✅ Use `PublicApiEndToEndTestBase`
- ✅ Real browser automation
- ✅ Real anonymous access patterns

## Enforcement Rules

### 1. Unit Test Verification Checklist
- [ ] No `DistributedApplication` or Aspire references
- [ ] No real database connections
- [ ] No `HttpClient` instances
- [ ] All dependencies are `Mock<T>`
- [ ] Tests focus on business logic only

### 2. Integration Test Verification Checklist
- [ ] Inherits from `AspireIntegrationTestBase` or similar
- [ ] No `Mock<T>` instances anywhere
- [ ] Uses real Aspire-orchestrated infrastructure
- [ ] Tests cross-component interactions
- [ ] Includes database setup/cleanup

### 3. E2E Test Verification Checklist
- [ ] Uses real browser automation
- [ ] Tests complete user workflows
- [ ] Verifies database state changes
- [ ] No mocks in test setup

## Common Violations and Fixes

### Violation: Repository Testing Infrastructure
```csharp
❌ WRONG:
[Fact]
public async Task AdminRepository_ShouldSaveToDatabase()
{
    var repo = new AdminServiceRepository(_realContext); // ❌ Testing EF Core
    await repo.AddAsync(service);
    await repo.SaveChangesAsync();
}

✅ CORRECT:
[Fact] 
public async Task AddAsync_WithValidService_ShouldCallDbContextAdd()
{
    // ARRANGE
    var service = CreateTestService();
    
    // ACT
    await _repository.AddAsync(service, CancellationToken.None);
    
    // ASSERT - Verify interaction, not infrastructure
    _mockContext.Verify(c => c.Services.AddAsync(service, CancellationToken.None), Times.Once);
}
```

### Violation: Mixed Mock/Real Usage
```csharp
❌ WRONG:
public class MixedTest : AspireIntegrationTestBase
{
    private readonly Mock<IServiceRepository> _mockRepo; // ❌ Mock in integration test
    
    [Fact]
    public async Task ShouldWorkWithMixedDependencies()
    {
        var response = await HttpClient.GetAsync("/api/services"); // Real HTTP
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(services); // Mock repo
    }
}

✅ CORRECT: Split into separate test types
// Unit Test
public class ServiceUseCaseUnitTests
{
    private readonly Mock<IServiceRepository> _mockRepo; // ✅ Unit test uses mocks
}

// Integration Test  
public class ServiceApiIntegrationTests : AspireIntegrationTestBase
{
    // ✅ Integration test uses real implementations only
}
```

## Migration Guide for Existing Tests

### Step 1: Identify Test Type
- Does it test business logic in isolation? → Unit Test (use mocks)
- Does it test component interactions? → Integration Test (no mocks)
- Does it test user workflows? → E2E Test (no mocks)

### Step 2: Apply Appropriate Pattern
- Unit: Mock all dependencies
- Integration: Use Aspire base classes
- E2E: Use Playwright base classes

### Step 3: Verify Compliance
Run the checklist appropriate for each test type.

## Benefits of Consistent Mock Usage

1. **Clear Test Boundaries** - Each test type has distinct responsibilities
2. **Reliable Test Feedback** - Unit tests fail for business logic, integration tests for infrastructure
3. **Faster Development** - Clear patterns reduce decision fatigue
4. **Better Maintainability** - Consistent patterns are easier to understand and modify
5. **Contract Compliance** - Ensures proper contract-first testing practices

## Conclusion

Following these guidelines ensures:
- ✅ Unit tests validate business logic contracts
- ✅ Integration tests validate system integration contracts  
- ✅ E2E tests validate user workflow contracts
- ✅ No mixed patterns that confuse test purposes
- ✅ Consistent, maintainable test suites across all Services API projects