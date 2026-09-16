namespace Shared.Services.Logging;

/// <summary>
/// The only way a personal identifier may reach a log line. Logs ship to a store with its own
/// access and retention, outside the database's controls, so they get a stand-in: stable enough
/// to correlate repeated sightings, useless for recovering the value. Log the user id instead
/// whenever there is one.
/// </summary>
public interface ILogPseudonymizer
{
    /// <summary>
    /// A mask a person can recognise (<c>j***@e***.com</c>), followed by a keyed hash of the
    /// normalised address (<c>hmac:52447c619733b888</c>) when a key is configured. Log it as
    /// <c>{EmailRef}</c>.
    /// </summary>
    string PseudonymizeEmail(string? email);
}
