using System;
using System.Collections.Generic;
using System.Text;

namespace TechnoLegendsManagementSystem.Shared.Models
{
    public record MeetingDto(Guid Id, string Title, DateTime StartsAt, DateTime EndsAt,
                          string HostName, int CheckedIn, string? MyStatus);
    public record AttendeeDto(string Name, DateTime JoinedAt, string Status);
    public record AttendanceSummaryDto(int Total, int OnTime, int Late, int Missed);
    public record MeetingsOverviewDto(List<MeetingDto> Upcoming, List<MeetingDto> Past, AttendanceSummaryDto Summary);
    public record JoinResultDto(string JoinUrl);
    public record CreateMeetingRequest(string Title, string JoinUrl, DateTime StartsAt, int DurationMinutes);
}
