using System;
using System.Collections.Generic;
using System.Text;

namespace TechnoLegendsManagementSystem.Shared.Services
{
    public class ApiException(string message, int? status = null, Exception? inner = null)
        : Exception(message, inner)
    {
        public int? Status { get; } = status;
    }
}
