using BankingService.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Bank;
using Banking;
using BankingService;

namespace Bank.Controllers
{

    namespace Bank.Controllers
    {
        public class LoginController : Controller
        {
            private readonly LoginService _loginService = new LoginService();

            [HttpGet]
            public ActionResult Index()
            {
                return View();
            }

            [HttpPost]
            public ActionResult Index(string userid, string password)
            {
                var role = _loginService.AuthenticateUser(userid, password);
                if (role != null)
                {
                    switch (role)
                    {
                        case "Customer":
                            return RedirectToAction("Dashboard", "Customer", new { id = userid });
                        case "Employee":
                            return RedirectToAction("Index", "Employee");
                        case "Manager":
                            return RedirectToAction("Index", "Manager");
                    }
                }

                ViewBag.ErrorMessage = "Invalid UserID or Password.";
                return View();
            }

        }

    }
}