using Microsoft.AspNetCore.Mvc.Formatters;
using System.Threading.Tasks;
using Utf8Json;

internal sealed class Utf8JsonOutputFormatter : IOutputFormatter
{
    private readonly IJsonFormatterResolver _resolver;

    public Utf8JsonOutputFormatter() : this(null) { }
    public Utf8JsonOutputFormatter(IJsonFormatterResolver resolver)
    {
        _resolver = resolver ?? JsonSerializer.DefaultResolver;
    }

    public bool CanWriteResult(OutputFormatterCanWriteContext context) => true;

    public Task WriteAsync(OutputFormatterWriteContext context)
    {
        if (!context.ContentTypeIsServerDefined)
            context.HttpContext.Response.ContentType = "application/json";

        if (context.ObjectType == typeof(object))
        {
            return JsonSerializer.NonGeneric.SerializeAsync(context.HttpContext.Response.Body, context.Object, _resolver);
        }
        else
        {
            return JsonSerializer.NonGeneric.SerializeAsync(context.ObjectType, context.HttpContext.Response.Body, context.Object, _resolver);
        }
    }
}