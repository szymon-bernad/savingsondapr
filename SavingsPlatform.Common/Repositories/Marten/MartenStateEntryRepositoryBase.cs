using Marten;
using Marten.Services;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Index;
using SavingsPlatform.Common.Helpers;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Repositories.Enums;
using SavingsPlatform.Common.Services;
using SavingsPlatform.Contracts.Accounts;
using System.Text;

namespace SavingsPlatform.Common.Repositories.Marten
{
    public abstract class MartenStateEntryRepositoryBase<TEntry, TData> : IStateEntryRepository<TEntry>
        where TEntry : IAggregateStateEntry
    {
        private readonly IDocumentSession _documentSession;
        private readonly IStateMapper<AggregateState<TData>, TEntry> _mapper;
        private readonly IEventPublishingService _eventPublishingService;
        private readonly ILogger _logger;

        public MartenStateEntryRepositoryBase(
            ISessionFactory docSession,
            IStateMapper<AggregateState<TData>, TEntry> stateMapper,
            IEventPublishingService publishingService,
            ILogger logger)
        {
            _documentSession = docSession.OpenSession();
            _mapper = stateMapper;
            _eventPublishingService = publishingService;
            _logger = logger;
        }

        public Task AddAccountAsync(TEntry account)
        {
            return TryUpsertAccountAsync(account, null);
        }

        public Task TryUpdateAccountAsync(TEntry account, MessageProcessedEntry? msgEntry)
        {
            return TryUpsertAccountAsync(account, msgEntry);
        }

        public async Task<ICollection<TEntry>> QueryAccountsByKeyAsync(string[] keyNames, object[] keyValues, int? limit = null)
        {
            _logger.LogInformation($"Querying accounts by key: {string.Join(", ", keyNames)}.");

            var result = await QueryAggregateStateByKeysAsync(keyNames, keyValues, limit);

            if (result?.Any() ?? false)
            {
                var mappedData = result.Select(
                    r =>
                    {
                        var m = _mapper.Map(r);
                        return m;
                    }).ToList();

                return mappedData;
            }

            return Enumerable.Empty<TEntry>().ToList();
        }

        public async Task<TEntry?> GetAccountAsync(string key)
        {
            var result = (await this.QueryAccountsByKeyAsync(
                                        new string[] { "id" }, new string[] { key }))
                                    .SingleOrDefault();
            if (result is not null)
            {
                return result;
            }

            return default;
        }

        protected async Task PostToStateStoreAsync(AggregateState<TData> entry, MessageProcessedEntry? msgEntry)
        {
            _documentSession.Store(entry);
            if (msgEntry is not null)
            {
                _documentSession.Store(msgEntry);
            }

            await _documentSession.SaveChangesAsync();
        }

        protected async Task TryUpsertAccountAsync(TEntry entry, MessageProcessedEntry? msgEntry)
        {
            try
            {
                var stateDto = _mapper.ReverseMap(entry);
                await PostToStateStoreAsync(stateDto, msgEntry);

                if (entry.UnpublishedEvents?.Any() ?? false)
                {
                    await _eventPublishingService.PublishEvents(entry.UnpublishedEvents);
                    await TryUpsertAfterEventsPublishedAsync(stateDto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error upserting account with key: {entry.Key}.");
                throw;
            }
        }

        protected Task TryUpsertAfterEventsPublishedAsync(AggregateState<TData> entry)
        {
            entry.HasUnpublishedEvents = false;
            entry.UnpublishedEventsJson = null;

            return PostToStateStoreAsync(entry, null);
        }

        protected async Task<ICollection<AggregateState<TData>>> QueryAggregateStateByKeysAsync(string[] keyNames, object[] keyValue, int? limit = null)
        {
            var queryStringBuilder = new StringBuilder();

            foreach (var q in Enumerable.Range(0, keyNames.Length))
            {
                var kn = keyNames[q];
                var qOp = "==";
                var keyNameSegments = kn.Split(' ');
                if (keyNameSegments.Count() > 1)
                {
                    kn = keyNameSegments.First();
                    qOp = GetQueryOperator(keyNameSegments.Last());
                }

                if (queryStringBuilder.Length > 0)
                {
                    queryStringBuilder.Append(" AND ");
                }

                string? kv;
                if (keyValue[q] is string)
                {
                    kv = $"\"{keyValue[q]}\"";
                }
                else if (keyValue[q] is bool v)
                {
                    kv = v ? "true" : "false";
                }
                else
                {
                    kv = $"{keyValue[q]}";
                }

                queryStringBuilder.Append($"data @@ '$.{kn} {qOp} {kv}'");
            }
            var queryStr = queryStringBuilder.ToString();
            var result =  (await _documentSession.QueryAsync<AggregateState<TData>>(queryStr))
                                .Take(limit?? 100)
                                .ToList();

            return result;
        }

        private string GetQueryOperator(string keyOp)
        {
            if ( Enum.TryParse<QueryOperator>(keyOp, out var queryOperator))
            {
                return queryOperator switch
                {
                    QueryOperator.Equal => "==",
                    QueryOperator.NotEqual => "<>",
                    QueryOperator.GreaterThan => ">",
                    QueryOperator.LessThan => "<",
                    QueryOperator.GreaterThanOrEqual => ">=",
                    QueryOperator.LessThanOrEqual => "<=",
                    _ => "==",
                };
            }

            return "==";
        }

        public async Task<bool> IsMessageProcessed(string msgId)
        {
            try
            {
                var res = await _documentSession.LoadAsync<MessageProcessedEntry>(msgId);
                return res is not null;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error checking if message with Id = {msgId} is processed.");
                throw;
            }
        }
    }
}
