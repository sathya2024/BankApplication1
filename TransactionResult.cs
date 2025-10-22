using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.ViewData
{
    public class TransactionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public decimal Amount { get; set; }
        public long TransactionId { get; set; }

    }
}
