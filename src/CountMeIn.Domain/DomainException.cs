namespace CountMeIn.Domain;

/// <summary>A business rule was broken. The message is safe to show to the user.</summary>
public class DomainException(string message) : Exception(message);

/// <summary>The caller is not the owner of the interest check.</summary>
public class NotOwnerException(string message) : DomainException(message);
