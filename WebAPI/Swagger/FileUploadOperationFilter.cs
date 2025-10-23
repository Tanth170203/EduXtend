using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace WebAPI.Swagger;

/// <summary>
/// Swagger filter to handle file upload operations with IFormFile
/// </summary>
public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Only process if action consumes multipart/form-data
        var consumesAttribute = context.MethodInfo
            .GetCustomAttributes<ConsumesAttribute>()
            .FirstOrDefault();

        if (consumesAttribute?.ContentTypes?.Contains("multipart/form-data") != true)
            return;

        // Get all parameters
        var parameters = context.MethodInfo.GetParameters();
        
        // Check if any parameter is IFormFile or contains IFormFile
        var hasFileParameter = parameters.Any(p => 
            p.ParameterType == typeof(IFormFile) || 
            p.ParameterType == typeof(IFormFile[]) ||
            p.ParameterType.GetProperties().Any(prop => prop.PropertyType == typeof(IFormFile)));

        if (!hasFileParameter)
            return;

        // Build schema properties
        var properties = new Dictionary<string, OpenApiSchema>();

        foreach (var parameter in parameters)
        {
            // Skip route parameters (like {id})
            if (parameter.GetCustomAttribute<FromRouteAttribute>() != null)
                continue;

            if (parameter.ParameterType == typeof(IFormFile))
            {
                // Single file parameter
                properties[parameter.Name ?? "file"] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                };
            }
            else if (parameter.ParameterType.GetProperties().Any(prop => prop.PropertyType == typeof(IFormFile)))
            {
                // DTO with IFormFile properties
                foreach (var property in parameter.ParameterType.GetProperties())
                {
                    if (property.PropertyType == typeof(IFormFile))
                    {
                        properties[property.Name] = new OpenApiSchema
                        {
                            Type = "string",
                            Format = "binary"
                        };
                    }
                    else if (property.PropertyType == typeof(string))
                    {
                        properties[property.Name] = new OpenApiSchema { Type = "string" };
                    }
                    else if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
                    {
                        properties[property.Name] = new OpenApiSchema { Type = "integer" };
                    }
                    else if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
                    {
                        properties[property.Name] = new OpenApiSchema { Type = "boolean" };
                    }
                    // Add more types as needed
                }
            }
            else
            {
                // Other parameters (string, int, etc.)
                if (parameter.ParameterType == typeof(string))
                {
                    properties[parameter.Name ?? "param"] = new OpenApiSchema { Type = "string" };
                }
                else if (parameter.ParameterType == typeof(int) || parameter.ParameterType == typeof(int?))
                {
                    properties[parameter.Name ?? "param"] = new OpenApiSchema { Type = "integer" };
                }
            }
        }

        // Only replace request body if we have properties
        if (properties.Any())
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = properties
                        }
                    }
                }
            };

            // Remove only [FromForm] parameters, keep route parameters
            var parametersToRemove = operation.Parameters
                .Where(p => p.In != ParameterLocation.Path) // Keep path/route parameters
                .ToList();

            foreach (var param in parametersToRemove)
            {
                operation.Parameters.Remove(param);
            }
        }
    }
}

