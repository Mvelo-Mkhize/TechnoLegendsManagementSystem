namespace TechnoLegendsMobile.Api.Data
{
    public static class NotificationExtensions
    {
        // adds rows to the context; the caller saves
        public static void AddNotifications(this AppDb db, IEnumerable<Guid> userIds,
                                            string kind, string title, string body, string? link)
        {
            foreach (var id in userIds.Distinct())
                db.Notifications.Add(new NotificationEntity
                {
                    UserId = id,
                    Kind = kind,
                    Title = title,
                    Body = body,
                    Link = link
                });
        }
    }
}
