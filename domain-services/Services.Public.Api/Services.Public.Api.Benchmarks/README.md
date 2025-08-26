# Services.Public.Api Performance Benchmarks

Performance benchmarking for Services.Public.Api using BenchmarkDotNet with real Aspire infrastructure.

## Purpose

Performance regressions may go undetected and compromise public website performance. These benchmarks validate:
- **Dapper repository performance** - Critical database operations
- **Use case performance** - Business logic with validation and caching
- **API endpoint performance** - Full HTTP request/response cycles

## Running Benchmarks

```bash
# Navigate to benchmark project
cd InternationalCenter.Services.Public.Api.Benchmarks

# Run all benchmarks
dotnet run -c Release

# Run specific benchmark category
dotnet run -c Release --filter "*Repository*"
dotnet run -c Release --filter "*UseCase*" 
dotnet run -c Release --filter "*Endpoint*"
```

## Benchmark Categories

### 1. Repository Benchmarks
Tests Dapper-based repository performance with real PostgreSQL:
- `GetBySlug` - Critical path for service pages
- `GetPublished` - Homepage and directory listings  
- `GetFeatured` - Featured services for homepage
- `GetPaged` - Pagination performance
- `Search` - Full-text search functionality
- `Count` - Statistics and pagination metadata

### 2. Use Case Benchmarks  
Tests business logic performance including validation, caching, and error handling:
- `GetServiceBySlug` - Most critical use case for website
- `GetServiceCategories` - Navigation and filtering
- `ServiceQuery` - Consolidated query operations
- Error handling scenarios

### 3. API Endpoint Benchmarks
Tests full HTTP request/response performance:
- `GET /api/services/{slug}` - Most critical endpoint
- `GET /api/services` - Published services listing
- `GET /api/services/featured` - Homepage featured content
- `GET /api/categories` - Navigation categories
- Pagination, search, and error handling endpoints

## Performance Targets

Based on public website requirements:
- **Service page load (GetBySlug)**: < 100ms database, < 200ms total
- **Homepage (Featured/Published)**: < 150ms database, < 300ms total  
- **Search operations**: < 250ms database, < 500ms total
- **Category navigation**: < 50ms database, < 100ms total

## Infrastructure

Benchmarks use:
- **Real Aspire orchestration** - Production-realistic testing
- **PostgreSQL database** - Actual database performance
- **Redis caching** - Realistic caching behavior
- **HTTP pipeline** - Full request/response cycle

## Output

BenchmarkDotNet provides:
- **Mean/Median response times**
- **Memory allocation analysis** 
- **Throughput measurements**
- **Performance comparisons**
- **Statistical analysis**

Results help identify performance regressions and optimization opportunities critical for public website user experience.