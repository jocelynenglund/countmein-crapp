namespace CountMeIn.Domain;

/// <summary>
/// A person who has changed their mind on this check at least once. Changing your mind removes your
/// participant row, so this is what remembers it, even if you count yourself in again later.
/// </summary>
public class ChangedMind
{
    private ChangedMind() { } // EF Core

    internal ChangedMind(string personId) => PersonId = personId;

    public int Id { get; private set; }

    public string PersonId { get; private set; } = null!;
}
