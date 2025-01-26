using SavingsPlatform.Contracts.Accounts.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Contracts.Accounts.Models;

public record CurrentAccountResponse(string Key, string ExternalRef, DateTime? OpenedOn, decimal TotalBalance, Currency Currency, AccountType Type);
