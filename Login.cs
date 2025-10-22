using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Banking;

namespace BankingService.Services
{
    public class LoginService
    {
        BankDBEntities b=new BankDBEntities();
        private readonly BankDBEntities _context;

        public LoginService()
        {
            _context = new BankDBEntities(); // Your EF context
        }

        public string GetCustomerIdByRefIdAndPassword(string refid, string password)
        {
            var login = _context.LoginDatas
                .FirstOrDefault(l => l.RefID == refid && l.UserPassword == password && l.UserRole == "Customer");

            return login?.RefID; // This is your CustomerId
        }

        public string AuthenticateUser(string refid, string password)
        {
            // Check in Customers
            var customer = _context.Customers
                .FirstOrDefault(c => c.CustomerId == refid);

            if (customer != null)
            {
                var loginCustomer = _context.LoginDatas
                    .FirstOrDefault(l => l.RefID == refid && l.UserPassword == password && l.UserRole == "Customer");
                if (loginCustomer != null)
                {
                    // Save role in session and return role
                    HttpContext.Current.Session["UserRole"] = "Customer";
                    HttpContext.Current.Session["RefID"] = refid;
                    return "Customer";
                }
            }

            // Check in Employees
            var employee = _context.Employees
                .FirstOrDefault(e => e.EmployeeId == refid);
            if (employee != null)
            {
                var loginEmployee = _context.LoginDatas
                    .FirstOrDefault(l => l.RefID == refid && l.UserPassword == password && l.UserRole == "Employee");
                if (loginEmployee != null)
                {
                    HttpContext.Current.Session["UserRole"] = "Employee";
                    HttpContext.Current.Session["RefID"] = refid;
                    return "Employee";
                }
            }

            // Check in Managers
            var manager = _context.Managers
                .FirstOrDefault(m => m.ManagerId == refid);
            if (manager != null)
            {
                var loginManager = _context.LoginDatas
                    .FirstOrDefault(l => l.RefID == refid && l.UserPassword == password && l.UserRole == "Manager");
                if (loginManager != null)
                {
                    HttpContext.Current.Session["UserRole"] = "Manager";
                    HttpContext.Current.Session["RefID"] = refid;
                    return "Manager";
                }
            }

            return null; // Authentication failed
        }
        
    }

}

