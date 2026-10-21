using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using TechnoLegendsManagementSystem.Shared.Models;
using System.Text;

namespace TechnoLegendsManagementSystem.Shared.Services
{
    public class ProfileService(IHttpClientFactory factory)
    {
        private ProfileDto? _cached;

        public async Task<ProfileDto> GetAsync()
        {
            if (_cached is not null) return _cached;

            HttpResponseMessage res;
            try { res = await factory.CreateClient("api").GetAsync("api/me/profile"); }
            catch (HttpRequestException) { throw new ApiException("Can't reach the server. Check your connection and try again."); }
            catch (TaskCanceledException) { throw new ApiException("The server took too long to respond."); }

            if (!res.IsSuccessStatusCode)
                throw new ApiException(
                    res.StatusCode == HttpStatusCode.NotFound ? "No profile exists for this account yet."
                    : res.StatusCode == HttpStatusCode.Unauthorized ? "Your session expired. Please sign in again."
                    : "Couldn't load your profile.", (int)res.StatusCode);

            _cached = await res.Content.ReadFromJsonAsync<ProfileDto>()
                      ?? throw new ApiException("The server sent an unexpected response.");
            CurrentUser.Set(_cached.Name, _cached.Department);
            return _cached;
        }

        public void Clear()
        {
            _cached = null;
            CurrentUser.Clear();
        }
    }
}
