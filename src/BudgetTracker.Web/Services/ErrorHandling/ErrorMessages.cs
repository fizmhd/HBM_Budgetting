namespace BudgetTracker.Web.Services.ErrorHandling;

public static class ErrorMessages
{
    public static class Auth
    {
        public const string InvalidCredentials = "Invalid email or password.";
        public const string AccountLocked = "Account is locked or disabled. Please contact support.";
        public const string SessionExpired = "Session expired. Please login again.";
        public const string Unauthorized = "You are not authorized to perform this action.";
    }

    public static class Validation
    {
        public const string Default = "Please check your input and try again.";
        public const string EmailExists = "Email is already registered.";
        public const string InvalidData = "The submitted data is invalid.";
    }

    public static class Network
    {
        public const string ConnectionError = "Network error. Please check your internet connection.";
        public const string Timeout = "The request timed out. Please try again.";
        public const string ServerError = "A server error occurred. Please try again later.";
    }

    public static class Common
    {
        public const string GeneralError = "Something went wrong. Please try again.";
        public const string TooManyRequests = "Too many attempts. Please try again later.";
        public const string NotFound = "The requested resource was not found.";
    }
}
