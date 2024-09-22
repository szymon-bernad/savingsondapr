using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAAFOConsole;

record Transaction(string ExternalRef, decimal Amount, DateTime TransactionDate, string OpType);
