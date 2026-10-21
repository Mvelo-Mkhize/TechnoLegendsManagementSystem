using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace TechnoLegendsManagementSystem.Shared.Services
{
    public class AuthHeaderHandler : DelegatingHandler
    {
        private readonly IAuthService _auth;
        public AuthHeaderHandler(IAuthService auth) => _auth = auth;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var token = await _auth.GetAccessTokenAsync();
            if (token is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return await base.SendAsync(request, ct);
        }
    }
}
