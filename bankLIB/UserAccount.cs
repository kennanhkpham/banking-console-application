using System.Data.Common;
using System.ComponentModel.DataAnnotations.Schema;
namespace bankLIB;

public class UserAccount
{
    [Column("username")]
    public string username{get;set;} = "";
    [Column("password")]
    public string password{get;set;} = "";

    [Column("accNo")]
    public int accNo{get;set;}
    [Column("name")]
    public string name{get;set;} = "";
    [Column("accBalance")]
    public int accBalance{get;set;}
    [NotMapped]
    public List<string> Transactions { get; set; } = new List<string>();

    public ChequeBookStatus ChequeBookStatus { get; set; } = ChequeBookStatus.None;

    public int Withdraw(int amount)
    {
        accBalance = accBalance - amount;

        Transactions.Add($"Withdrawal: -${amount}, Balance: ${accBalance}"
        );

        return accBalance;
    }
    public int Deposit(int amount)
    {
        accBalance = accBalance + amount;

        Transactions.Add($"Deposit: +${amount}, Balance: ${accBalance}"
        );
        return accBalance;
    }

    public IEnumerable<string> GetLastFiveTransactions()
    {
        return Transactions.TakeLast(5);
    }

    public void Transfer(int amount)
    {
        Transactions.Add($"Transfer: +${amount}, Balance: ${accBalance}");
    }

    public static void RequestChequeBook(UserAccount acc)
    {
        
    }

    public static void VerifyRequest(UserAccount acc)
    {
        
    }
}


