namespace FAAFOConsole;

public record ArgsInputRecord {

    public string BaseUrl { get; init; } = "http://localhost:5136";

    public string OpEndpointPath { get; init; } = "/api/accounts/";

    public string StatusEndpointPath { get; init; } = "/api/accounts/";

    public string ExternalRef { get; init; } = string.Empty;

    public int NumberOfRequests { get; init; } = 16;


    public decimal AverageAmount { get; init; } = 255m;
    
    public char OperationType { get; init; } = 'B';

    public override string ToString()
    {
        return $"ArgsInputRecord: {{{nameof(ExternalRef)}: {ExternalRef}, {nameof(NumberOfRequests)}: {NumberOfRequests}, {nameof(AverageAmount)}: {AverageAmount}, {nameof(OperationType)}: {OperationType} }}";
    }
}