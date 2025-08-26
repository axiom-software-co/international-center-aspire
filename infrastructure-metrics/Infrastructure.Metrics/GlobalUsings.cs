global using Infrastructure.Metrics.Abstractions;
global using Infrastructure.Metrics.Models;
global using Infrastructure.Metrics.Services;
global using Infrastructure.Metrics.Configuration;
global using Infrastructure.Metrics.Middleware;

global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;

global using FluentValidation;

global using OpenTelemetry;
global using OpenTelemetry.Metrics;
global using OpenTelemetry.Resources;

global using Microsoft.AspNetCore.Http;

global using System.Diagnostics;
global using System.Diagnostics.Metrics;
global using System.Text.Json;