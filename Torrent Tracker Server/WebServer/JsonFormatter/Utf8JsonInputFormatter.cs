using Microsoft.AspNetCore.Mvc.Formatters;
using System.Threading.Tasks;
using Utf8Json;

internal sealed class Utf8JsonInputFormatter : IInputFormatter
{
    private readonly IJsonFormatterResolver _resolver;

    public Utf8JsonInputFormatter() : this(null) { }
    public Utf8JsonInputFormatter(IJsonFormatterResolver resolver)
    {
        _resolver = resolver ?? JsonSerializer.DefaultResolver;
    }

    public bool CanRead(InputFormatterContext context) => context.HttpContext.Request.ContentType.StartsWith("application/json");

    public Task<InputFormatterResult> ReadAsync(InputFormatterContext context)
    {
        var request = context.HttpContext.Request;

        if (request.Body.CanSeek && request.Body.Length == 0)
            return InputFormatterResult.NoValueAsync();

        var result = JsonSerializer.NonGeneric.Deserialize(context.ModelType, request.Body, _resolver);
        return InputFormatterResult.SuccessAsync(result);
    }
}