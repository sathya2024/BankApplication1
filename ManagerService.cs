using Banking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web.Mvc;
using System.Web.WebPages.Html;




namespace BankingService.Services
{
    public class ManagerService
    {
        private readonly BankDBEntities _context;
        public ManagerService()
        {
            _context = new BankDBEntities();
        }
        public List<PendingApprovalViewModel> GetPendingApprovals()
        {
            var pendingCustomers = _context.Customers
            .Where(c => c.Approval == "Pending")
            .Select(c => new PendingApprovalViewModel
            {
                Type = "Customer",
                Name = c.CustomerName,
                DepartmentID = c.CustomerEmail,
                PAN = c.PAN
            });
            var pendingEmployees = _context.Employees
            .Where(e => e.Approval == "Pending")
            .Select(e => new PendingApprovalViewModel
            {
                Type = "Employee",
                Name = e.EmployeeName,
                DepartmentID = e.DepartmentId,
                PAN = e.PAN
            });
            return pendingCustomers.Concat(pendingEmployees).ToList();
        }
        public void ApproveUser(string pan)
        {
            var customer = _context.Customers.FirstOrDefault(c => c.PAN == pan);
            if (customer != null)
            {
                customer.Approval = "Approved";
                _context.SaveChanges();
                return;
            }
            var employee = _context.Employees.FirstOrDefault(e => e.PAN == pan);
            if (employee != null)
            {
                employee.Approval = "Approved";
                _context.SaveChanges();
            }
        }
        public bool AddEmployeeWithLogin(Employee model, string password)
        {
            try
            {
                // Generate a random 5-digit EmployeeId
                var random = new Random();
                string employeeId;

                // Ensure uniqueness by checking against existing IDs
                do
                {
                    employeeId = random.Next(10000, 99999).ToString(); // 5-digit number
                }
                while (_context.Employees.Any(e => e.EmployeeId == employeeId));

                model.EmployeeId = employeeId;
                model.Approval = "Pending";

                _context.Employees.Add(model);

                // Add to LoginData
                var login = new LoginData
                {
                    UserName = model.EmployeeName,
                    UserPassword = password,
                    UserRole = "Employee",
                    RefID = model.EmployeeId
                };

                _context.LoginDatas.Add(login);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }
        public List<Customer> GetAllCustomers()
        {
            return _context.Customers.ToList();
        }

        public List<System.Web.Mvc.SelectListItem> GetApprovedEmployeesForDropdown()
        {
            // Step 1: Materialize the query first
            var employees = _context.Employees
                .Where(e => e.Approval == "Approved")
                .Select(e => new { e.EmployeeId, e.EmployeeName })
                .ToList(); // ✅ Forces EF to execute the query

            // Step 2: Format the display text in memory
            return employees.Select(e => new System.Web.Mvc.SelectListItem
            {
                Value = e.EmployeeId,
                Text = $"{e.EmployeeName} ({e.EmployeeId})" 
            }).ToList();
        }

        public bool DeleteEmployeeWithLogin(string employeeId)
        {
            var employee = _context.Employees.FirstOrDefault(e => e.EmployeeId == employeeId);
            if (employee == null) return false;

            var login = _context.LoginDatas.FirstOrDefault(l => l.RefID == employeeId && l.UserRole == "Employee");
            if (login != null)
            {
                _context.LoginDatas.Remove(login);
            }

            _context.Employees.Remove(employee);
            _context.SaveChanges();
            return true;
        }



        public Customer GetCustomerById(string customerId)
        {
            return _context.Customers.FirstOrDefault(c => c.CustomerId == customerId);
        }



        public string GenerateCustomerId()
        {
            int count = _context.Customers.Count() + 1;
            return "CUS" + count.ToString("D5");
        }

        public bool AddCustomerWithLogin(Customer model, string password)
        {
            try
            {
                int count = _context.Customers.Count() + 1;
                string customerId = "CUS" + count.ToString("D5");

                while (_context.Customers.Any(c => c.CustomerId == customerId))
                {
                    count++;
                    customerId = "CUS" + count.ToString("D5");
                }
                model.CustomerId = customerId;
                model.Approval = "Pending"; // ✅ Set approval status

                model.CustomerId = customerId;

                _context.Customers.Add(model);

                var login = new LoginData
                {
                    UserName = model.CustomerName,
                    UserPassword = password,
                    UserRole = "Customer",
                    RefID = customerId
                };

                _context.LoginDatas.Add(login);
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("AddCustomerWithLogin failed: " + ex.Message);
                return false;
            }
        }

        public void PayEMI(string loanAccountId, decimal amount)
        {
            // Get the loan account
            var loan = _context.LoanAccounts.FirstOrDefault(l => l.LNAccountId == loanAccountId);
            if (loan == null)
                throw new Exception("Loan account not found.");

            if (amount <= 0)
                throw new Exception("Payment amount must be greater than zero.");

            if (amount > loan.DueAmount)
                throw new Exception($"Payment exceeds remaining due amount: ₹{loan.DueAmount:F2}");

            // Deduct the paid amount
            loan.DueAmount -= amount;
            if (loan.DueAmount < 0) loan.DueAmount = 0;

            // Record a transaction
            var transaction = new LoanTransaction
            {
                LNAccountId = loan.LNAccountId,
                Amount = amount,
                TransactionType = "EMI",
                TransactionDate = DateTime.Now,
                Penalty = 0
            };
            _context.LoanTransactions.Add(transaction);

            // If loan fully paid, set account status to Inactive
            if (loan.DueAmount == 0)
            {
                loan.LoanStatus = "Inactive";

                var account = _context.Accounts.FirstOrDefault(a => a.AccountId == loan.LNAccountId);
                if (account != null)
                    account.AccountStatus = "Inactive";
            }

            _context.SaveChanges();
        }

        public bool DeleteCustomer(string customerId)
        {
            try
            {
                // Check for active accounts in Accounts table
                bool hasActiveAccounts = _context.Accounts.Any(a =>
                    a.CustomerId == customerId &&
                    a.AccountStatus == "Active");

                // Check for linked records in LoanAccount, FD, SB
                bool hasLoan = _context.LoanAccounts.Any(l => l.CustomerId == customerId);
                bool hasFD = _context.FixedDeposits.Any(fd => fd.CustomerId == customerId);
                bool hasSB = _context.SavingsAccounts.Any(sb => sb.CustomerId == customerId);

                if (hasActiveAccounts || hasLoan || hasFD || hasSB)
                {
                    return false; // Cannot delete
                }

                // Delete LoginData
                var login = _context.LoginDatas.FirstOrDefault(l => l.RefID == customerId);
                if (login != null)
                {
                    _context.LoginDatas.Remove(login);
                }

                // Delete Customer
                var customer = _context.Customers.FirstOrDefault(c => c.CustomerId == customerId);
                if (customer != null)
                {
                    _context.Customers.Remove(customer);
                }

                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("DeleteCustomer failed: " + ex.Message);
                return false;
            }
        }



        public void PayEMI(LoanAccount loan, decimal amount)
        {
            if (loan == null)
                throw new Exception("Loan account is null.");

            PayEMI(loan.LNAccountId, amount); // reuse existing method
        }

        public bool CreateSBAccountWithDetails(Customer customer, decimal initialDeposit, int createdBy, out string generatedAccountId)
        {
            generatedAccountId = string.Empty;

            try
            {
                if (initialDeposit < 1000)
                    throw new Exception("Minimum deposit should be ₹1000.");

                bool alreadyExists = _context.SavingsAccounts.Any(a => a.CustomerId == customer.CustomerId);
                if (alreadyExists)
                    throw new Exception("Customer already has a Savings Account.");

                var existingCustomer = _context.Customers.SingleOrDefault(c => c.CustomerId == customer.CustomerId);
                if (existingCustomer != null)
                {
                    existingCustomer.CustomerName = customer.CustomerName;
                    existingCustomer.PhoneNumber = customer.PhoneNumber;
                    existingCustomer.CustomerDOB = customer.CustomerDOB;
                    existingCustomer.CustomerAddress = customer.CustomerAddress;
                    existingCustomer.CustomerEmail = customer.CustomerEmail;
                    existingCustomer.PAN = customer.PAN;
                }

                string accountId;
                var random = new Random();
                do
                {
                    accountId = "SB" + random.Next(10000, 99999);
                } while (_context.Accounts.Any(a => a.AccountId == accountId));

                generatedAccountId = accountId;

                var account = new Account
                {
                    AccountId = accountId,
                    CustomerId = customer.CustomerId,
                    CreatedBy = createdBy,
                    OpenDate = DateTime.Now,
                    AccountStatus = "Active"
                };
                _context.Accounts.Add(account);

                var sbAccount = new SavingsAccount
                {
                    SBAccountId = accountId,
                    CustomerId = customer.CustomerId,
                    Balance = initialDeposit
                };
                _context.SavingsAccounts.Add(sbAccount);

                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating SB account: " + ex.Message);
                return false;
            }
        }


        public bool CloseSBAccount(string accountId)
        {
            try
            {
                var account = _context.Accounts.FirstOrDefault(a => a.AccountId == accountId);
                if (account != null && account.AccountStatus == "Active")
                {
                    account.AccountStatus = "Closed";
                    account.CloseDate = DateTime.Now;

                    _context.SaveChanges();
                    return true;
                }
                return false; // Already closed or not found
            }
            catch (Exception ex)
            {
                // Optional: log error
                Console.WriteLine("Error closing account: " + ex.Message);
                return false;
            }
        }

        public SavingsAccount GetSBAccountDetails(string accountId)
        {
            return _context.SavingsAccounts.FirstOrDefault(a => a.SBAccountId == accountId);
        }


        // Existing: Get all transactions
        public List<SavingsTransaction> GetAllSBTransactions()
        {
            return _context.SavingsTransactions
                .OrderByDescending(t => t.TransactionDate)
                .ToList();
        }

        // Deposit method
        public string Deposit(string sbAccountId, decimal amount)
        {
            if (string.IsNullOrEmpty(sbAccountId))
                return "Enter a valid SB Account ID.";

            if (amount <= 0)
                return "Amount must be greater than zero.";

            var sbAccount = _context.SavingsAccounts.SingleOrDefault(s => s.SBAccountId == sbAccountId);
            if (sbAccount == null)
                return "SB Account not found.";

            sbAccount.Balance += amount;

            var txn = new SavingsTransaction
            {
                SBAccountId = sbAccountId,
                Amount = amount,
                TransactionType = "D",
                TransactionDate = DateTime.Now,
                Balance = (decimal)sbAccount.Balance  // store current balance after deposit
            };
            _context.SavingsTransactions.Add(txn);

            _context.SavingsTransactions.Add(txn);
            _context.SaveChanges();

            return $"₹{amount} deposited successfully!";
        }

        // Withdraw method
        public string Withdraw(string sbAccountId, decimal amount)
        {
            if (string.IsNullOrEmpty(sbAccountId))
                return "Enter a valid SB Account ID.";

            if (amount <= 0)
                return "Amount must be greater than zero.";

            var sbAccount = _context.SavingsAccounts.SingleOrDefault(s => s.SBAccountId == sbAccountId);
            if (sbAccount == null)
                return "SB Account not found.";

            if (amount > sbAccount.Balance)
                return "Insufficient balance.";

            sbAccount.Balance -= amount;

            var txn = new SavingsTransaction
            {
                SBAccountId = sbAccountId,
                Amount = amount,
                TransactionType = "W",
                TransactionDate = DateTime.Now,
                Balance = (decimal)sbAccount.Balance  // store balance after withdrawal
            };
            _context.SavingsTransactions.Add(txn);
            _context.SaveChanges();

            return $"₹{amount} withdrawn successfully!";
        }

        // Optional: Get transactions for a single account
        public List<SavingsTransaction> GetTransactionsByAccount(string sbAccountId)
        {
            return _context.SavingsTransactions
                .Where(t => t.SBAccountId == sbAccountId)
                .OrderByDescending(t => t.TransactionDate)
                .ToList();
        }


        // Generate Loan Account ID
        public string GenerateLoanAccountId()
        {
            return "LN" + new Random().Next(10000, 99999);
        }



        // Get Loan Account by ID
        public LoanAccount GetLoanAccountById(string lnAccountId)
        {
            return _context.LoanAccounts.FirstOrDefault(l => l.LNAccountId == lnAccountId);
        }

        // Calculate Age
        public int CalculateAge(DateTime dob)
        {
            var today = DateTime.Today;
            int age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age)) age--;
            return age;
        }

        // Process Loan & EMI calculation
        public LoanAccount ProcessLoan(LoanAccount loan, Customer customer, decimal monthlyTakeHome, out string errorMessage, out decimal emi)
        {
            errorMessage = "";
            emi = 0;

            int age = CalculateAge((DateTime)customer.CustomerDOB);
            bool isSenior = age >= 60;

            if (loan.LoanAmount < 10000)
            {
                errorMessage = "Minimum loan amount is ₹10,000.";
                return null;
            }

            if (isSenior && loan.LoanAmount > 100000)
            {
                errorMessage = "Senior citizens cannot take loans above ₹1,00,000.";
                return null;
            }

            decimal interest = isSenior ? 9.5m :
                loan.LoanAmount <= 500000 ? 10m :
                loan.LoanAmount <= 1000000 ? 9.5m : 9m;

            decimal totalPayable = (decimal)(loan.LoanAmount * (1 + (interest / 100) * loan.TimePeriod));
            emi = (decimal)(totalPayable / (loan.TimePeriod * 12));

            if (emi > monthlyTakeHome * 0.6m)
            {
                errorMessage = "EMI exceeds 60% of monthly take-home.";
                return null;
            }

            loan.Interest = interest;
            loan.TotalPayable = totalPayable;
            loan.DueAmount = totalPayable;
            loan.Emi = emi;
            loan.StartDate = DateTime.Now;

            return loan;
        }

        public void CloseLoanAccount(LoanAccount loan)
        {
            if (loan == null) throw new Exception("Loan account is null.");

            // Ensure dues cleared for normal closure
            if (loan.DueAmount > 0)
                throw new Exception($"Cannot close loan. Outstanding due: ₹{loan.DueAmount:F2}");

            // Set AccountStatus to Inactive
            var account = _context.Accounts.FirstOrDefault(a => a.AccountId == loan.LNAccountId);
            if (account != null)
            {
                account.AccountStatus = "Inactive";
            }

            loan.LoanStatus = "Inactive";
            _context.SaveChanges();
        }

        public void ForecloseLoanAccount(LoanAccount loan, decimal paidAmount)
        {
            if (loan == null) throw new Exception("Loan account is null.");

            // Deduct paid amount from DueAmount
            loan.DueAmount -= paidAmount;
            if (loan.DueAmount < 0) loan.DueAmount = 0;

            // Set status to Inactive if fully foreclosed
            if (loan.DueAmount == 0)
            {
                var account = _context.Accounts.FirstOrDefault(a => a.AccountId == loan.LNAccountId);
                if (account != null)
                    account.AccountStatus = "Inactive";

                loan.LoanStatus = "Inactive";
            }

            _context.SaveChanges();
        }

        // Save Loan and Account
        public void SaveLoanAndAccount(LoanAccount loan, int createdBy, DateTime openDate, DateTime? closeDate, string accountStatus = null)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Generate Account ID if not set
                    if (string.IsNullOrEmpty(loan.LNAccountId))
                    {
                        loan.LNAccountId = GenerateLoanAccountId();
                    }

                    // Set default status
                    loan.LoanStatus = accountStatus ?? "Active";
                    loan.StartDate = openDate;

                    var account = new Account
                    {
                        AccountId = loan.LNAccountId,
                        CustomerId = loan.CustomerId,
                        CreatedBy = createdBy,
                        OpenDate = openDate,
                        CloseDate = closeDate,
                        AccountStatus = loan.LoanStatus  // <-- Active by default
                    };

                    _context.Accounts.Add(account);
                    _context.LoanAccounts.Add(loan);
                    _context.SaveChanges();

                    transaction.Commit();
                }
                catch (DbEntityValidationException dbEx)
                {
                    var allErrors = new List<string>();
                    foreach (var validationErrors in dbEx.EntityValidationErrors)
                        foreach (var validationError in validationErrors.ValidationErrors)
                            allErrors.Add($"Entity: {validationErrors.Entry.Entity.GetType().Name}, Property: {validationError.PropertyName}, Error: {validationError.ErrorMessage}");

                    transaction.Rollback();
                    throw new Exception("Entity Validation Failed: " + string.Join("; ", allErrors));
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new Exception("Failed to save loan and account: " + ex.Message);
                }
            }
        }


        public LoanAccount GetLoanById(string LNAccountId)
        {
            return _context.LoanAccounts.FirstOrDefault(l => l.LNAccountId == LNAccountId);
        }





        // Add Loan Transaction
        public void AddLoanTransaction(string lnAccountId, decimal amount, string transactionType, decimal penalty = 0)
        {
            var loan = _context.LoanAccounts.FirstOrDefault(l => l.LNAccountId == lnAccountId);
            if (loan == null) throw new Exception("Loan account not found.");

            if (transactionType == "Repayment" || transactionType == "Foreclose")
                loan.DueAmount -= amount;
            else if (transactionType == "Disbursement")
                loan.DueAmount += amount;

            if (loan.DueAmount < 0) loan.DueAmount = 0;

            var transaction = new LoanTransaction
            {
                LNAccountId = lnAccountId,
                Amount = amount,
                TransactionType = transactionType,
                TransactionDate = DateTime.Now,
                Penalty = penalty
            };

            _context.LoanTransactions.Add(transaction);
            _context.SaveChanges();
        }

        // Get all transactions for a specific loan
        public List<LoanTransaction> GetTransactionsByLoan(string lnAccountId)
        {
            return _context.LoanTransactions
                .Where(t => t.LNAccountId == lnAccountId)
                .OrderByDescending(t => t.TransactionDate)
                .ToList();
        }

        public List<LoanTransaction> GetAllLoanTransactions(string LNAccountId)
        {
            return _context.LoanTransactions
                           .Where(t => t.LNAccountId == LNAccountId)
                           .OrderByDescending(t => t.TransactionDate)
                           .ToList();
        }

    }
    public class PendingApprovalViewModel
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string DepartmentID { get; set; }
        public string PAN { get; set; }
    }
}




