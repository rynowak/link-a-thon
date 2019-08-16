using Microsoft.AspNetCore.Http;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using uController;

namespace Sample
{
    internal class JsonRequestReader : IHttpRequestReader
    {
        public ValueTask<object> ReadAsync(HttpContext httpContext, Type targetType)
        {
            return JsonSerializer.DeserializeAsync(httpContext.Request.Body, targetType);
        }
    }
}