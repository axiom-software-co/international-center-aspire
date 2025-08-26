namespace Infrastructure.SecretStore.Abstractions;

public interface ISecretRotationService
{
    Task<SecretRotationResult> RotateSecretAsync(string secretName, 
        CancellationToken cancellationToken = default);
    
    Task<SecretRotationResult> RotateSecretAsync(string secretName, 
        Func<string, Task<string>> secretGenerator, CancellationToken cancellationToken = default);
    
    Task<bool> IsSecretDueForRotationAsync(string secretName, 
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<string>> GetSecretsDueForRotationAsync(
        CancellationToken cancellationToken = default);
    
    Task<SecretRotationSchedule?> GetRotationScheduleAsync(string secretName, 
        CancellationToken cancellationToken = default);
    
    Task SetRotationScheduleAsync(string secretName, SecretRotationSchedule schedule, 
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<SecretRotationHistory>> GetRotationHistoryAsync(string secretName, 
        CancellationToken cancellationToken = default);
}