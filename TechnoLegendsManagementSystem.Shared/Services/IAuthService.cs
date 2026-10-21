using System;
using System.Collections.Generic;
using System.Text;

namespace TechnoLegendsManagementSystem.Shared.Services
{
    public interface IAuthService
    {
       
        Task<string?> GetAccessTokenAsync();
        Task SignOutAsync();
    }
}
