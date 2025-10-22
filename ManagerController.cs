using Banking.ViewData;
using Banking;
using BankingService.Services;
using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Services.Description;
using System.Xml.Linq;
namespace Bank.Controllers
{
    public class ManagerController : Controller
    {
        private readonly BankDBEntities _context = new BankDBEntities();
        private readonly ManagerService _managerService;
        public ManagerController()
        {
            _managerService = new ManagerService();
        }



        //-----------------------------------------------------------------------------------------------------------
        //Approve user
        //-------------------------------------------------------------------------------------------------------------
        // GET: Manager
        public ActionResult Index()
        {
            return View();
        }
        // GET: Manager/Approve
        public ActionResult Approve()
        {
            var pendingList = _managerService.GetPendingApprovals();
            return View(pendingList);
        }


        [HttpPost]
        public ActionResult ApproveUser(string pan)
        {
            var customer = _context.Customers.FirstOrDefault(c => c.PAN == pan);
            if (customer != null)
            {
                customer.Approval = "Approved";
                _context.Entry(customer).State = System.Data.Entity.EntityState.Modified;
                _context.SaveChanges();
                TempData["Message"] = $"Customer {customer.CustomerName} approved successfully.";
                return RedirectToAction("Approve");
            }

            var employee = _context.Employees.FirstOrDefault(e => e.PAN == pan);
            if (employee != null)
            {
                employee.Approval = "Approved";
                _context.Entry(employee).State = System.Data.Entity.EntityState.Modified;
                _context.SaveChanges();
                TempData["Message"] = $"Employee {employee.EmployeeName} approved successfully.";
                return RedirectToAction("Approve");
            }

            TempData["Message"] = "User not found.";
            return RedirectToAction("Approve");
        }

        //-------------------------------------------------------------------------------------------------------------------------------------
        //Add and delete Employee 
        //--------------------------------------------------------------------------------------------------------------------------------------
        // GET: Manager/DeleteEmployee
        [HttpGet]
        public ActionResult DeleteEmployee()
        {
            var employees = _context.Employees
                .Where(e => e.Approval == "Approved")
                .ToList();

            return View(employees); // This loads DeleteEmployee.cshtml
        }



        // POST: Manager/DeleteEmployeeConfirmed
        [HttpPost]
        public ActionResult DeleteEmployeeConfirmed(string employeeId)
        {
            try
            {
                var employee = _context.Employees.FirstOrDefault(e => e.EmployeeId == employeeId);
                if (employee != null)
                {
                    var login = _context.LoginDatas.FirstOrDefault(l => l.RefID == employeeId && l.UserRole == "Employee");
                    if (login != null)
                    {
                        _context.LoginDatas.Remove(login);
                    }

                    _context.Employees.Remove(employee);
                    _context.SaveChanges();
                    TempData["Message"] = "Employee deleted successfully.";
                }
                else
                {
                    TempData["Message"] = "Employee not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error deleting employee: " + ex.Message;
            }

            return RedirectToAction("DeleteEmployee");
        }

        // GET: Manager/Employees
        public ActionResult Employees()
        {
            return View();
        }
        // POST: Manager/Employees
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Employees(Employee model, string password)
        {
            if (ModelState.IsValid)
            {
                bool success = _managerService.AddEmployeeWithLogin(model, password);
                if (success)
                {
                    TempData["Message"] = "Employee added successfully!";
                    return RedirectToAction("Employees");
                }
                TempData["Message"] = "Failed to add employee.";
            }
            return View(model);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------
        //Add and delete Customer 
        //--------------------------------------------------------------------------------------------------------------------------------------
        [HttpGet]
        public ActionResult AddCustomer()
        {
            return View("AddCustomer", new Customer());
        }


        [HttpPost]
        public ActionResult AddCustomer(Customer model, string password)
        {
            if (!ModelState.IsValid)
            {
                return View("AddCustomer", model);
            }

            try
            {
                bool success = _managerService.AddCustomerWithLogin(model, password);

                if (success)
                {
                    ViewBag.Message = $"Customer '{model.CustomerName}' added successfully with the customerid {model.CustomerId}!";
                    return View("AddCustomer", new Customer()); // Reset form
                }
                else
                {
                    ModelState.AddModelError("", "Failed to add customer. Please try again.");
                    return View("AddCustomer", model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error: " + ex.Message);
                return View("AddCustomer", model);
            }
        }



        // GET: /Customer/DeleteCustomer
        [HttpGet]
        public ActionResult DeleteCustomer()
        {
            var customers = _managerService.GetAllCustomers(); // You need to fetch the list
            return View("DeleteCustomer", customers);
        }

        // POST: /Customer/DeleteCustomer
        [HttpPost]
        public ActionResult DeleteCustomer(string customerId)
        {
            bool success = _managerService.DeleteCustomer(customerId);

            if (success)
            {
                TempData["Message"] = $"Customer {customerId} deleted successfully.";
            }
            else
            {
                TempData["Message"] = $"Cannot delete customer {customerId}. Active account(s) exist.";
            }

            return RedirectToAction("DeleteCustomer"); // Redirect back to the list
        }

        //-------------------------------------------------------------------------------------------------------------------------------------
        //Add and delete Savings Account
        //--------------------------------------------------------------------------------------------------------------------------------------
        [HttpGet]
        public ActionResult SavingsAccount()
        {
            return View("SavingsAccount");
        }

        [HttpGet]
        public ActionResult LoadPartial(string name)
        {
            switch (name)
            {
                case "_CreateSBAccount":
                    return PartialView("_CreateSBAccount");
                case "_CloseSBAccount":
                    return PartialView("_CloseSBAccount");
                case "_ViewSBTransactions":
                    var transactions = _managerService.GetAllSBTransactions();
                    return PartialView("_ViewSBTransactions", transactions);
                default:
                    return Content("Invalid request");
            }
        }

        // GET: Manager/_CreateSBAccount



        [HttpGet]
        public PartialViewResult CreateSBAccount()
        {
            return PartialView("_CreateSBAccount", new Customer());
        }

        [HttpPost]
        public PartialViewResult CreateSBAccount(Customer customer, decimal initialDeposit, int createdBy)
        {
            try
            {
                if (customer == null || string.IsNullOrEmpty(customer.CustomerId))
                {
                    ViewBag.Message = "Please provide valid customer details.";
                    return PartialView("_CreateSBAccount", customer);
                }

                string generatedAccountId;
                var service = new ManagerService();
                bool success = service.CreateSBAccountWithDetails(customer, initialDeposit, createdBy, out generatedAccountId);

                if (success)
                {
                    ViewBag.Message = $"Savings Account created successfully! Account ID: {generatedAccountId}";
                    return PartialView("_CreateSBAccount", new Customer()); // reset form
                }
                else
                {
                    ViewBag.Message = "Error creating account. Please check the details and try again.";
                    return PartialView("_CreateSBAccount", customer);
                }
            }
            catch (Exception ex)
            {
                // ✅ Return a PartialViewResult with the actual exception message
                ViewBag.Message = "Error creating account: " + ex.Message;
                return PartialView("_CreateSBAccount", customer);
            }
        }

        [HttpGet]
        public JsonResult GetCustomerDetails(string customerId)
        {
            var customer = _context.Customers.SingleOrDefault(c => c.CustomerId == customerId);
            if (customer == null)
                return Json(new { success = false, message = "Customer not found." }, JsonRequestBehavior.AllowGet);

            return Json(new
            {
                success = true,
                data = new
                {
                    customer.CustomerName,
                    customer.PhoneNumber,
                    customer.CustomerDOB,
                    customer.CustomerAddress,
                    customer.CustomerEmail,
                    customer.PAN
                }
            }, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public PartialViewResult CloseSBAccount()
        {
            return PartialView("_CloseSBAccount");
        }

        [HttpPost]
        public PartialViewResult _CloseSBAccount(string accountId, int closedBy)
        {
            var accountDetails = _managerService.GetSBAccountDetails(accountId);

            if (accountDetails == null)
            {
                ViewBag.Message = "Account not found.";
                return PartialView("_CloseSBAccount");
            }

            bool result = _managerService.CloseSBAccount(accountId);

            ViewBag.Message = result
                ? $"Account {accountId} closed successfully by Employee ID {closedBy}."
                : "Error closing account.";

            return PartialView("_CloseSBAccount", accountDetails);
        }

        // Main Transactions Tab — shows all transactions initially
        [HttpGet]
        public PartialViewResult ViewSBTransactions()
        {
            var transactions = _managerService.GetAllSBTransactions();
            return PartialView("_ViewSBTransactions", transactions);
        }

        // GET: Deposit Partial
        [HttpGet]
        public PartialViewResult Deposit()
        {
            return PartialView("_DepositTransaction");
        }

        // POST: Deposit
        [HttpPost]
        public PartialViewResult Deposit(string sbAccountId, decimal amount)
        {
            // Call service
            string message = _managerService.Deposit(sbAccountId, amount);
            ViewBag.Message = message;

            // Load recent transactions for this account
            ViewBag.Transactions = _managerService.GetTransactionsByAccount(sbAccountId);

            return PartialView("_DepositTransaction");
        }

        // GET: Withdraw Partial
        [HttpGet]
        public PartialViewResult Withdraw()
        {
            return PartialView("_WithdrawTransaction");
        }

        // POST: Withdraw
        [HttpPost]
        public PartialViewResult Withdraw(string sbAccountId, decimal amount)
        {
            // Call service
            string message = _managerService.Withdraw(sbAccountId, amount);
            ViewBag.Message = message;

            // Load recent transactions for this account
            ViewBag.Transactions = _managerService.GetTransactionsByAccount(sbAccountId);

            return PartialView("_WithdrawTransaction");
        }


        //// GET: Transaction History Partial
        [HttpGet]
        public PartialViewResult TransactionHistory()
        {
            // Initially, show all transactions (or empty if you want user to input account)
            var transactions = _managerService.GetAllSBTransactions();
            return PartialView("_TransactionHistory", transactions);
        }

        // Optional: POST to filter by account
        [HttpPost]
        public PartialViewResult TransactionHistory(string sbAccountId)
        {
            List<SavingsTransaction> transactions;
            if (!string.IsNullOrEmpty(sbAccountId))
            {
                transactions = _managerService.GetTransactionsByAccount(sbAccountId);
            }
            else
            {
                transactions = _managerService.GetAllSBTransactions();
            }

            ViewBag.AccountId = sbAccountId;
            return PartialView("_TransactionHistory", transactions);
        }
        // GET: Load Export Partial
        [HttpGet]
        public PartialViewResult ExportTransactions()
        {
            return PartialView("_ExportTransactions", new List<SavingsTransaction>());
        }

        // POST: Filter transactions by account
        [HttpPost]
        public PartialViewResult ExportTransactions(string sbAccountId)
        {
            List<SavingsTransaction> transactions = new List<SavingsTransaction>();
            if (!string.IsNullOrEmpty(sbAccountId))
            {
                transactions = _managerService.GetTransactionsByAccount(sbAccountId);
            }

            ViewBag.AccountId = sbAccountId;
            return PartialView("_ExportTransactions", transactions);
        }

        // POST: Export filtered transactions to Excel
        [HttpPost]
        public ActionResult ExportToExcel(string sbAccountId)
        {
            var transactions = _managerService.GetTransactionsByAccount(sbAccountId);
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Transactions");
                ws.Cells[1, 1].Value = "Transaction ID";
                ws.Cells[1, 2].Value = "Account ID";
                ws.Cells[1, 3].Value = "Amount";
                ws.Cells[1, 4].Value = "Type";
                ws.Cells[1, 5].Value = "Date & Time";
                ws.Cells[1, 6].Value = "Balance";

                int row = 2;
                foreach (var t in transactions)
                {
                    ws.Cells[row, 1].Value = t.TransactionId;
                    ws.Cells[row, 2].Value = t.SBAccountId;
                    ws.Cells[row, 3].Value = t.Amount;
                    ws.Cells[row, 4].Value = t.TransactionType;
                    ws.Cells[row, 5].Value = t.TransactionDate?.ToString("dd-MM-yyyy HH:mm:ss");
                    ws.Cells[row, 6].Value = t.Balance;
                    row++;
                }

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                string fileName = $"Transactions_{sbAccountId}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        // POST: Export filtered transactions to PDF
        [HttpPost]
        public ActionResult ExportToPDF(string sbAccountId)
        {
            var transactions = _managerService.GetTransactionsByAccount(sbAccountId);

            using (MemoryStream ms = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4, 10, 10, 10, 10);
                PdfWriter.GetInstance(doc, ms);
                doc.Open();

                Paragraph title = new Paragraph($"Transaction History for Account {sbAccountId}")
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20f
                };
                doc.Add(title);

                PdfPTable table = new PdfPTable(6) { WidthPercentage = 100 };
                table.AddCell("Transaction ID");
                table.AddCell("Account ID");
                table.AddCell("Amount");
                table.AddCell("Type");
                table.AddCell("Date & Time");
                table.AddCell("Balance");

                foreach (var t in transactions)
                {
                    table.AddCell(t.TransactionId.ToString());
                    table.AddCell(t.SBAccountId);
                    table.AddCell(t.Amount.ToString());
                    table.AddCell(t.TransactionType);
                    table.AddCell(t.TransactionDate?.ToString("dd-MM-yyyy HH:mm:ss") ?? "-");
                    table.AddCell(t.Balance.ToString());
                }

                doc.Add(table);
                doc.Close();

                byte[] bytes = ms.ToArray();
                string fileName = $"Transactions_{sbAccountId}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                return File(bytes, "application/pdf", fileName);
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------
        //Loan Account
        //--------------------------------------------------------------------------------------------------------------------------------------
        [HttpGet]
        public PartialViewResult _CloseLoanAccount()
        {
            return PartialView("_CloseLoanAccount", new LoanAccount());
        }

        [HttpPost]
        public PartialViewResult _CloseLoanAccount(string LNAccountId)
        {
            var loan = _managerService.GetLoanById(LNAccountId);
            if (loan == null)
            {
                ViewBag.Message = "Loan account not found.";
                return PartialView("_CloseLoanAccount");
            }

            if (loan.DueAmount > 0)
            {
                ViewBag.Message = $"Loan cannot be closed. Outstanding due: ₹{loan.DueAmount:F2}";
                return PartialView("_CloseLoanAccount");
            }

            try
            {
                _managerService.CloseLoanAccount(loan);
                ViewBag.Message = "Loan account closed successfully.";
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Error closing loan account: " + ex.Message;
            }

            return PartialView("_CloseLoanAccount");
        }



        [HttpGet]
        public ActionResult LoanAccount()
        {
            var loan = new LoanAccount
            {
                LNAccountId = _managerService.GenerateLoanAccountId()
            };
            return View("_LoanAccountMain", loan);
        }

        [HttpGet]
        public PartialViewResult _CreateLoanAccount()
        {
            return PartialView("_CreateLoanAccount", new LoanAccount
            {
                LNAccountId = _managerService.GenerateLoanAccountId()
            });
        }

        [HttpPost]
        public PartialViewResult _CreateLoanAccount(LoanAccount model, decimal MonthlyTakeHome, int CreatedBy, int MonthDueDate)
        {
            var customer = _managerService.GetCustomerById(model.CustomerId);
            if (customer == null)
            {
                ViewBag.Message = "Customer not found.";
                return PartialView("_CreateLoanAccount", model);
            }

            if (!string.Equals(customer.Approval, "Approved", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Message = "Loan account can only be created for approved customers.";
                return PartialView("_CreateLoanAccount", model);
            }

            string error;
            decimal emi;
            var processedLoan = _managerService.ProcessLoan(model, customer, MonthlyTakeHome, out error, out emi);

            if (processedLoan == null)
            {
                ViewBag.Message = error;
                return PartialView("_CreateLoanAccount", model);
            }

            try
            {
                _managerService.SaveLoanAndAccount(processedLoan, CreatedBy, DateTime.Now,null, "Active");
                ViewBag.Message = $"Loan account created successfully! EMI: ₹{emi:F2}";
                return PartialView("_CreateLoanAccount", new LoanAccount
                {
                    LNAccountId = _managerService.GenerateLoanAccountId()
                });
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Error saving loan account: " + ex.Message;
                return PartialView("_CreateLoanAccount", model);
            }
        }
        [HttpGet]
        public PartialViewResult _PayEMI()
        {
            // Only render the form to enter LoanAccountId
            return PartialView("_PayEMI");
        }



        [HttpPost]
        public PartialViewResult _PayEMI(string LNAccountId, decimal Amount)
        {
            try
            {
                _managerService.PayEMI(LNAccountId, Amount);
                ViewBag.Message = $"EMI of ₹{Amount:F2} paid successfully.";
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Error paying EMI: " + ex.Message;
            }

            return PartialView("_PayEMI");
        }

        [HttpGet]
        public JsonResult GetLoanDetails(string LNAccountId)
        {
            var loan = _managerService.GetLoanById(LNAccountId);
            if (loan == null)
                return Json(new { success = false, message = "Loan not found" }, JsonRequestBehavior.AllowGet);

            return Json(new
            {
                success = true,
                data = new
                {
                    loan.LoanAmount,
                    loan.Emi,
                    loan.DueAmount
                }
            }, JsonRequestBehavior.AllowGet);
        }



        [HttpGet]
        public PartialViewResult _LoanTransactions()
        {
            return PartialView("_LoanTransactions", new List<LoanTransaction>());
        }

        [HttpPost]
        public PartialViewResult _LoanTransactions(string lnAccountId)
        {
            var transactions = _managerService.GetTransactionsByLoan(lnAccountId);
            ViewBag.LoanAccountId = lnAccountId;
            return PartialView("_LoanTransactions", transactions);
        }

        [HttpGet]
        public PartialViewResult _ForecloseLoan()
        {
            return PartialView("_ForecloseLoan", new LoanAccount());
        }

        [HttpPost]
        public PartialViewResult _ForecloseLoan(string lnAccountId, decimal amountPaid, decimal penalty = 0)
        {
            var loan = _managerService.GetLoanAccountById(lnAccountId);
            if (loan == null)
            {
                ViewBag.Message = "Loan account not found.";
                return PartialView("_ForecloseLoan", new LoanAccount());
            }

            loan.DueAmount -= amountPaid;
            if (loan.DueAmount < 0) loan.DueAmount = 0;

            if (loan.DueAmount == 0) loan.LoanStatus = "Closed";

            _managerService.AddLoanTransaction(lnAccountId, amountPaid, "Foreclose", penalty);

            ViewBag.Message = $"Loan Foreclosed. Remaining Due: ₹{loan.DueAmount:F2}";
            return PartialView("_ForecloseLoan", loan);
        }

        [HttpGet]
        public PartialViewResult _ExportLoanTransactions()
        {
            return PartialView("_ExportLoanTransactions", new List<LoanTransaction>());
        }

        [HttpPost]
        public PartialViewResult _ExportLoanTransactions(string lnAccountId)
        {
            var transactions = _managerService.GetTransactionsByLoan(lnAccountId);
            ViewBag.LoanAccountId = lnAccountId;
            return PartialView("_ExportLoanTransactions", transactions);
        }


        [HttpGet]
        public PartialViewResult _ViewLoanTransactions(string LNAccountId)
        {
            try
            {
                if (string.IsNullOrEmpty(LNAccountId))
                {
                    ViewBag.Message = "Loan Account ID is missing.";
                    return PartialView("_ViewLoanTransactions", new List<LoanTransaction>());
                }

                var transactions = _managerService.GetAllLoanTransactions(LNAccountId);

                if (transactions == null || transactions.Count == 0)
                {
                    ViewBag.Message = "No transactions found for this Loan Account.";
                }

                return PartialView("_ViewLoanTransactions", transactions);
            }
            catch (Exception ex)
            {
                // Log or show the real exception temporarily
                ViewBag.Error = ex.Message;
                return PartialView("_ViewLoanTransactions", new List<LoanTransaction>());
            }
        }




        // Loads the tab (input box + container)
        [HttpGet]
        public PartialViewResult _LoanTransactionsTab()
        {
            return PartialView("_LoanTransactionsTab");
        }

        // Loads only the table for a specific Loan Account
        [HttpGet]
        public PartialViewResult _LoanTransactionTable(string LNAccountId)
        {
            if (string.IsNullOrEmpty(LNAccountId))
                return PartialView("_LoanTransactionTable", new List<LoanTransaction>());

            var transactions = _managerService.GetAllLoanTransactions(LNAccountId);
            return PartialView("_LoanTransactionTable", transactions);
        }


        // GET: Manager/Customers
        public ActionResult Customers()
        {
            // Placeholder for Customer logic
            return View();
        }
    }
}