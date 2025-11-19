namespace Challenge.Models;

/// <summary>
/// View model for error pages, containing request identification information.
/// </summary>
public class ErrorViewModel
{
    /// <summary>
    /// Gets or sets the request ID for tracking and debugging purposes.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Gets a value indicating whether the request ID should be displayed.
    /// </summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
