namespace BudgetTracker.Api.Infrastructure.Authentication;

/// <summary>
/// Constants used for authentication and authorization
/// </summary>
public static class AuthenticationConstants
{
    /// <summary>
    /// Key used to store the current user in HttpContext.Items
    /// </summary>
    public const string HttpContextUserItemKey = "User";

    /// <summary>
    /// Name of the Supabase authentication provider
    /// </summary>
    public const string SupabaseProviderName = "supabase";

    /// <summary>
    /// Configuration section name for Supabase
    /// </summary>
    public const string SupabaseSectionName = "Supabase";
}
