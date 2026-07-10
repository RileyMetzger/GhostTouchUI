namespace GhostTouchUi.Model;

/// <summary>
/// Represents a single log item displayed in the main window.
/// </summary>
public class MouseLog
{
    /// <summary>
    /// Gets or sets the time at which the log entry was created.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the message text shown to the user.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the log entry marks activation state.
    /// </summary>
    public bool IsActivationEvent { get; set; }
}
