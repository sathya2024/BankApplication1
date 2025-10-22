using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BankingService.Services;
using Banking;
using Banking.ViewData;
public class SignupController : Controller
{
    private readonly SignupService _service;
    public SignupController()
    {
        _service = new SignupService(new BankDBEntities());
    }
    [HttpGet]
    public ActionResult Index()
    {
        return View(new SignupViewModel());
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Index(SignupViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (ModelState.IsValid)
        {
            // Auto-assign name from username
            if (model.UserType == "Customer")
            {
                model.CustomerName = model.UserName;
            }
            else if (model.UserType == "Employee")
            {
                model.EmployeeName = model.UserName;

                // Map DepartmentId based on selection
                if (model.DepartmentId == "Employee_Deposit")
                    model.DepartmentId = "1"; // example ID from dep table
                else if (model.DepartmentId == "Employee_Loan")
                    model.DepartmentId = "2"; // example ID from dep table
            }

            _service.Register(model);
            TempData["SuccessMessage"] = "Signup successful!";
            return RedirectToAction("Index","Login");
        }
        return View(model);
    }
}
