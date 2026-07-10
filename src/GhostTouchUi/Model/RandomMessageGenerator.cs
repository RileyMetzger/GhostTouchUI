namespace GhostTouchUi.Model;

/// <summary>
/// Returns a random string from a predefined set of messages.
/// </summary>
public class RandomMessageGenerator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RandomMessageGenerator"/> class.
    /// </summary>
    /// <param name="messages">The messages that can be returned.</param>
    public RandomMessageGenerator(params string[] messages)
    {
        Random = new();
        Messages = messages;
    }

    /// <summary>
    /// Gets the random number generator used to select messages.
    /// </summary>
    public Random Random { get; }

    /// <summary>
    /// Gets the set of candidate messages.
    /// </summary>
    public string[] Messages { get; }

    /// <summary>
    /// Returns a randomly selected message from <see cref="Messages"/>.
    /// </summary>
    /// <returns>A randomly selected message.</returns>
    public string Get()
    {
        var index = Random.Next(0, Messages.Length);
        return Messages[index];
    }
}
