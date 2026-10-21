using System;
using System.Collections.Generic;
using System.Text;

namespace TechnoLegendsManagementSystem.Shared.Models
{
    public record ProfileDto(Guid Id, string Name, string Title, string Department, string Role, string Email);
}
