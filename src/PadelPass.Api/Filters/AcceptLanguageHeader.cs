using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PadelPass.Api.Filters;

public class AcceptLanguageHeader : IOperationFilter
{

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters == null)
            operation.Parameters = new List<OpenApiParameter>();

        var schema = new OpenApiSchema
        {
            Type = "string",
        };
        schema.Enum.Add(new OpenApiString("ar-SA"));
        schema.Enum.Add(new OpenApiString("en-US"));

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Accept-Language",
            In = ParameterLocation.Header,
            Description = "Add language you want here",
            Required = true,
            Schema = schema
        });
    }
}