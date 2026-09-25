using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using bankLIB;

namespace bankAPP.DB;

[Table("UserAccounts")]
public partial class Account
{
    [Key]
    [Column("accNo")]
    public int AccNo { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("accBalance")]
    public int AccBalance { get; set; }

    [Column("username")] // Added Username mapping
    [Required]
    public string Username { get; set; } = "";

    [Column("password")] // Added Password mapping
    [Required]
    public string Password { get; set; } = "";
    [NotMapped]
    public List<string> Transactions { get; set; } = new List<string>();


    public ChequeBookStatus ChequeBookStatus { get; set; } = ChequeBookStatus.None;


    // -----METHODS-----------

    public int Withdraw(int amount)
    {
        AccBalance = AccBalance - amount;

        Transactions.Add($"Withdrawal: -${amount}, Balance: ${AccBalance}"
        );

        return AccBalance;
    }
    public int Deposit(int amount)
    {
        AccBalance = AccBalance + amount;

        Transactions.Add($"Deposit: +${amount}, Balance: ${AccBalance}"
        );
        return AccBalance;
    }

    public void Transfer(int amount,string recipient)
    {
        Transactions.Add($"Transfer: -${amount}, Balance: ${AccBalance} Recipient: {recipient}");
    }

    public IEnumerable<string> GetLastFiveTransactions()
    {
        return Transactions.TakeLast(5);
    }

    
    public void RequestChequeBook(Account acc)
    {
        if (acc.ChequeBookStatus == ChequeBookStatus.None)
        {
            acc.ChequeBookStatus = ChequeBookStatus.Pending;

            Console.WriteLine("Account Number: " + acc.AccNo);
            Console.WriteLine("Account Name: " + acc.Name);
            Console.WriteLine("Your request for a cheque book has been submitted!");
        }
        else if (acc.ChequeBookStatus == ChequeBookStatus.Pending)
        {
            Console.WriteLine("Your cheque book request is already pending.");
        }
        else if (acc.ChequeBookStatus == ChequeBookStatus.Approved)
        {
            Console.WriteLine("Your cheque book request has already been approved.");
        }
    }

    public static void VerifyRequest(Account acc)
    {
        
    }
}
