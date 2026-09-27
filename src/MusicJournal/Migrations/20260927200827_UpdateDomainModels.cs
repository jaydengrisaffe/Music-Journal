using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicJournal.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDomainModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GlobalTracks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Artist = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Album = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Rank = table.Column<int>(type: "INTEGER", nullable: true),
                    SpotifyUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SpotifyId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AlbumArtUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalTracks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfileViewModel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileViewModel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FavoriteTracks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Artist = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Album = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SpotifyUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SpotifyId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    GlobalTrackId = table.Column<int>(type: "INTEGER", nullable: false),
                    AlbumArtUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProfileViewModelId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoriteTracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FavoriteTracks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FavoriteTracks_GlobalTracks_GlobalTrackId",
                        column: x => x.GlobalTrackId,
                        principalTable: "GlobalTracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FavoriteTracks_ProfileViewModel_ProfileViewModelId",
                        column: x => x.ProfileViewModelId,
                        principalTable: "ProfileViewModel",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteTracks_GlobalTrackId",
                table: "FavoriteTracks",
                column: "GlobalTrackId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteTracks_ProfileViewModelId",
                table: "FavoriteTracks",
                column: "ProfileViewModelId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteTracks_UserId",
                table: "FavoriteTracks",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FavoriteTracks");

            migrationBuilder.DropTable(
                name: "GlobalTracks");

            migrationBuilder.DropTable(
                name: "ProfileViewModel");
        }
    }
}
