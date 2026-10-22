using System;
using System.Collections.Generic;
using System.Text;

namespace TechnoLegendsManagementSystem.Shared.Models
{
    public record PersonDto(Guid Id, string Name, string Title);

    public record ChannelDto(long Id, string Name, string Kind, string? LastText, string? LastSender,
                             DateTime? LastAt, int Unread, int Members);

    public record MessageDto(long Id, long ChannelId, Guid SenderId, string SenderName, string SenderTitle,
                             string Text, DateTime CreatedAt);

    public record SendMessageRequest(string Text);
    public record CreateDmRequest(Guid UserId);
    public record CreateChannelRequest(string Name);
}
