global using Infrastructure.SecretStore.Abstractions;
global using Infrastructure.SecretStore.Models;
global using Infrastructure.SecretStore.Services;
global using Infrastructure.SecretStore.Configuration;
global using Infrastructure.SecretStore.HealthChecks;

global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using Microsoft.Extensions.Diagnostics.HealthChecks;

global using FluentValidation;

global using Azure.Security.KeyVault.Secrets;
global using Azure.Security.KeyVault.Keys;
global using Azure.Security.KeyVault.Certificates;
global using Azure.Identity;
global using Azure.Core;

global using System.Text.Json;