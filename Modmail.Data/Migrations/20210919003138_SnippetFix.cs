using Microsoft.EntityFrameworkCore.Migrations;

namespace Modmail.Data.Migrations
{
    public partial class SnippetFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ModmailSnippets",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "ModmailSnippets");
        }
    }
}
