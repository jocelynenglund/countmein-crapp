namespace CountMeIn.Domain;

/// <summary>Base type for every business-rule violation raised by the domain model.</summary>
public abstract class DomainException(string message) : Exception(message);

/// <summary>The input breaks a basic rule (blank title, goal out of range, ...).</summary>
public class InvalidInterestCheckException(string message) : DomainException(message);

public class InterestCheckClosedException() : DomainException("This interest check has closed");

public class AlreadyClosedException() : DomainException("It's already closed");

public class AlreadyCountedInException() : DomainException("You're already counted in");

public class NotCountedInException() : DomainException("You're not counted in");

public class NotOwnerException(string message) : DomainException(message);
