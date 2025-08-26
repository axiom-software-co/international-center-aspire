using System.Text;

namespace Service.Audit.Services;

public sealed class HmacAuditSigningService : IAuditSigningService
{
    private readonly ILogger<HmacAuditSigningService> _logger;
    private readonly AuditServiceOptions _options;

    public HmacAuditSigningService(
        ILogger<HmacAuditSigningService> logger,
        IOptions<AuditServiceOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        
        if (string.IsNullOrEmpty(_options.SigningKey))
        {
            throw new InvalidOperationException("Signing key is required for HMAC audit signing service");
        }
    }

    public async Task<string> GenerateSignatureAsync(string data, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(data))
        {
            throw new ArgumentException("Data cannot be null or empty", nameof(data));
        }

        try
        {
            var keyBytes = Encoding.UTF8.GetBytes(_options.SigningKey!);
            var dataBytes = Encoding.UTF8.GetBytes(data);

            using var hmac = _options.SigningAlgorithm.ToUpperInvariant() switch
            {
                "HMACSHA256" => new HMACSHA256(keyBytes),
                "HMACSHA512" => new HMACSHA512(keyBytes),
                _ => throw new NotSupportedException($"Signing algorithm '{_options.SigningAlgorithm}' is not supported")
            };

            var hashBytes = await Task.Run(() => hmac.ComputeHash(dataBytes), cancellationToken);
            var signature = Convert.ToBase64String(hashBytes);

            _logger.LogDebug("Generated signature using {Algorithm} for data length {Length}", 
                _options.SigningAlgorithm, data.Length);

            return signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate signature using {Algorithm}", _options.SigningAlgorithm);
            throw;
        }
    }

    public async Task<bool> VerifySignatureAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        if (auditEvent == null)
        {
            throw new ArgumentNullException(nameof(auditEvent));
        }

        if (string.IsNullOrEmpty(auditEvent.Signature))
        {
            _logger.LogWarning("Audit event {AuditId} has no signature to verify", auditEvent.Id);
            return false;
        }

        var dataToSign = auditEvent.GetDataForSigning();
        return await VerifySignatureAsync(dataToSign, auditEvent.Signature, cancellationToken);
    }

    public async Task<bool> VerifySignatureAsync(string data, string signature, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(data))
        {
            throw new ArgumentException("Data cannot be null or empty", nameof(data));
        }

        if (string.IsNullOrEmpty(signature))
        {
            throw new ArgumentException("Signature cannot be null or empty", nameof(signature));
        }

        try
        {
            var expectedSignature = await GenerateSignatureAsync(data, cancellationToken);
            var isValid = string.Equals(expectedSignature, signature, StringComparison.Ordinal);

            _logger.LogDebug("Signature verification result: {IsValid} for data length {Length}", 
                isValid, data.Length);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify signature using {Algorithm}", _options.SigningAlgorithm);
            return false;
        }
    }
}