using bankAPP.DB;
using bankLIB;
using System.ComponentModel;
using System.Diagnostics;
using System.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Transactions;
using System.Net.Http.Headers;
using System.Data;
using Microsoft.IdentityModel.Tokens;

#region Connecting the Code to DB through DbContext()
var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("userBankManagementDB")
    ?? throw new InvalidOperationException("Connection string 'userBankManagementDB' not found.");

builder.Services.AddDbContext<UserBankManagementDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddSingleton<JwtTokenService>();

var host = builder.Build();
var jwtTokenService = host.Services.GetRequiredService<JwtTokenService>();

UserBankManagementDbContext GetDbContext() => host.Services.CreateScope().ServiceProvider.GetRequiredService<UserBankManagementDbContext>();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<UserBankManagementDbContext>();
    try
    {
        if (dbContext.Database.CanConnect())
        {
            Console.WriteLine("Successfully connected to userBankManagementDB!");
            var accountCount = dbContext.Accounts.Count();
            Console.WriteLine($"Found {accountCount} account records in the database.");
        }
        else
        {
            Console.WriteLine("Could not connect to the database.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database connection failed: {ex.Message}");
    }
}
#endregion
#region Admin Login hardcoded
string adminUsername = "admin";
string adminPassword = "adminpassword";
#endregion
#region Main Menu Logic
bool onMainMenu = true;
while(onMainMenu)
{
    Console.WriteLine("Welcome to the bank! Choose your options");
    Console.WriteLine("1. Customer");
    Console.WriteLine("2. Admin");
    Console.WriteLine("3. Exit");
    int mainMenuInput = ReadMenuChoice();

    switch(mainMenuInput)
    {
        #region Customer View
        case 1:
            Console.WriteLine("Customer Username: ");
            string c_user_input = Console.ReadLine() ?? string.Empty;
            Console.WriteLine("Customer Password: ");
            string c_pass_input = PasswordReader.ReadPassword();
            using(var db = GetDbContext())
            {
                    var currentCustomer = db.Accounts
                        .Where(a => a.Username == c_user_input && a.Password == c_pass_input)
                        .AsEnumerable()
                        .FirstOrDefault(a => string.Equals(a.Username, c_user_input, StringComparison.Ordinal)
                            && string.Equals(a.Password, c_pass_input, StringComparison.Ordinal));
                    if(currentCustomer != null)
                    {
                        string customerToken = jwtTokenService.CreateToken(
                            currentCustomer.Username,
                            "Customer",
                            currentCustomer.AccNo);
                        var customerPrincipal = jwtTokenService.ValidateToken(customerToken);
                        if (!customerPrincipal.IsInRole("Customer") ||
                            customerPrincipal.FindFirst("account_number")?.Value != currentCustomer.AccNo.ToString())
                        {
                            Console.WriteLine("Customer authentication failed.");
                            break;
                        }
                        Console.WriteLine($"JWT authenticated {customerPrincipal.Identity?.Name} as Customer (expires in 15 minutes).");

                        bool oncustomerMenu = true;
                        while(oncustomerMenu && IsTokenAuthorized(jwtTokenService, customerToken, "Customer"))
                        {
                            Console.WriteLine("What would you like to do?");
                            Console.WriteLine("1. Check Account Details");
                            Console.WriteLine("2. Withdraw");
                            Console.WriteLine("3. Deposit");
                            Console.WriteLine("4. Transfer");
                            Console.WriteLine("5. Check last 5 transaction");
                            Console.WriteLine("6. Request Cheque Book");
                            Console.WriteLine("7. Change Password");
                            Console.WriteLine("8. Exit");
                            int c_accInput = ReadMenuChoice();
                            if (!IsTokenAuthorized(jwtTokenService, customerToken, "Customer"))
                            {
                                oncustomerMenu = false;
                                Console.WriteLine("Customer session expired or is no longer valid. Please log in again.");
                                break;
                            }
                            switch(c_accInput)
                            {
                                case 1:
                                    Console.WriteLine("----------------------------------------------");
                                    Console.WriteLine("Name : "+currentCustomer.Name);
                                    Console.WriteLine("Account Number : "+currentCustomer.AccNo);
                                    Console.WriteLine("Account Balance : "+currentCustomer.AccBalance);
                                    Console.WriteLine("----------------------------------------------");
                                    break;
                                case 2:
                                    Console.WriteLine("How much would you like to withdraw?");
                                    if (int.TryParse(Console.ReadLine(), out int withdraw_amount) && withdraw_amount > 0)
                                    {

                                            if (withdraw_amount > currentCustomer.AccBalance)
                                            {
                                                Console.WriteLine("Insufficient amount of money.");
                                            }
                                            else
                                            {
                                                int new_withdraw_balance = currentCustomer.Withdraw(withdraw_amount);
                                                db.SaveChanges();
                                                Console.WriteLine("----------------------------------------------");
                                                Console.WriteLine("Withdraw Amount : $" + withdraw_amount);
                                                Console.WriteLine("Your new account balance : $" + new_withdraw_balance);
                                                Console.WriteLine("Withdraw success!");
                                                Console.WriteLine("----------------------------------------------");
                                            }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Invalid withdrawal amount.");
                                    }
                                    break;
                                case 3:
                                    Console.WriteLine("How much would you like to Deposit?");
                                    int deposit_amount = Convert.ToInt32(Console.ReadLine());
                                    int new_deposit_balance = currentCustomer.Deposit(deposit_amount);
                                    db.SaveChanges();
                                    Console.WriteLine("----------------------------------------------");
                                    Console.WriteLine("Deposit Amount : $" + deposit_amount);
                                    Console.WriteLine("Your new account balance : $" + new_deposit_balance);
                                    Console.WriteLine("Deposit success!");
                                    Console.WriteLine("----------------------------------------------");
                                    break;
                                case 4:
                                    Console.WriteLine("How much would you like to Transfer?");
                                    if (int.TryParse(Console.ReadLine(), out int transfer_amount) && transfer_amount <= currentCustomer.AccBalance)
                                    {
                                        Console.WriteLine("Enter recipient account number:");
                                        if (int.TryParse(Console.ReadLine(), out int recipientAccNo))
                                        {
                                                var recipient = db.Accounts.FirstOrDefault(a => a.AccNo == recipientAccNo);
                                                if (recipient != null)
                                                {
                                                    currentCustomer.AccBalance -= transfer_amount;
                                                    recipient.AccBalance = recipient.AccBalance + transfer_amount;
                                                    db.SaveChanges();
                                                    Console.WriteLine("----------------------------------------------");
                                                    Console.WriteLine("Transfer Amount : $" + transfer_amount);
                                                    Console.WriteLine("Sender's New Balance : $" + currentCustomer.AccBalance);
                                                    Console.WriteLine("Receiver's New Balance : $" + recipient.AccBalance);
                                                    Console.WriteLine("Transfer successful!");
                                                    Console.WriteLine("----------------------------------------------");
                                                    currentCustomer.Transfer(transfer_amount,recipient.Name);
                                                }
                                                else
                                                {
                                                    Console.WriteLine("Recipient account not found.");
                                                }

                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Invalid amount or insufficient balance.");
                                    }
                                    break;
                                case 5:
                                    Console.WriteLine("----------------------------------------------");
                                    Console.WriteLine("Last 5 Transactions:");
                                    if (!currentCustomer.Transactions.Any())
                                    {
                                        Console.WriteLine("No transactions found.");
                                    }
                                    else
                                    {
                                        foreach (string transaction in currentCustomer.GetLastFiveTransactions())
                                        {
                                            Console.WriteLine(transaction);
                                        }
                                    }
                                    Console.WriteLine("----------------------------------------------");
                                    break;
                                case 6:
                                    currentCustomer.RequestChequeBook(currentCustomer);
                                    db.SaveChanges();
                                    break;
                                case 7:
                                    Console.Write("Enter your current password: ");
                                    string currentPass = PasswordReader.ReadPassword();

                                    if (currentPass == currentCustomer.Password)
                                    {
                                        Console.Write("Enter new password: ");
                                        currentCustomer.Password = PasswordReader.ReadPassword();
                                        
                                        if(currentPass == currentCustomer.Password)
                                        {
                                            Console.WriteLine("Your password is the same as previous. No change has been made.");
                                        }
                                        else
                                        {
                                            db.SaveChanges();
                                            Console.WriteLine("Password updated successfully.");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Invalid Password");
                                    }
                                    break;
                                case 8:
                                    oncustomerMenu = false;
                                    Console.WriteLine("Exiting Customer Account............");
                                    break;
                                default:
                                    Console.WriteLine("Invalid Choice");
                                    break;
                            }
                        }
                        if (oncustomerMenu)
                        {
                            Console.WriteLine("Customer session expired or is no longer valid. Please log in again.");
                        }
                    }
                else
                {
                    Console.WriteLine("Invalid Credential");
                }
            }
            break;
        #endregion
        #region Admin View
        case 2:
            Console.WriteLine("Admin Username: ");
            string a_user_input = Console.ReadLine() ?? string.Empty;
            if(adminUsername == a_user_input)
            {
                Console.WriteLine("Admin Password: ");
                string a_pass_input = PasswordReader.ReadPassword();
                if(adminPassword == a_pass_input)
                {
                    string adminToken = jwtTokenService.CreateToken(adminUsername, "Admin");
                    var adminPrincipal = jwtTokenService.ValidateToken(adminToken);
                    if (!adminPrincipal.IsInRole("Admin") || adminPrincipal.Identity?.Name != adminUsername)
                    {
                        Console.WriteLine("Admin authentication failed.");
                        break;
                    }
                    Console.WriteLine($"JWT authenticated {adminPrincipal.Identity.Name} as Admin (expires in 15 minutes).");

                    bool onadminMenu = true;
                    while(onadminMenu && IsTokenAuthorized(jwtTokenService, adminToken, "Admin"))
                    {
                        Console.WriteLine("What would you like to do?");
                        Console.WriteLine("1. Create New Account");
                        Console.WriteLine("2. Delete Account");
                        Console.WriteLine("3. Edit Account Details");
                        Console.WriteLine("4. Display Summary");
                        Console.WriteLine("5. Reset Customer Password");
                        Console.WriteLine("6. Approve Cheque Book Request");
                        Console.WriteLine("7. Exit");
                        int a_accInput = ReadMenuChoice();
                        if (!IsTokenAuthorized(jwtTokenService, adminToken, "Admin"))
                        {
                            onadminMenu = false;
                            Console.WriteLine("Admin session expired or is no longer valid. Please log in again.");
                            break;
                        }
                        switch(a_accInput)
                        {
                            case 1:
                                Console.WriteLine("Enter the username");
                                string new_user = Console.ReadLine();
                                Console.WriteLine("Enter the password");
                                string new_pass = Console.ReadLine();
                                Console.WriteLine("Enter the name");
                                string new_name = Console.ReadLine();
                                Console.WriteLine("Enter the account number");
                                int new_accNo = Convert.ToInt32(Console.ReadLine());
                                Console.WriteLine("Enter the account balance");
                                int new_accbalance = Convert.ToInt32(Console.ReadLine());

                                using (var db = GetDbContext())
                                {
                                    // 1. Instantiate the object with properties mapped to your DB schema
                                    var newAccount = new Account()
                                    {
                                        Username = new_user,
                                        Password = new_pass,
                                        Name = new_name,
                                        AccNo = new_accNo,
                                        AccBalance = new_accbalance,
                                    };
                                db.Accounts.Add(newAccount);
                                db.SaveChanges();
                                Console.WriteLine("New account has been created");
                                }
                                break;

                            case 2:
                                using(var db = GetDbContext())
                                {
                                    Console.WriteLine("Customer accounts:");
                                    foreach (var item in db.Accounts)
                                    {
                                        Console.WriteLine(item.Name + ":" + item.AccNo);
                                    }
                                    Console.WriteLine("Which account (account Number) do you want to delete?");
                                    string accDelete = Console.ReadLine();
                                    if (!int.TryParse(accDelete, out int delete_accNo))
                                    {
                                        Console.WriteLine("Please enter a valid account number.");
                                        break;
                                    }

                                    Account? accountToDelete = db.Accounts.FirstOrDefault(account => account.AccNo == delete_accNo);

                                    if (accountToDelete is null)
                                    {
                                        Console.WriteLine("Account not found.");
                                        break;
                                    }

                                    db.Accounts.Remove(accountToDelete);
                                    db.SaveChanges();
                                    Console.WriteLine("Account " +  delete_accNo + " deleted.");
                                }

                                break;
                            case 3:
                                using(var db = GetDbContext())
                                {
                                    bool onEditMenu = true; 
                                    while(onEditMenu)
                                    {
                                        Console.WriteLine("Which customer do you want to edit on?");
                                        foreach(var item in db.Accounts)
                                        {
                                            Console.WriteLine("Name :" + item.Name  +"|" + "Account Number: " + item.AccNo);
                                        }
                                        string accNo_selected_to_edit = Console.ReadLine() ?? String.Empty;
                                        if (int.TryParse(accNo_selected_to_edit, out int accNo_selected))
                                        {
                                            Account? accountToDisplay = db.Accounts.FirstOrDefault(account => account.AccNo == accNo_selected);

                                            Console.WriteLine("Which component do you want to edit?");
                                            Console.WriteLine("1. Name: " + accountToDisplay.Name);
                                            Console.WriteLine("2. Account Number: " + accountToDisplay.AccNo);
                                            Console.WriteLine("3. UserName: " + accountToDisplay.Username);
                                            Console.WriteLine("4. Password: can be changed in admin menu #6");
                                            Console.WriteLine("5. Account Balance: " + accountToDisplay.AccBalance);
                                            Console.WriteLine("6. Exit");
                                            int edit_options = Convert.ToInt32(Console.ReadLine());
                                            switch(edit_options)
                                            {
                                                case 1:
                                                    Console.WriteLine("New Name: ");
                                                    string enter_newName = Console.ReadLine();
                                                    accountToDisplay.Name = enter_newName;
                                                    db.SaveChanges();
                                                    Console.WriteLine("Name has been changed");
                                                    break;
                                                case 2:
                                                    Console.WriteLine("New Account Number: ");
                                                    int enter_newAccNo = Convert.ToInt32(Console.ReadLine());
                                                    accountToDisplay.AccNo = enter_newAccNo;
                                                    db.SaveChanges();
                                                    Console.WriteLine("Account number has been changed");
                                                    break;
                                                case 3:
                                                    Console.WriteLine("New username: ");
                                                    int enter_newUsername = Convert.ToInt32(Console.ReadLine());
                                                    accountToDisplay.AccNo = enter_newUsername;
                                                    db.SaveChanges();
                                                    Console.WriteLine("Username has been changed");
                                                    break;
                                                case 4: 
                                                    Console.WriteLine("Password Feature can be changed through the admin men on #6");
                                                    break;
                                                case 5:
                                                    break;
                                                case 6:
                                                    Console.WriteLine("Exiting the edit menu...............");
                                                    break;
                                                default:
                                                    Console.WriteLine("Invalid operation");
                                                    break;
                                            }
                                            break;
                                        }


                                    }
                                }
                                break;
                            case 4:
                                using(var db = GetDbContext())
                                {
                                    Console.WriteLine("Which account (account Number) do you want a summary of?");
                                    foreach(var item in db.Accounts)
                                    {
                                        Console.WriteLine("________________________________________________________________________________\n");
                                        Console.WriteLine("account # : " +item.AccNo + " | " + "name : " + item.Name + " | " + "username : " + item.Username + " | " + "balance : $" + item.AccBalance);
                                    }
                                    Console.WriteLine("________________________________________________________________________________");
                                    Console.WriteLine("End of Summary");
                                }

                                break;
                            case 5:
                                using(var db = GetDbContext())
                                {
                                    Console.WriteLine("Customer accounts:");
                                    foreach (var item in db.Accounts)
                                    {
                                        Console.WriteLine(item.Name + ":" + item.AccNo);
                                    }
                                    Console.WriteLine("Which account (account Number) do you reset the password for?");
                                    string accPassReset = Console.ReadLine();
                                    if (!int.TryParse(accPassReset, out int reset_Pass))
                                    {
                                        Console.WriteLine("Please enter a valid account number.");
                                        break;
                                    }

                                    var accountToResetPassword = db.Accounts.FirstOrDefault(account => account.AccNo == reset_Pass);

                                    if (accountToResetPassword is null)
                                    {
                                        Console.WriteLine("Account not found.");
                                        break;
                                    }

                                    Console.WriteLine("Enter the customer's current password");
                                    string oldPassword = PasswordReader.ReadPassword();;
                                    if(string.Equals(oldPassword, accountToResetPassword.Password, StringComparison.Ordinal))
                                    {
                                        Console.WriteLine("Enter your new password");
                                        string newPassword = PasswordReader.ReadPassword();
                                        if(oldPassword == newPassword)
                                        {
                                            Console.WriteLine("The password is the same as previous. No change has been made");
                                        }
                                        else
                                        {
                                            accountToResetPassword.Password = newPassword;
                                            db.SaveChanges();
                                            Console.WriteLine("Updated password success!");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Invalid old password");
                                    }
                                }
                                break;
                            case 6:
                                using (var db = GetDbContext())
                                {
                                    var pendingAccounts = db.Accounts
                                        .Where(a => a.ChequeBookStatus == ChequeBookStatus.Pending)
                                        .ToList();

                                    if (!pendingAccounts.Any())
                                    {
                                        Console.WriteLine("There are no pending cheque book requests.");
                                        break;
                                    }

                                    Console.WriteLine("Pending Cheque Book Requests:");

                                    foreach (var account in pendingAccounts)
                                    {
                                        Console.WriteLine(
                                            $"Name: {account.Name} | Account Number: {account.AccNo}"
                                        );
                                    }

                                    Console.Write("Enter the account number to approve: ");

                                    if (int.TryParse(Console.ReadLine(), out int accountNumber))
                                    {
                                        var accountToApprove = db.Accounts
                                            .FirstOrDefault(a =>
                                                a.AccNo == accountNumber &&
                                                a.ChequeBookStatus == ChequeBookStatus.Pending);

                                        if (accountToApprove != null)
                                        {
                                            accountToApprove.ChequeBookStatus =
                                                ChequeBookStatus.Approved;

                                            db.SaveChanges();

                                            Console.WriteLine("Cheque book request approved.");
                                        }
                                        else
                                        {
                                            Console.WriteLine("Pending request not found.");
                                        }
                                    }
                                }
                                if (onadminMenu)
                                {
                                    Console.WriteLine("Admin session expired or is no longer valid. Please log in again.");
                                }

                                break;
                            case 7:
                                onadminMenu = false;
                                Console.WriteLine("Exiting Admin Account............");
                                break;
                            default:
                                Console.WriteLine("Invalid Choice");
                                break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Invalid Credential");
                }
            }
            else
            {
                Console.WriteLine("Invalid Credential");
            }
            break;
        #endregion
        #region Exit Application Logic
        case 3:
            onMainMenu = false;
            Console.WriteLine("Exiting Application..............");
            break;
        default:
            Console.WriteLine("Invalid Choice");
            break;
        #endregion
    }

}
static int ReadMenuChoice()
{
    return int.TryParse(Console.ReadLine(), out int choice) ? choice : -1;
}
static bool IsTokenAuthorized(JwtTokenService tokenService, string token, string requiredRole)
{
    try
    {
        return tokenService.ValidateToken(token).IsInRole(requiredRole);
    }
    catch (SecurityTokenException)
    {
        return false;
    }
}
#endregion