using System;
using System.Collections.Generic;
using System.Text;

namespace TechnoLegendsManagementSystem.Shared.Models
{

    public record NotificationDto(long Id, string Kind, string Title, string Body, string? Link, DateTime CreatedAt, bool IsRead);
    public record NotificationsResponse(List<NotificationDto> Items, int Unread);
}
