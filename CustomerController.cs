using System;
using System.Web.Mvc;
using BankingService.Services;
using Banking;

namespace BankingWeb.Controllers
{
    public class CustomerController : Controller
    {
        private readonly CustomerService _service;

        public CustomerController()
        {
            _service = new CustomerService();
        }

        //Dashboard
    public ActionResult Dashboard(string id)
    {
        var model = _service.GetDashboard(id);
        if (model == null)
        {
            ViewBag.ErrorMessage = "Customer not found.";
            return RedirectToAction("Index", "Login");
        }
        model.CustomerId = id;
        return View(model);
    }

    //Account Details
    public ActionResult AccountDetails(string id)
        {
            var model = _service.GetAccountDetails(id);
            return View(model);
        }
    //Savings Tab
        public ActionResult SavingsView(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Message"] = "Invalid customer ID.";
                return RedirectToAction("Dashboard");
            }

            var cleanId = id.Trim().ToUpper(); 
            var model = _service.GetSavingsDetails(cleanId);
            ViewBag.CustomerId = cleanId;

            return View(model);
        }

        public PartialViewResult SavingsDetails(string id)
        {
            var cleanId = id?.Trim().ToUpper();
            var model = _service.GetSavingsDetails(cleanId);
            return PartialView("_SavingsDetails", model);
        }

        public PartialViewResult DepositForm(string id)
        {
            var model = _service.GetSavingsDetails(id);
            return PartialView("_DepositForm", model);
        }

        [HttpPost]
        public ActionResult Deposit(decimal amount, string id)
        {
            var accountId = _service.GetSavingsAccountId(id);
            var success = _service.Deposit(accountId, amount);
            TempData["Message"] = success ? "Deposit successful" : "Deposit failed";
            return RedirectToAction("SavingsView", new { id });
        }

        public PartialViewResult WithdrawForm(string id)
        {
            var model = _service.GetSavingsDetails(id);
            return PartialView("_WithdrawForm", model);
        }

        [HttpPost]
        public ActionResult Withdraw(decimal amount, string id)
        {
            var accountId = _service.GetSavingsAccountId(id);
            var success = _service.Withdraw(accountId, amount);
            TempData["Message"] = success ? "Withdrawal successful" : "Withdrawal failed";
            return RedirectToAction("SavingsView", new { id });
        }

        public PartialViewResult SavingsTransactions(string id)
        {
            var accountId = _service.GetSavingsAccountId(id);
            var transactions = _service.GetTopTransactions(accountId, 3);
            return PartialView("_SavingsTransactions", transactions);
        }

        public PartialViewResult ViewAllTransactions(string id)
        {
            var accountId = _service.GetSavingsAccountId(id);
            var transactions = _service.GetSavingsTransactions(accountId);
            return PartialView("_SavingsTransactions", transactions);
        }



        // FD Main View
        public ActionResult FDView(string id)
        {
            ViewBag.CustomerId = id;
            var model = _service.GetFDDetails(id);
            return View(model);
        }

        // Partial: FD Details
        public PartialViewResult FDDetails(string id)
        {
            var model = _service.GetFDDetails(id);
            return PartialView("_FDDetails", model);
        }

        // Partial: FD Transactions
        public PartialViewResult FDTransactions(string id)
        {
            var transactions = _service.GetFDTransactions(id);
            return PartialView("_FDTransactions", transactions);
        }

        // Partial: FD Closure Form
        public PartialViewResult FDClosureForm(string id)
        {
            ViewBag.CustomerId = id;
            return PartialView("_FDClosureForm");
        }

        // POST: Premature FD Closure
        [HttpPost]
        public ActionResult PrematureWithdraw(string fdAccountId)
        {
            var result = _service.PrematureWithdrawFD(fdAccountId);

            TempData["Message"] = result.Message;
            TempData["Success"] = result.Success;
            TempData["Amount"] = result.Amount;
            var customerId = _service.GetCustomerIdByFD(fdAccountId);
            return RedirectToAction("FDView", new { id = customerId });

        }

        [HttpPost]
        public ActionResult WithdrawFD(string fdAccountId)
        {
            var result = _service.WithdrawFD(fdAccountId);
            TempData["Success"] = result.Success;
            TempData["Message"] = result.Message;
            TempData["Amount"] = result.Amount;
            var customerId = _service.GetCustomerIdByFD(fdAccountId);
            return RedirectToAction("FDView", new { id = customerId });
        }

      //View all loans for a customer
        public ActionResult LoanView(string id)
        {
            var loans = _service.GetAllLoans(id);
            ViewBag.CustomerId = id;
            return View(loans); // Renders LoanView.cshtml with List<LoanAccount>
        }

        // Loan details for a specific loan
        public PartialViewResult LoanDetails(string loanId)
        {
            var loan = _service.GetLoanById(loanId);
            return PartialView("_LoanDetails", loan);
        }

        //  EMI payment form
        public PartialViewResult EMI(string loanId)
        {

            var loan = _service.GetLoanById(loanId);
            ViewBag.LoanId = loanId;
            ViewBag.CustomerId = loan?.CustomerId;
            return PartialView("_EMI");

            
        }
        public ActionResult AutoPayEMI(string loanId)
        {
            var loan = _service.GetLoanById(loanId);
            if (loan == null || !loan.Emi.HasValue || !loan.DueAmount.HasValue)
            {
                TempData["Message"] = "Loan not found or EMI not available.";
                return RedirectToAction("LoanView", new { id = loan?.CustomerId });
            }

            var emiAmount = Math.Ceiling(loan.Emi.Value);
            var success = _service.PayEMI(loan.CustomerId, loanId, emiAmount);

            TempData["Message"] = success ? $"EMI of ₹{emiAmount:N0} paid successfully." : "EMI payment failed.";
            return RedirectToAction("LoanView", new { id = loan.CustomerId });
        }



        //  Part payment form
        public PartialViewResult PartPay(string loanId)
        {
            var loan = _service.GetLoanById(loanId);
            ViewBag.LoanId = loanId;
            ViewBag.CustomerId = loan?.CustomerId;
            return PartialView("_PartPay");
        }

        [HttpPost]
        public ActionResult PartPay(decimal amount, string loanId, string customerId)
        {
            var success = _service.PartPay(customerId, loanId, amount);
            TempData["Message"] = success ? "Part payment successful." : "Payment failed.";
            return RedirectToAction("LoanView", new { id = customerId });
        }

        //  Foreclosure form
        //public PartialViewResult Foreclose(string loanId)
        //{
        //    var loan = _service.GetLoanById(loanId);
        //    ViewBag.LoanId = loanId;
        //    ViewBag.CustomerId = loan?.CustomerId;
        //    return PartialView("_Foreclose");
        //}
        public ActionResult Foreclose(string loanId)
        {
            var loan = _service.GetLoanById(loanId);
            if (loan == null)
            {
                TempData["Message"] = "Loan not found.";
                return RedirectToAction("LoanSummary");
            }

            return View("_Foreclose", loan); // Make sure this matches your view name
        }

        [HttpPost]
        public ActionResult ForecloseLoan(string loanId, string customerId)
        {
            var success = _service.ForecloseLoan(customerId, loanId);
            TempData["Message"] = success ? "Loan foreclosed successfully." : "Foreclosure failed.";
            return RedirectToAction("LoanView", new { id = customerId });
        }

        //  Transactions for a specific loan
        public PartialViewResult LoanTransactions(string loanId)
        {
            var txns = _service.GetLoanTransactionsByLoanId(loanId);
            return PartialView("_LoanTransactions", txns);
        }

    }
}
    
