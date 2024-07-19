using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace Server.Services
{
    public class SwaggerTryItOutDefaulValue : ISchemaFilter
    {
        /// <summary>
        /// Apply is called for each parameter
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="context"></param>
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.ParameterInfo != null)
            {
                var att = context.ParameterInfo.GetCustomAttribute<SwaggerTryItOutDefaulValueAttribute>();
                if ((att != null))
                {
                    schema.Example = new Microsoft.OpenApi.Any.OpenApiString(att.Value.ToString());
                }
            }
        }
    }

    public class SwaggerTryItOutDefaulValueAttribute : Attribute
    {
        public string Value { get; }

        public SwaggerTryItOutDefaulValueAttribute(string value)
        {
            Value = value;
        }
    }
}

