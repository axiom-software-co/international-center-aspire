# Services.Admin.Api Medical-Grade Performance Benchmarks

Performance benchmarking for Services.Admin.Api using BenchmarkDotNet with real Aspire infrastructure and medical-grade audit logging.

## Purpose

Medical-grade API performance benchmarks required for compliance and reliability. These benchmarks validate:
- **EF Core repository performance** - Complex database operations with audit logging
- **Admin use case performance** - Business logic with medical-grade validation and audit
- **API endpoint performance** - Full HTTP request/response with authentication simulation
- **Medical-grade audit performance** - Comprehensive audit logging overhead measurement

## Running Benchmarks

```bash
# Navigate to benchmark project
cd InternationalCenter.Services.Admin.Api.Benchmarks

# Run all benchmarks
dotnet run -c Release

# Run specific benchmark category
dotnet run -c Release --filter "*EfCore*"
dotnet run -c Release --filter "*UseCase*" 
dotnet run -c Release --filter "*Endpoint*"
dotnet run -c Release --filter "*Audit*"
```

## Benchmark Categories

### 1. EF Core Repository Benchmarks
Tests EF Core repository performance with medical-grade audit logging:
- `GetById with Include` - Critical for admin service editing
- `GetAll with Include and OrderBy` - Admin service listing
- `GetBySpecification` - Advanced admin filtering with complex queries
- `GetPaged` - Admin pagination with large datasets
- `Add/Update with audit` - Service creation/modification with audit overhead
- `SaveChanges` - EF Core transaction performance

### 2. Admin Use Case Benchmarks  
Tests business logic performance with medical-grade validation and audit:
- `CreateService` - Most critical admin operation
- `UpdateService` - Service modification workflows
- `DeleteService` - Service deletion with audit compliance
- Error handling scenarios with audit logging
- Validation performance with medical-grade requirements

### 3. Admin API Endpoint Benchmarks
Tests full HTTP request/response performance with authentication:
- `POST /api/admin/services` - Service creation (most critical)
- `PUT /api/admin/services/{id}` - Service updates
- `GET /api/admin/services` - Admin dashboard listings
- `DELETE /api/admin/services/{id}` - Service deletion
- Authentication/authorization error handling
- Concurrent admin user simulation

### 4. Medical-Grade Audit Benchmarks
Tests audit logging performance for compliance validation:
- Business event audit logging (Create, Update, Delete operations)
- Security event audit logging (authentication, authorization failures)
- Performance event audit logging (SLA monitoring)
- Database audit operations with EF Core
- Complex audit data serialization
- Concurrent audit logging simulation

## Medical-Grade Performance Targets

Based on admin workflow requirements and compliance needs:
- **Admin operations (Create/Update)**: < 300ms total (including audit)
- **Admin listings**: < 200ms database + EF Core overhead
- **Search operations**: < 400ms with complex queries and audit
- **Audit logging overhead**: < 50ms per audit event
- **Authentication checks**: < 100ms validation

## Infrastructure

Benchmarks use:
- **Real Aspire orchestration** - Production-realistic testing
- **PostgreSQL with EF Core** - Actual ORM performance with audit
- **Medical-grade audit simulation** - Realistic audit logging overhead
- **HTTP pipeline** - Full admin request/response cycle
- **Authentication simulation** - Entra External ID workflow simulation

## Compliance Focus

These benchmarks specifically measure:
- **Audit logging overhead** - Required for medical-grade compliance
- **EF Core change tracking** - Entity state management performance
- **Complex query performance** - Specification pattern with includes
- **Transaction performance** - SaveChanges with audit trails
- **Security event logging** - Authentication/authorization audit
- **Data integrity validation** - Medical-grade validation overhead

## Output

BenchmarkDotNet provides:
- **Mean/Median response times** with audit overhead
- **Memory allocation analysis** for EF Core operations
- **Throughput measurements** for admin operations  
- **Performance comparisons** between operations
- **Medical-grade compliance validation** timing

Results help ensure admin API meets medical-grade performance requirements while maintaining comprehensive audit compliance for regulatory needs.