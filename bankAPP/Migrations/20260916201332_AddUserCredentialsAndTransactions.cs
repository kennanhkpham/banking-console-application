using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bankAPP.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCredentialsAndTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    accNo = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    accBalance = table.Column<int>(type: "int", nullable: false),
                    Transactions = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__accounts__A471970548783782", x => x.accNo);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accounts");
        }
    }
}
