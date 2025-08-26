namespace InternationalCenter.Tests.Shared.Contracts;

/// <summary>
/// Contract interface for gateway audit logging behavior testing
/// Ensures medical-grade audit compliance with zero data loss
/// Contract-first testing approach without knowledge of concrete implementations
/// Public Gateway: Anonymous usage pattern logging
/// Admin Gateway: Medical-grade audit with user context tracking
/// </summary>
public interface IGatewayAuditContract
{
    /// <summary>
    /// Contract test: Verify gateway logs all requests with required audit fields
    /// Admin Gateway: User ID, correlation ID, request URL, timestamp, app version
    /// Public Gateway: Anonymous ID, correlation ID, request URL, timestamp, app version
    /// </summary>
    Task VerifyAuditContract_WithAnyRequest_LogsRequiredFields();
    
    /// <summary>
    /// Contract test: Verify gateway maintains correlation ID throughout request chain
    /// </summary>
    Task VerifyAuditContract_WithCorrelationId_MaintainsTraceability();
    
    /// <summary>
    /// Contract test: Verify gateway logs authentication events
    /// Admin Gateway: Login, logout, authentication failures
    /// Public Gateway: N/A (anonymous access)
    /// </summary>
    Task VerifyAuditContract_WithAuthenticationEvent_LogsSecurityEvent();
    
    /// <summary>
    /// Contract test: Verify gateway logs authorization failures for security analysis
    /// </summary>
    Task VerifyAuditContract_WithAuthorizationFailure_LogsSecurityViolation();
    
    /// <summary>
    /// Contract test: Verify gateway logs rate limiting violations
    /// </summary>
    Task VerifyAuditContract_WithRateLimitViolation_LogsComplianceEvent();
    
    /// <summary>
    /// Contract test: Verify gateway logs security policy violations
    /// </summary>
    Task VerifyAuditContract_WithSecurityViolation_LogsSecurityEvent();
    
    /// <summary>
    /// Contract test: Verify gateway audit logs persist to database correctly
    /// Admin Gateway: EF Core with PostgreSQL for zero data loss
    /// Public Gateway: Structured logging only
    /// </summary>
    Task VerifyAuditContract_WithDatabasePersistence_SavesAuditLog();
    
    /// <summary>
    /// Contract test: Verify gateway audit logging uses structured format
    /// </summary>
    Task VerifyAuditContract_WithStructuredLogging_FollowsStandards();
    
    /// <summary>
    /// Contract test: Verify gateway audit logging handles failures gracefully
    /// </summary>
    Task VerifyAuditContract_WithLoggingFailure_DoesNotAffectRequest();
    
    /// <summary>
    /// Contract test: Verify gateway audit logs contain no sensitive information
    /// </summary>
    Task VerifyAuditContract_WithSensitiveData_RedactsProperly();
    
    /// <summary>
    /// Contract test: Verify gateway audit logs support retention policies
    /// </summary>
    Task VerifyAuditContract_WithRetentionPolicy_MaintainsCompliance();
    
    /// <summary>
    /// Contract test: Verify gateway audit logging performance under load
    /// </summary>
    Task VerifyAuditContract_WithHighLoad_MaintainsPerformance();
}