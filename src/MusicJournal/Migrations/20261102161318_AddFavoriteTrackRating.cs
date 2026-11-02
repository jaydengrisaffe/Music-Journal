using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicJournal.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoriteTrackRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "FavoriteTracks",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rating",
                table: "FavoriteTracks");
        }
    }
}
