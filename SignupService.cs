using Banking;
using Banking.ViewData;
using System;
using System.Linq;
public class SignupService
{
    private readonly BankDBEntities _context;
    public SignupService(BankDBEntities context)
    {
        _context = context;
    }
    public string GenerateEmployeeId()
    {
        string id;
        do
        {
            id = new Random().Next(10000, 99999).ToString();
        } while (_context.Employees.Any(e => e.EmployeeId == id));
        return id;
    }
    public string GenerateCustomerId()
    {
        string id;
        do
        {
            id = "CUS" + new Random().Next(10000, 99999).ToString();
        } while (_context.Customers.Any(c => c.CustomerId == id));
        return id;
    }
    public void Register(SignupViewModel model)
    {
        if (model.UserType == "Employee")
        {
            var empId = GenerateEmployeeId();
            var employee = new Employee
            {
                EmployeeId = empId,
                EmployeeName = model.EmployeeName,
                DepartmentId = model.DepartmentId,
                PAN = model.EmployeePAN,
                Approval = "Pending"
            };
            var login = new LoginData
            {
                UserName = model.UserName,
                UserPassword = model.Password,
                UserRole = "Employee",
                RefID = empId
            };
            _context.Employees.Add(employee);
            _context.LoginDatas.Add(login);
        }
        else if (model.UserType == "Customer")
        {
            var custId = GenerateCustomerId();
            var customer = new Customer
            {
                CustomerId = custId,
                CustomerName = model.CustomerName,
                CustomerDOB = model.CustomerDOB,
                PhoneNumber = model.PhoneNumber,
                CustomerAddress = model.CustomerAddress,
                CustomerEmail = model.CustomerEmail,
                PAN = model.CustomerPAN,
                Approval = "Pending"
            };
            var login = new LoginData
            {
                UserName = model.UserName,
                UserPassword = model.Password,
                UserRole = "Customer",
                RefID = custId
            };
            _context.Customers.Add(customer);
            _context.LoginDatas.Add(login);
        }
        _context.SaveChanges();
    }
}
