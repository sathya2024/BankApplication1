using Banking;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;

namespace BankingService.Services
{
    public class CustomerService
    {
        private readonly BankDBEntities _context;

        public CustomerService()
        {
            _context = new BankDBEntities();
        }

        //Dashboard Summary
        public CustomerDashboardViewModel GetDashboard(string customerId)
        {
            var customer = _context.Customers.FirstOrDefault(c => c.CustomerId == customerId);
            if (customer == null) return null;

            return new CustomerDashboardViewModel
            {
                CustomerId = customer.CustomerId,

                Name = customer.CustomerName,
                Email = customer.CustomerEmail,
                HasSavings = _context.SavingsAccounts.Any(sa => sa.CustomerId == customerId),
                HasFD = _context.FixedDeposits.Any(fd => fd.CustomerId == customerId),
                HasLoan = _context.LoanAccounts.Any(ln => ln.CustomerId == customerId)
            };
        }

        //Account Details
        public List<Account> GetAccountDetails(string customerId)
        {
            return _context.Accounts.Where(a => a.CustomerId == customerId).ToList();
        }

        //Savings
        public SavingsAccount GetSavingsDetails(string customerId)
        {
            return _context.SavingsAccounts.FirstOrDefault(sa => sa.CustomerId == customerId);
        }

        public string GetSavingsAccountId(string customerId)
        {
            return _context.SavingsAccounts
                .Where(sa => sa.CustomerId == customerId)
                .Select(sa => sa.SBAccountId)
                .FirstOrDefault();
        }

        public bool Deposit(string accountId, decimal amount)
        {
            if (amount < 100) return false;

            var account = _context.SavingsAccounts.FirstOrDefault(sa => sa.SBAccountId == accountId);
            if (account == null) return false;

            account.Balance += amount;
            _context.SavingsTransactions.Add(new SavingsTransaction
            {
                SBAccountId = accountId,
                Amount = amount,
                TransactionType = "D",
                TransactionDate = DateTime.Now
            });
            _context.SaveChanges();
            return true;
        }

        public bool Withdraw(string accountId, decimal amount)
        {
            var account = _context.SavingsAccounts.FirstOrDefault(sa => sa.SBAccountId == accountId);
            if (account == null || amount < 100 || account.Balance - amount < 1000) return false;

            account.Balance -= amount;
            _context.SavingsTransactions.Add(new SavingsTransaction
            {
                SBAccountId = accountId,
                Amount = amount,
                TransactionType = "W",
                TransactionDate = DateTime.Now
            });
            _context.SaveChanges();
            return true;
        }


        public List<SavingsTransaction> GetTopTransactions(string accountId, int count)
        {
            return _context.SavingsTransactions
                .Where(t => t.SBAccountId == accountId)
                .OrderByDescending(t => t.TransactionDate)
                .Take(count)
                .ToList();
        }

        public List<SavingsTransaction> GetSavingsTransactions(string accountId)
        {
            return _context.SavingsTransactions
                .Where(t => t.SBAccountId == accountId)
                .OrderByDescending(t => t.TransactionDate)
                .ToList();
        }

        //FD CODE

       public List<FixedDeposit> GetFDDetails(string customerId)
{
    return _context.FixedDeposits
        .Where(fd => fd.CustomerId == customerId && fd.Status== "ACTIVE")
        .OrderByDescending(fd => fd.OpenDate)
        .ToList();
}

        public List<FDTransaction> GetFDTransactions(string customerId)
        {
            var fdIds = _context.FixedDeposits
                .Where(fd => fd.CustomerId == customerId)
                .Select(fd => fd.FDAccountId)
                .ToList();

            return _context.FDTransactions
       .Where(t => fdIds.Contains(t.FDAccountId))
       .OrderByDescending(t => t.TransactionDate)
       .ToList();
        }


        public string GetCustomerIdByFD(string fdAccountId)
        {
            var fdRecord = _context.FixedDeposits.FirstOrDefault(fd => fd.FDAccountId == fdAccountId);
            return fdRecord?.CustomerId;
        }

        public TransactionResult PrematureWithdrawFD(string fdAccountId)
        {
            var fdRecord = _context.FixedDeposits.FirstOrDefault(fd => fd.FDAccountId == fdAccountId);
            if (fdRecord == null)
                return new TransactionResult { Success = false, Message = "FD not found." };

            fdRecord.Status = "Closed";
            fdRecord.CloseDate = DateTime.Now;
            _context.Entry(fdRecord).State = EntityState.Modified;


            var accountRecord = _context.Accounts.FirstOrDefault(a => a.AccountId == fdAccountId);
            if (accountRecord != null)
            {
                accountRecord.AccountStatus = "Closed";
                accountRecord.CloseDate = DateTime.Now;
                _context.Entry(accountRecord).State = EntityState.Modified;
            }


            var transaction = new FDTransaction
            {
                FDAccountId = fdRecord.FDAccountId,
                TransactionDate = DateTime.Now,
                TransactionType = "Premature Closure",
                Amount = fdRecord.Amount,
                Remark = "FD closed before maturity"
            };

            _context.FDTransactions.Add(transaction);

            try
            {
                _context.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                foreach (var validationErrors in ex.EntityValidationErrors)
                {
                    foreach (var error in validationErrors.ValidationErrors)
                    {
                        Console.WriteLine($"Property: {error.PropertyName} Error: {error.ErrorMessage}");
                    }
                }
                throw;
            }

            return new TransactionResult
            {
                Success = true,
                Message = "FD closed prematurely.",
                Amount = (decimal)transaction.Amount,
                TransactionId = transaction.TransactionId
            };
        }


        public TransactionResult WithdrawFD(string fdAccountId)
        {


            var fdRecord = _context.FixedDeposits.FirstOrDefault(fd => fd.FDAccountId == fdAccountId);
            if (fdRecord == null)
                return new TransactionResult { Success = false, Message = "FD not found." };

            if (DateTime.Today != fdRecord.MaturityDate?.Date)
                return new TransactionResult { Success = false, Message = "FD not yet matured." };

            fdRecord.Status = "Closed";
            fdRecord.CloseDate = DateTime.Now;
            _context.Entry(fdRecord).State = EntityState.Modified;

            var accountRecord = _context.Accounts.FirstOrDefault(a => a.AccountId == fdAccountId);
            if (accountRecord != null)
            {
                accountRecord.AccountStatus = "Closed";
                accountRecord.CloseDate = DateTime.Now;
                _context.Entry(accountRecord).State = EntityState.Modified;
            }


            var transaction = new FDTransaction
            {
                FDAccountId = fdRecord.FDAccountId,
                TransactionDate = DateTime.Now,
                TransactionType = "Maturity Withdrawal",
                Amount = fdRecord.Amount,
                Remark = "FD withdrawn on maturity"
            };

            _context.FDTransactions.Add(transaction);

            try
            {
                _context.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                foreach (var validationErrors in ex.EntityValidationErrors)
                {
                    foreach (var error in validationErrors.ValidationErrors)
                    {
                        Console.WriteLine($"Property: {error.PropertyName} Error: {error.ErrorMessage}");
                    }
                }
                throw;
            }

            return new TransactionResult
            {
                Success = true,
                Message = "FD withdrawn on maturity.",
                Amount = (decimal)transaction.Amount,
                TransactionId = transaction.TransactionId
            };
        }

        //  Get all loans 
        public List<LoanAccount> GetAllLoans(string customerId)
            {
                return _context.LoanAccounts
                    .Where(ln => ln.CustomerId == customerId)
                    .OrderByDescending(ln => ln.StartDate)
                    .ToList();
            }

            //  Get a specific loan by ID
            public LoanAccount GetLoanById(string loanId)
            {
                return _context.LoanAccounts.FirstOrDefault(ln => ln.LNAccountId == loanId);
            }

            //  Get transactions for a specific loan
            public List<LoanTransaction> GetLoanTransactionsByLoanId(string loanId)
            {
                return _context.LoanTransactions
                    .Where(txn => txn.LNAccountId == loanId)
                    .OrderByDescending(txn => txn.TransactionDate)
                    .ToList();
            }


        
        public bool PayEMI(string customerId, string loanId, decimal amount)
        {
            var loan = GetLoanById(loanId);
            if (loan == null || loan.CustomerId != customerId || !loan.LoanAmount.HasValue || !loan.TimePeriod.HasValue)
                return false;

            loan.DueAmount = Math.Max(loan.DueAmount.Value - amount, 0);

            _context.LoanTransactions.Add(new LoanTransaction
            {
                LNAccountId = loan.LNAccountId,
                Amount = amount,
                TransactionType = "EMI",
                TransactionDate = DateTime.Now,
                Penalty = 0
            });

            _context.SaveChanges();
            return true;
        }


        //  Part payment for a specific loan
        public bool PartPay(string customerId, string loanId, decimal amount)
            {
                var loan = GetLoanById(loanId);
                if (loan == null || loan.CustomerId != customerId || amount <= 0) return false;

                loan.DueAmount = Math.Max(loan.DueAmount.Value - amount, 0);

                _context.LoanTransactions.Add(new LoanTransaction
                {
                    LNAccountId = loan.LNAccountId,
                    Amount = amount,
                    TransactionType = "PartPay",
                    TransactionDate = DateTime.Now,
                    Penalty = 0
                });

                _context.SaveChanges();
                return true;
            }

        //foreclose code
        public bool ForecloseLoan(string customerId, string loanId)
        {
            var loan = GetLoanById(loanId);
            if (loan == null || loan.CustomerId != customerId) return false;

            _context.LoanTransactions.Add(new LoanTransaction
            {
                LNAccountId = loan.LNAccountId,
                Amount = loan.DueAmount ?? 0m,
                TransactionType = "Foreclose",
                TransactionDate = DateTime.Now,
                Penalty = 0
            });

            loan.LoanStatus = "Foreclosed";
            loan.DueAmount = 0m;

            try
            {
                _context.SaveChanges();
                return true;
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                foreach (var eve in ex.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Console.WriteLine($"Property: {ve.PropertyName}, Error: {ve.ErrorMessage}");
                    }
                }
                return false;
            }
        }

    }
    

    // ViewModel for Dashboard
    public class CustomerDashboardViewModel
    {
        public string CustomerId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public bool HasSavings { get; set; }
        public bool HasFD { get; set; }
        public bool HasLoan { get; set; }
    }
    public class TransactionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public decimal Amount { get; set; }
        public long TransactionId { get; set; }
        public decimal NewBalance { get; set; }
    }

}


