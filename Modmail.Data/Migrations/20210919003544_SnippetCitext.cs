using Microsoft.EntityFrameworkCore.Migrations;

namespace Modmail.Data.Migrations
{
    public partial class SnippetCitext : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ModmailSnippets",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ModmailSnippets",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);
        }
    }
}
