using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LibraryManagement.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Authors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Nationality = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Biography = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Publishers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: true),
                    Website = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Publishers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Readers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReaderCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Gender = table.Column<int>(type: "INTEGER", nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ReaderType = table.Column<int>(type: "INTEGER", nullable: false),
                    RegisteredAt = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ExpirationDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    AvatarPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Readers", x => x.Id);
                    table.CheckConstraint("CK_Readers_ExpirationDate", "\"ExpirationDate\" > \"RegisteredAt\"");
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BookCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ISBN = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    PublisherId = table.Column<int>(type: "INTEGER", nullable: false),
                    PublicationYear = table.Column<int>(type: "INTEGER", nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Price = table.Column<long>(type: "INTEGER", nullable: false),
                    CoverImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                    table.CheckConstraint("CK_Books_PageCount", "\"PageCount\" > 0");
                    table.CheckConstraint("CK_Books_Price", "\"Price\" >= 0");
                    table.CheckConstraint("CK_Books_PublicationYear", "\"PublicationYear\" > 0");
                    table.ForeignKey(
                        name: "FK_Books_Publishers_PublisherId",
                        column: x => x.PublisherId,
                        principalTable: "Publishers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmployeeCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Gender = table.Column<int>(type: "INTEGER", nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookAuthors",
                columns: table => new
                {
                    BookId = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthorId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookAuthors", x => new { x.BookId, x.AuthorId });
                    table.ForeignKey(
                        name: "FK_BookAuthors_Authors_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Authors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookAuthors_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookCategories",
                columns: table => new
                {
                    BookId = table.Column<int>(type: "INTEGER", nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookCategories", x => new { x.BookId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_BookCategories_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookCopies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CopyCode = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    BookId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShelfLocation = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ImportedAt = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PhysicalCondition = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookCopies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookCopies_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BorrowSlips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BorrowCode = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ReaderId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    BorrowDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ExpectedReturnDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BorrowSlips", x => x.Id);
                    table.CheckConstraint("CK_BorrowSlips_ExpectedReturnDate", "\"ExpectedReturnDate\" >= \"BorrowDate\"");
                    table.ForeignKey(
                        name: "FK_BorrowSlips_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BorrowSlips_Readers_ReaderId",
                        column: x => x.ReaderId,
                        principalTable: "Readers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UpdatedByEmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemSettings_Employees_UpdatedByEmployeeId",
                        column: x => x.UpdatedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BorrowSlipDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BorrowSlipId = table.Column<int>(type: "INTEGER", nullable: false),
                    BookCopyId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpectedReturnDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ActualReturnDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    RenewalCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BorrowSlipDetails", x => x.Id);
                    table.CheckConstraint("CK_BorrowSlipDetails_RenewalCount", "\"RenewalCount\" >= 0");
                    table.ForeignKey(
                        name: "FK_BorrowSlipDetails_BookCopies_BookCopyId",
                        column: x => x.BookCopyId,
                        principalTable: "BookCopies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BorrowSlipDetails_BorrowSlips_BorrowSlipId",
                        column: x => x.BorrowSlipId,
                        principalTable: "BorrowSlips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Fines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FineCode = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ReaderId = table.Column<int>(type: "INTEGER", nullable: false),
                    BorrowSlipDetailId = table.Column<int>(type: "INTEGER", nullable: false),
                    FineType = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<long>(type: "INTEGER", nullable: false),
                    PaidAmount = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedByEmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fines", x => x.Id);
                    table.CheckConstraint("CK_Fines_Amount", "\"Amount\" >= 0");
                    table.CheckConstraint("CK_Fines_PaidAmount", "\"PaidAmount\" >= 0");
                    table.CheckConstraint("CK_Fines_PaidAmountNotGreaterThanAmount", "\"PaidAmount\" <= \"Amount\"");
                    table.ForeignKey(
                        name: "FK_Fines_BorrowSlipDetails_BorrowSlipDetailId",
                        column: x => x.BorrowSlipDetailId,
                        principalTable: "BorrowSlipDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Fines_Employees_CreatedByEmployeeId",
                        column: x => x.CreatedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Fines_Readers_ReaderId",
                        column: x => x.ReaderId,
                        principalTable: "Readers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReturnRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BorrowSlipDetailId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReturnDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ReturnedCondition = table.Column<int>(type: "INTEGER", nullable: false),
                    OverdueDays = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnRecords", x => x.Id);
                    table.CheckConstraint("CK_ReturnRecords_OverdueDays", "\"OverdueDays\" >= 0");
                    table.ForeignKey(
                        name: "FK_ReturnRecords_BorrowSlipDetails_BorrowSlipDetailId",
                        column: x => x.BorrowSlipDetailId,
                        principalTable: "BorrowSlipDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnRecords_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinePayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FineId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<long>(type: "INTEGER", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PaymentMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinePayments", x => x.Id);
                    table.CheckConstraint("CK_FinePayments_Amount", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_FinePayments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinePayments_Fines_FineId",
                        column: x => x.FineId,
                        principalTable: "Fines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Authors",
                columns: new[] { "Id", "Biography", "CreatedAt", "DateOfBirth", "FullName", "IsActive", "Nationality", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Nguyễn Nhật Ánh", true, "Việt Nam", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Nam Cao", true, "Việt Nam", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Robert C. Martin", true, "Hoa Kỳ", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Yuval Noah Harari", true, "Israel", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Dale Carnegie", true, "Hoa Kỳ", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Tiểu thuyết, truyện ngắn và thơ", true, "Văn học", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Khoa học tự nhiên và ứng dụng", true, "Khoa học", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Công nghệ thông tin và kỹ thuật", true, "Công nghệ", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Lịch sử Việt Nam và thế giới", true, "Lịch sử", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Phát triển bản thân và kỹ năng", true, "Kỹ năng sống", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Publishers",
                columns: new[] { "Id", "Address", "CreatedAt", "Email", "IsActive", "Name", "PhoneNumber", "UpdatedAt", "Website" },
                values: new object[,]
                {
                    { 1, "Thành phố Hồ Chí Minh", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Nhà xuất bản Trẻ", null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 2, "Hà Nội", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Nhà xuất bản Kim Đồng", null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 3, "Thành phố Hồ Chí Minh", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Nhà xuất bản Tổng hợp TP.HCM", null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.InsertData(
                table: "Readers",
                columns: new[] { "Id", "Address", "AvatarPath", "CreatedAt", "DateOfBirth", "Email", "ExpirationDate", "FullName", "Gender", "Notes", "PhoneNumber", "ReaderCode", "ReaderType", "RegisteredAt", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Thành phố Hồ Chí Minh", null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(1996, 1, 10), "reader1@example.com", new DateOnly(2027, 1, 15), "Lê Hoàng Nam", 1, null, "0912000001", "DG0001", 1, new DateOnly(2026, 1, 15), 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "Thành phố Hồ Chí Minh", null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(1997, 2, 10), "reader2@example.com", new DateOnly(2027, 1, 15), "Phạm Thu Hà", 2, null, "0912000002", "DG0002", 1, new DateOnly(2026, 1, 15), 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "Thành phố Hồ Chí Minh", null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(1998, 3, 10), "reader3@example.com", new DateOnly(2027, 1, 15), "Đỗ Minh Khang", 1, null, "0912000003", "DG0003", 3, new DateOnly(2026, 1, 15), 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "Thành phố Hồ Chí Minh", null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(1999, 4, 10), "reader4@example.com", new DateOnly(2027, 1, 15), "Nguyễn Bảo Ngọc", 2, null, "0912000004", "DG0004", 1, new DateOnly(2026, 1, 15), 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, "Thành phố Hồ Chí Minh", null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2000, 5, 10), "reader5@example.com", new DateOnly(2027, 1, 15), "Trần Gia Huy", 1, null, "0912000005", "DG0005", 1, new DateOnly(2026, 1, 15), 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, "Thành phố Hồ Chí Minh", null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2001, 6, 10), "reader6@example.com", new DateOnly(2027, 1, 15), "Vũ Khánh Linh", 2, null, "0912000006", "DG0006", 2, new DateOnly(2026, 1, 15), 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, "Thành phố Hồ Chí Minh", null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2002, 7, 10), "reader7@example.com", new DateOnly(2027, 1, 15), "Hoàng Anh Tuấn", 1, null, "0912000007", "DG0007", 3, new DateOnly(2026, 1, 15), 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, "Thành phố Hồ Chí Minh", null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2003, 8, 10), "reader8@example.com", new DateOnly(2027, 1, 15), "Bùi Mai Chi", 2, null, "0912000008", "DG0008", 1, new DateOnly(2026, 1, 15), 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9, "Thành phố Hồ Chí Minh", null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2004, 9, 10), "reader9@example.com", new DateOnly(2027, 1, 15), "Đặng Đức Anh", 1, null, "0912000009", "DG0009", 4, new DateOnly(2026, 1, 15), 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 10, "Thành phố Hồ Chí Minh", null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2005, 10, 10), "reader10@example.com", new DateOnly(2027, 1, 15), "Phan Ngọc Lan", 2, null, "0912000010", "DG0010", 3, new DateOnly(2026, 1, 15), 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Quản trị viên hệ thống", true, "Administrator", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nhân viên thư viện", true, "Librarian", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Books",
                columns: new[] { "Id", "BookCode", "CoverImagePath", "CreatedAt", "Description", "ISBN", "IsActive", "Language", "PageCount", "Price", "PublicationYear", "PublisherId", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "S0001", null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "9786041123451", true, "Tiếng Việt", 300, 11000000L, 2019, 1, "Mắt biếc", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "S0002", null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "9786041123452", true, "Tiếng Việt", 220, 9000000L, 2020, 1, "Cho tôi xin một vé đi tuổi thơ", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "S0003", null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "9786041123453", true, "Tiếng Việt", 180, 6500000L, 2018, 2, "Chí Phèo", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "S0004", null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "9786041123454", true, "English", 464, 32000000L, 2021, 3, "Clean Code", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, "S0005", null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "9786041123455", true, "English", 432, 35000000L, 2022, 3, "Clean Architecture", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, "S0006", null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "9786041123456", true, "Tiếng Việt", 512, 25000000L, 2020, 3, "Sapiens", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, "S0007", null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "9786041123457", true, "Tiếng Việt", 480, 26000000L, 2021, 3, "Homo Deus", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, "S0008", null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "9786041123458", true, "Tiếng Việt", 320, 12000000L, 2022, 1, "Đắc nhân tâm", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9, "S0009", null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "9786041123459", true, "Tiếng Việt", 400, 18000000L, 2019, 3, "Lược sử Việt Nam", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 10, "S0010", null, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "9786041123460", true, "Tiếng Việt", 380, 28000000L, 2024, 3, "Nhập môn khoa học dữ liệu", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "Address", "CreatedAt", "DateOfBirth", "Email", "EmployeeCode", "FullName", "Gender", "IsActive", "LastLoginAt", "PasswordHash", "PhoneNumber", "RoleId", "UpdatedAt", "Username" },
                values: new object[,]
                {
                    { 1, "Thành phố Hồ Chí Minh", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(1990, 1, 15), "admin@library.local", "NV0001", "Quản trị hệ thống", 0, true, null, "$2a$12$PIo15XwwVaJM3R6rcweNauhTHdvEVyxL1dYHEfm4Iu.wyFTSGrDcq", "0901000001", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin" },
                    { 2, "Thành phố Hồ Chí Minh", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(1995, 3, 20), "minhanh@library.local", "NV0002", "Nguyễn Minh Anh", 2, true, null, "$2a$12$2plWRQiBcw43230QskNoz.vAwMNgE6JQnEy/PV6TdlyuODmburmS2", "0901000002", 2, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), "librarian1" },
                    { 3, "Thành phố Hồ Chí Minh", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(1993, 8, 12), "quocbao@library.local", "NV0003", "Trần Quốc Bảo", 1, true, null, "$2a$12$2plWRQiBcw43230QskNoz.vAwMNgE6JQnEy/PV6TdlyuODmburmS2", "0901000003", 2, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), "librarian2" }
                });

            migrationBuilder.InsertData(
                table: "ActivityLogs",
                columns: new[] { "Id", "Action", "CreatedAt", "Description", "EmployeeId", "EntityId", "EntityName" },
                values: new object[,]
                {
                    { 1, "DatabaseInitialized", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Khởi tạo dữ liệu mẫu của hệ thống.", 1, null, "Database" },
                    { 2, "BorrowCreated", new DateTime(2026, 7, 22, 8, 0, 0, 0, DateTimeKind.Utc), "Tạo phiếu mượn PM202607-001.", 2, "1", "BorrowSlip" },
                    { 3, "BookReturned", new DateTime(2026, 6, 17, 9, 30, 0, 0, DateTimeKind.Utc), "Xử lý trả sách cho phiếu PM202606-001.", 3, "1", "ReturnRecord" }
                });

            migrationBuilder.InsertData(
                table: "BookAuthors",
                columns: new[] { "AuthorId", "BookId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 1, 2 },
                    { 2, 3 },
                    { 3, 4 },
                    { 3, 5 },
                    { 4, 6 },
                    { 4, 7 },
                    { 5, 8 },
                    { 4, 9 },
                    { 3, 10 }
                });

            migrationBuilder.InsertData(
                table: "BookCategories",
                columns: new[] { "BookId", "CategoryId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 2, 1 },
                    { 2, 5 },
                    { 3, 1 },
                    { 4, 3 },
                    { 5, 3 },
                    { 6, 2 },
                    { 6, 4 },
                    { 7, 2 },
                    { 8, 5 },
                    { 9, 4 },
                    { 10, 2 },
                    { 10, 3 }
                });

            migrationBuilder.InsertData(
                table: "BookCopies",
                columns: new[] { "Id", "BookId", "CopyCode", "CreatedAt", "ImportedAt", "Notes", "PhysicalCondition", "ShelfLocation", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 1, "BS001-01", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 1, 11), null, 2, "Kệ 1-01", 2, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 1, "BS001-02", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 1, 12), null, 2, "Kệ 1-01", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, 2, "BS002-01", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 1, 13), null, 2, "Kệ 1-02", 2, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, 2, "BS002-02", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 1, 14), null, 2, "Kệ 1-02", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, 2, "BS002-03", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 1, 15), null, 2, "Kệ 1-02", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, 3, "BS003-01", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 1, 16), null, 2, "Kệ 1-03", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, 4, "BS004-01", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 1, 17), null, 2, "Kệ 2-04", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, 4, "BS004-02", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 1, 18), null, 2, "Kệ 2-04", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9, 4, "BS004-03", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 1, 19), null, 2, "Kệ 2-04", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 10, 4, "BS004-04", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 1, 20), null, 2, "Kệ 2-04", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 11, 5, "BS005-01", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 1, 21), null, 2, "Kệ 2-05", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 12, 5, "BS005-02", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 1, 22), null, 2, "Kệ 2-05", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 13, 6, "BS006-01", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 1, 23), null, 2, "Kệ 2-06", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 14, 6, "BS006-02", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 1, 24), null, 2, "Kệ 2-06", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 15, 6, "BS006-03", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 1, 25), null, 2, "Kệ 2-06", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 16, 6, "BS006-04", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 1, 26), null, 2, "Kệ 2-06", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 17, 6, "BS006-05", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 1, 27), null, 2, "Kệ 2-06", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 18, 7, "BS007-01", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 1, 28), null, 2, "Kệ 3-07", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 19, 8, "BS008-01", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 1, 29), null, 2, "Kệ 3-08", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 20, 8, "BS008-02", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 1, 30), null, 2, "Kệ 3-08", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 21, 8, "BS008-03", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 1, 31), null, 2, "Kệ 3-08", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 22, 9, "BS009-01", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 2, 1), null, 2, "Kệ 3-09", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 23, 9, "BS009-02", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 2, 2), null, 2, "Kệ 3-09", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 24, 10, "BS010-01", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 2, 3), null, 2, "Kệ 4-10", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 25, 10, "BS010-02", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 2, 4), null, 2, "Kệ 4-10", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 26, 10, "BS010-03", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 2, 5), null, 2, "Kệ 4-10", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 27, 10, "BS010-04", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 2, 6), null, 2, "Kệ 4-10", 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "BorrowSlips",
                columns: new[] { "Id", "BorrowCode", "BorrowDate", "CreatedAt", "EmployeeId", "ExpectedReturnDate", "Notes", "ReaderId", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "PM202607-001", new DateOnly(2026, 7, 22), new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Utc), 2, new DateOnly(2026, 8, 5), null, 1, 1, new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "PM202607-002", new DateOnly(2026, 6, 26), new DateTime(2026, 6, 26, 0, 0, 0, 0, DateTimeKind.Utc), 2, new DateOnly(2026, 7, 10), null, 2, 4, new DateTime(2026, 6, 26, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "PM202606-001", new DateOnly(2026, 6, 1), new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, new DateOnly(2026, 6, 15), null, 3, 3, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "Description", "Key", "UpdatedAt", "UpdatedByEmployeeId", "Value" },
                values: new object[,]
                {
                    { 1, "Số bản sách được mượn tối đa", "MaximumBorrowedBooks", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "5" },
                    { 2, "Số ngày mượn mặc định", "DefaultBorrowDays", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "14" },
                    { 3, "Số lần gia hạn tối đa", "MaximumRenewalCount", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "2" },
                    { 4, "Số ngày cho mỗi lần gia hạn", "RenewalDays", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "7" },
                    { 5, "Mức phạt quá hạn mỗi ngày", "OverdueFinePerDay", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "5000" },
                    { 6, "Hệ số phạt mất sách", "LostBookFineMultiplier", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "2.0" },
                    { 7, "Hệ số phạt hư hỏng", "DamagedBookFineMultiplier", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "0.5" },
                    { 8, "Thời hạn thẻ độc giả theo tháng", "ReaderCardValidityMonths", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "12" }
                });

            migrationBuilder.InsertData(
                table: "BorrowSlipDetails",
                columns: new[] { "Id", "ActualReturnDate", "BookCopyId", "BorrowSlipId", "CreatedAt", "ExpectedReturnDate", "Notes", "RenewalCount", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, null, 1, 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 8, 5), null, 0, 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, null, 3, 2, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 7, 10), null, 0, 3, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, new DateOnly(2026, 6, 17), 7, 3, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 6, 15), null, 0, 2, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Fines",
                columns: new[] { "Id", "Amount", "BorrowSlipDetailId", "CreatedAt", "CreatedByEmployeeId", "FineCode", "FineType", "PaidAmount", "ReaderId", "Reason", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 9000000L, 2, new DateTime(2026, 7, 28, 0, 0, 0, 0, DateTimeKind.Utc), 2, "TP202607-001", 1, 0L, 2, "Quá hạn 18 ngày tính đến ngày tạo dữ liệu mẫu.", 1, new DateTime(2026, 7, 28, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 1000000L, 3, new DateTime(2026, 6, 17, 9, 30, 0, 0, DateTimeKind.Utc), 3, "TP202606-001", 1, 500000L, 3, "Trả sách quá hạn 2 ngày.", 2, new DateTime(2026, 6, 17, 9, 35, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "ReturnRecords",
                columns: new[] { "Id", "BorrowSlipDetailId", "CreatedAt", "EmployeeId", "Notes", "OverdueDays", "ReturnDate", "ReturnedCondition" },
                values: new object[] { 1, 3, new DateTime(2026, 6, 17, 9, 30, 0, 0, DateTimeKind.Utc), 3, "Sách được trả trong tình trạng tốt.", 2, new DateOnly(2026, 6, 17), 2 });

            migrationBuilder.InsertData(
                table: "FinePayments",
                columns: new[] { "Id", "Amount", "CreatedAt", "EmployeeId", "FineId", "Notes", "PaymentDate", "PaymentMethod" },
                values: new object[] { 1, 500000L, new DateTime(2026, 6, 17, 9, 35, 0, 0, DateTimeKind.Utc), 3, 2, "Thanh toán một phần.", new DateTime(2026, 6, 17, 9, 35, 0, 0, DateTimeKind.Utc), 1 });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_CreatedAt",
                table: "ActivityLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_EmployeeId_Action",
                table: "ActivityLogs",
                columns: new[] { "EmployeeId", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_Authors_FullName",
                table: "Authors",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_BookAuthors_AuthorId",
                table: "BookAuthors",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_BookCategories_CategoryId",
                table: "BookCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BookCopies_BookId_Status",
                table: "BookCopies",
                columns: new[] { "BookId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BookCopies_CopyCode",
                table: "BookCopies",
                column: "CopyCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Books_BookCode",
                table: "Books",
                column: "BookCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Books_ISBN",
                table: "Books",
                column: "ISBN",
                unique: true,
                filter: "\"ISBN\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Books_PublisherId",
                table: "Books",
                column: "PublisherId");

            migrationBuilder.CreateIndex(
                name: "IX_Books_Title",
                table: "Books",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowSlipDetails_BookCopyId_Status",
                table: "BorrowSlipDetails",
                columns: new[] { "BookCopyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BorrowSlipDetails_BorrowSlipId_BookCopyId",
                table: "BorrowSlipDetails",
                columns: new[] { "BorrowSlipId", "BookCopyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BorrowSlipDetails_ExpectedReturnDate",
                table: "BorrowSlipDetails",
                column: "ExpectedReturnDate");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowSlips_BorrowCode",
                table: "BorrowSlips",
                column: "BorrowCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BorrowSlips_BorrowDate",
                table: "BorrowSlips",
                column: "BorrowDate");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowSlips_EmployeeId",
                table: "BorrowSlips",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowSlips_ReaderId_Status",
                table: "BorrowSlips",
                columns: new[] { "ReaderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email",
                table: "Employees",
                column: "Email",
                unique: true,
                filter: "\"Email\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeCode",
                table: "Employees",
                column: "EmployeeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_RoleId",
                table: "Employees",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Username",
                table: "Employees",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinePayments_EmployeeId",
                table: "FinePayments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_FinePayments_FineId_PaymentDate",
                table: "FinePayments",
                columns: new[] { "FineId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Fines_BorrowSlipDetailId",
                table: "Fines",
                column: "BorrowSlipDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_Fines_CreatedByEmployeeId",
                table: "Fines",
                column: "CreatedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Fines_FineCode",
                table: "Fines",
                column: "FineCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fines_ReaderId_Status",
                table: "Fines",
                columns: new[] { "ReaderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Publishers_Name",
                table: "Publishers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Readers_Email",
                table: "Readers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Readers_ReaderCode",
                table: "Readers",
                column: "ReaderCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Readers_Status",
                table: "Readers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRecords_BorrowSlipDetailId",
                table: "ReturnRecords",
                column: "BorrowSlipDetailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRecords_EmployeeId",
                table: "ReturnRecords",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRecords_ReturnDate",
                table: "ReturnRecords",
                column: "ReturnDate");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_UpdatedByEmployeeId",
                table: "SystemSettings",
                column: "UpdatedByEmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "BookAuthors");

            migrationBuilder.DropTable(
                name: "BookCategories");

            migrationBuilder.DropTable(
                name: "FinePayments");

            migrationBuilder.DropTable(
                name: "ReturnRecords");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "Authors");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Fines");

            migrationBuilder.DropTable(
                name: "BorrowSlipDetails");

            migrationBuilder.DropTable(
                name: "BookCopies");

            migrationBuilder.DropTable(
                name: "BorrowSlips");

            migrationBuilder.DropTable(
                name: "Books");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Readers");

            migrationBuilder.DropTable(
                name: "Publishers");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
