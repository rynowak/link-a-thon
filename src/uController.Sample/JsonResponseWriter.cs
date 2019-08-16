using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Threading.Tasks;
using uController;

namespace Sample
{
    internal class JsonResponseWriter : IHttpResponseWriter
    {
        public Task WriteAsync(HttpContext httpContext, object value)
        {
            return JsonSerializer.SerializeAsync(httpContext.Response.Body, value, value.GetType());
        }
    }
}