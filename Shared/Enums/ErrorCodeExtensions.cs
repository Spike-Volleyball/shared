using System.Text.RegularExpressions;

namespace Shared.Enums
{
    public static class ErrorCodeExtensions
    {
        /// <summary>
        /// Converts ErrorCodeEnum to SCREAMING_SNAKE_CASE string code
        /// Example: EventFull -> "EVENT_FULL"
        /// </summary>
        public static string ToStringCode(this ErrorCodeEnum errorCode)
        {
            var enumName = errorCode.ToString();

            // Insert underscore before uppercase letters (except the first one)
            var snakeCase = Regex.Replace(enumName, "(?<!^)([A-Z])", "_$1");

            // Convert to uppercase
            return snakeCase.ToUpperInvariant();
        }

        /// <summary>
        /// Returns user-friendly title for common errors
        /// Falls back to formatted enum name if no specific title is defined
        /// </summary>
        public static string ToTitle(this ErrorCodeEnum errorCode)
        {
            return errorCode switch
            {
                // General
                ErrorCodeEnum.ValidationError => "Validation Failed",
                ErrorCodeEnum.Unauthorized => "Unauthorized Access",
                ErrorCodeEnum.InternalError => "Internal Server Error",
                ErrorCodeEnum.EntityNotFound => "Resource Not Found",
                ErrorCodeEnum.Forbidden => "Access Forbidden",
                ErrorCodeEnum.RateLimited => "Too Many Requests",

                // Authentication
                ErrorCodeEnum.InvalidCredentials => "Invalid Credentials",
                ErrorCodeEnum.TokenExpired => "Token Expired",
                ErrorCodeEnum.TokenInvalid => "Invalid Token",
                ErrorCodeEnum.EmailNotVerified => "Email Not Verified",
                ErrorCodeEnum.AccountLocked => "Account Locked",

                // User/Profile
                ErrorCodeEnum.UserNotFound => "User Not Found",
                ErrorCodeEnum.EmailAlreadyExists => "Email Already Exists",
                ErrorCodeEnum.InvalidEmail => "Invalid Email Address",
                ErrorCodeEnum.WeakPassword => "Password Too Weak",

                // Events
                ErrorCodeEnum.EventNotFound => "Event Not Found",
                ErrorCodeEnum.EventFull => "Event Full",
                ErrorCodeEnum.EventCancelled => "Event Cancelled",
                ErrorCodeEnum.AlreadyJoined => "Already Joined",
                ErrorCodeEnum.NotParticipant => "Not a Participant",
                ErrorCodeEnum.RegistrationClosed => "Registration Closed",
                ErrorCodeEnum.PositionAlreadyClaimed => "Position Already Claimed",

                // Clubs
                ErrorCodeEnum.ClubNotFound => "Club Not Found",
                ErrorCodeEnum.NotClubMember => "Not a Club Member",
                ErrorCodeEnum.AlreadyClubMember => "Already a Club Member",
                ErrorCodeEnum.InvitationNotFound => "Invitation Not Found",
                ErrorCodeEnum.InvitationExpired => "Invitation Expired",
                ErrorCodeEnum.InsufficientPermissions => "Insufficient Permissions",

                // Payments
                ErrorCodeEnum.PaymentFailed => "Payment Failed",
                ErrorCodeEnum.PaymentRequired => "Payment Required",
                ErrorCodeEnum.RefundFailed => "Refund Failed",

                // Fallback: Convert enum name to title case with spaces
                _ => Regex.Replace(errorCode.ToString(), "(?<!^)([A-Z])", " $1")
            };
        }
    }
}
