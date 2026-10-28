using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace TechnoLegendsManagementSystem.Shared.Services;

public interface ISessionPersistence : IGotrueSessionPersistence<Session>
{
    Task InitializeAsync() => Task.CompletedTask;
}