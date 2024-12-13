// See https://aka.ms/new-console-template for more information
using FAAFOConsole;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;


//var argsInput = SetupRun(args);

//Console.WriteLine($"Running FAAFO-Console with args: {argsInput}");
Console.WriteLine("##################################");
//await RunTest(argsInput);

bool hasChanged = false;
string lastResponse = string.Empty;
using var httpClient = new HttpClient();
while (!hasChanged)
{
    var newResponse = await httpClient.GetStringAsync("https://sod-api.yellowstone-da8942a7.polandcentral.azurecontainerapps.io/healthver");
    Console.WriteLine($"{DateTime.UtcNow.ToString("O")}: {newResponse}");

    if(lastResponse != string.Empty && lastResponse != newResponse)
    {
        hasChanged = true;
    }
    else
    {
        lastResponse = newResponse;
    }
    await Task.Delay(5_000);
}
#region Functions
static async Task RunTest(ArgsInputRecord argsInput)
{
    using var httpClient = new HttpClient();
    var random = new Random();
    var requests = Enumerable.Range(0, argsInput.NumberOfRequests).Select(i => GenerateRandomRequest(random, argsInput.ExternalRef!, argsInput.NumberOfRequests, argsInput.OperationType)).ToList();

    var tasks = requests.Select(r =>
    {
        return httpClient.PostAsync($"{argsInput.BaseUrl}{argsInput.OpEndpointPath}{r.OpType}",
            new StringContent(JsonSerializer.Serialize(r), Encoding.UTF8, "application/json"));
    });

    var currentBalance = await GetCurrentBalance(argsInput, argsInput.ExternalRef, httpClient);
    var timer = new Stopwatch();
    timer.Start();
    var results = await Task.WhenAll(tasks);

    if (results.All(r => r.IsSuccessStatusCode))
    {
        Console.WriteLine("All requests succeeded");
        var urls = results.Select(r => $"{argsInput.BaseUrl}{r.Headers.Location}");
        Console.WriteLine($"URLs: {string.Join(", ", urls)}");

        var expectedBalance = GetExpectedBalance(requests);
        var q = 100;
        while (q > 0)
        {
            Console.WriteLine($"[Elapsed Time = {timer.ElapsedMilliseconds}ms], checking balance... {q} tries left");
            var resBalance = await GetCurrentBalance(argsInput, argsInput.ExternalRef, httpClient);

            var resRes = await Task.WhenAll(urls.Select(u => httpClient.GetAsync(u)));
            Console.WriteLine($"Requests not processed: {resRes.Count(r => r.StatusCode == HttpStatusCode.NotFound)}");

            if (resBalance == currentBalance + expectedBalance)
            {
                Console.WriteLine("Balance is as expected");
                return;
            }
            await Task.Delay(1_000);
            --q;
        }
        timer.Stop();
        Console.WriteLine($"WARNING: Balance doesn't match expected value of {expectedBalance} after 100 tries.");
    }
    else
    {
        Console.WriteLine("WARNING: Some requests failed");
    }
}


static Transaction GenerateRandomRequest(Random random, string externalRef, decimal avg, char opTypeMode)
{
    var optype = opTypeMode switch
    {
        'C' => ":credit",
        'D' => ":debit",
        _ => random.NextDouble() < 0.5 ? ":credit" : ":debit"
    };

    return new Transaction(externalRef, random.Next((int)Math.Round(0.25m * avg), (int)Math.Round(1.25m * avg)), DateTime.UtcNow, optype);
}

static decimal GetExpectedBalance(IEnumerable<Transaction> txns)
{
    return txns.Aggregate(0.0m, (acc, txn) => txn.OpType == ":credit" ? acc + txn.Amount : acc - txn.Amount);
}

static async Task<decimal?> GetCurrentBalance(ArgsInputRecord argsInput, string extRef, HttpClient httpClient)
{
    var accRes = await httpClient.GetAsync($"{argsInput.BaseUrl}{argsInput.StatusEndpointPath}{extRef}");
    var balance = (await JsonSerializer.DeserializeAsync<JsonObject[]>(await accRes.Content.ReadAsStreamAsync()))?.FirstOrDefault();

    if (balance?.TryGetPropertyValue("totalBalance", out var totalBalance) ?? false)
    {
        var actbalance = totalBalance.GetValue<decimal>();
        return actbalance;
    }
    return null;
}

static ArgsInputRecord SetupRun(string[] args)
{
    var argName = string.Empty;
    var argsInput = new ArgsInputRecord();
    foreach (var arg in args)
    {
        if (arg.StartsWith("--"))
        {
            argName = arg.Substring(2);

        }
        else if (!string.IsNullOrEmpty(argName))
        {
            switch (argName)
            {
                case nameof(ArgsInputRecord.BaseUrl):
                    argsInput = argsInput with { BaseUrl = arg };
                    break;
                case nameof(ArgsInputRecord.ExternalRef):
                    argsInput = argsInput with { ExternalRef = arg };
                    break;
                case nameof(ArgsInputRecord.NumberOfRequests):
                    argsInput = argsInput with { NumberOfRequests = int.Parse(arg) };
                    break;
                case nameof(ArgsInputRecord.AverageAmount):
                    argsInput = argsInput with { AverageAmount = decimal.Parse(arg) };
                    break;
                case nameof(ArgsInputRecord.OperationType):
                    argsInput = argsInput with { OperationType = arg.ToUpper().First() };
                    break;
            }
        }
    }

    if (string.IsNullOrEmpty(argsInput.ExternalRef))
    {
        Console.WriteLine("Enter ExternalRef: ");
        var extref = Console.ReadLine();

        if (!string.IsNullOrEmpty(extref))
        {
            argsInput = argsInput with { ExternalRef = extref };
        }
        else
        {
            Console.WriteLine("ExternalRef is required");
            throw new InvalidOperationException("ExternalRef is required");
        }
    }
    return argsInput;
}
#endregion

