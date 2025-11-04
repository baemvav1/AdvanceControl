using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.Services.Auth
{
    /// <summary>
    /// DelegatingHandler that attaches Bearer token to outgoing requests and tries one automatic refresh on 401.
    /// Register with HttpClient pipeline: AddHttpClient(...).AddHttpMessageHandler&lt;AuthenticatedHttpHandler&gt;()
    /// </summary>
    public class AuthenticatedHttpHandler : DelegatingHandler
    {
        private readonly IAuthService _authService;

        public AuthenticatedHttpHandler(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // attach token if available
            var token = await _authService.GetAccessTokenAsync(cancellationToken);
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // try refresh once
                var refreshed = await _authService.RefreshTokenAsync(cancellationToken);
                if (!refreshed) return response;

                // dispose previous response and retry request with new token
                response.Dispose();

                var newToken = await _authService.GetAccessTokenAsync(cancellationToken);
                if (!string.IsNullOrEmpty(newToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                    // clone request (simple approach: create new request with same content)
                    var newRequest = await CloneHttpRequestMessageAsync(request);
                    return await base.SendAsync(newRequest, cancellationToken);
                }
            }

            return response;
        }

        // Helper to clone HttpRequestMessage (shallow copy + copy content stream)
        private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage req)
        {
            var clone = new HttpRequestMessage(req.Method, req.RequestUri);

            // copy headers
            foreach (var header in req.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            // copy properties
            clone.Version = req.Version;

            if (req.Content != null)
            {
                var ms = new System.IO.MemoryStream();
                await req.Content.CopyToAsync(ms);
                ms.Position = 0;
                clone.Content = new StreamContent(ms);
                if (req.Content.Headers != null)
                {
                    foreach (var h in req.Content.Headers)
                        clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
                }
            }

            return clone;
        }
    }
}