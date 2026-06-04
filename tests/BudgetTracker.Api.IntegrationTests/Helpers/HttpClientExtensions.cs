using System.Net.Http.Headers;

namespace BudgetTracker.Api.IntegrationTests.Helpers;

public static class HttpClientExtensions
{
    public static void SetTestUser(this HttpClient client, string userId, string email, bool isProfileComplete)
    {
        var token = $"{userId}|{email}|{isProfileComplete}";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", token);
    }
}
