﻿namespace SavingsOnDapr.EventStore.Aggregations;

public record AccountHierarchySummaryDto(
    DateTime? FromDate,
    DateTime? ToDate,
    string StreamId,
    decimal TotalAmountTransferredToSavings,
    decimal TotalAmountWithdrawnFromSavings,
    decimal TotalAmountOfNewDeposits,
    decimal TotalAmountOfWithdrawals,
    int TotalCountOfDepositTransfers);
