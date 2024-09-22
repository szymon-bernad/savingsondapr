namespace FAAFOConsole;

public record ArgsInputRecord {

    public string BaseUrl { get; init; } = "http://localhost:5136/";

    public string ExternalRef { get; init; } = string.Empty;
    
    public int NumberOfRequests { get; init; }
    
    public decimal AverageAmount { get; init; }
    
    public char OperationType { get; init; } = 'B';

    public override string ToString()
    {
        return $"ArgsInputRecord: {{{nameof(ExternalRef)}: {ExternalRef}, {nameof(NumberOfRequests)}: {NumberOfRequests}, {nameof(AverageAmount)}: {AverageAmount}, {nameof(OperationType)}: {OperationType} }}";
    }
}