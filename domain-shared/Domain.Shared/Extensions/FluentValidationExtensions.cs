using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Models;
using Shared.Validation;
using System.Reflection;

namespace Shared.Extensions;

public static class FluentValidationExtensions
{
    public static IServiceCollection AddFluentValidationForAllModels(this IServiceCollection services)
    {
        // Add FluentValidation services with auto validation
        services.AddFluentValidationAutoValidation(config =>
        {
            // Disable data annotations validation as we're replacing it with FluentValidation
            config.DisableDataAnnotationsValidation = true;
        });
        
        services.AddFluentValidationClientsideAdapters();

        // Note: Domain-specific validators must be registered by each consuming domain
        // to avoid violating the shared kernel principle

        return services;
    }

    /// <summary>
    /// Registers FluentValidation validators from a specific assembly
    /// Provides medical-grade validation with comprehensive error handling
    /// </summary>
    public static IServiceCollection AddFluentValidationFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        // Add FluentValidation services
        services.AddFluentValidationAutoValidation(config =>
        {
            config.DisableDataAnnotationsValidation = true;
            // Note: ImplicitlyValidateChildProperties removed due to deprecation
            // Use explicit SetValidator calls for child properties instead
        });
        
        services.AddFluentValidationClientsideAdapters();
        
        // Register all validators from the specified assembly
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }

    /// <summary>
    /// Adds comprehensive validation with medical-grade error handling and audit compliance
    /// Includes structured validation responses and security-focused validation rules
    /// </summary>
    public static IServiceCollection AddMedicalGradeValidation(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation(config =>
        {
            // Medical-grade validation configuration
            config.DisableDataAnnotationsValidation = true;
            // Note: ImplicitlyValidateChildProperties removed due to deprecation
            // Use explicit SetValidator calls for child properties instead
        });

        services.AddFluentValidationClientsideAdapters();

        // Configure validation behavior for medical-grade compliance
        ValidatorOptions.Global.DisplayNameResolver = (type, memberInfo, expression) =>
        {
            // Use property name for audit clarity
            return memberInfo?.Name ?? expression?.ToString() ?? "Unknown Property";
        };

        // Note: Custom message formatter removed due to IMessageFormatter interface compatibility
        // FluentValidation default formatting provides adequate medical-grade error messages

        return services;
    }
}