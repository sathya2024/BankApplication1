using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Banking.ViewData
{
    public class SignupViewModel
    {
        [Required(ErrorMessage = "User type is required")]
        public string UserType { get; set; } // "Customer" or "Employee"


        // Login fields
        [Required(ErrorMessage = "Username is required")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm Password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }


        // Customer fields
        public string CustomerName { get; set; }

        [Required(ErrorMessage ="Date Of Birth is required")]
        [DataType(DataType.Date)]
        public DateTime? CustomerDOB { get; set; }

        [Required(ErrorMessage = "Date Of Birth is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Address is required")]
        public string CustomerAddress { get; set; }

        [Required(ErrorMessage = "Email is required")]
        public string CustomerEmail { get; set; }

        [Required(ErrorMessage = "PAN is required")]
        [RegularExpression(@"^[A-Z]{4}[0-9]{4}$", ErrorMessage = "PAN must be 4 uppercase letters followed by 4 digits")]
        public string CustomerPAN { get; set; }



        // Employee fields
        public string EmployeeName { get; set; }

        [Required(ErrorMessage = "Department is required if Employee")]
        public string DepartmentId { get; set; }

        [Required(ErrorMessage = "PAN is required")]
        [RegularExpression(@"^[A-Z]{4}[0-9]{4}$", ErrorMessage = "PAN must be 4 uppercase letters followed by 4 digits")]
        public string EmployeePAN { get; set; }
    }
}
