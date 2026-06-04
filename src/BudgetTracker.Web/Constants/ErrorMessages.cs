namespace BudgetTracker.Web.Constants;

public static class ErrorMessages
{
    public static class Common
    {
        public const string GeneralError = "An unexpected error occurred. Please try again.";
        public const string NotFound = "The requested resource was not found.";
        public const string TooManyRequests = "You are making too many requests. Please wait a moment.";
    }

    public static class Network
    {
        public const string ConnectionError = "Unable to connect to the server. Please check your internet connection.";
        public const string Timeout = "The request timed out. Please try again.";
        public const string ServerError = "A server error occurred. Please contact support if the problem persists.";
    }

    public static class Validation
    {
        public const string Default = "Please check the highlighted errors and try again.";
        public const string InvalidData = "The data provided is invalid.";
        public const string EmailExists = "This email address is already registered.";
    }

    public static class Auth
    {
        public const string Unauthorized = "You do not have permission to access this resource.";
        public const string SessionExpired = "Your session has expired. Please log in again.";
    }
}
