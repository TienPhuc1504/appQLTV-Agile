using LibraryManagement.Core.Constants;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Data;

internal static class SeedData
{
    private const string AdministratorPasswordHash =
        "$2a$12$PIo15XwwVaJM3R6rcweNauhTHdvEVyxL1dYHEfm4Iu.wyFTSGrDcq";

    private const string LibrarianPasswordHash =
        "$2a$12$2plWRQiBcw43230QskNoz.vAwMNgE6JQnEy/PV6TdlyuODmburmS2";

    private static readonly DateTime SeedCreatedAt =
        new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    public static void Apply(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        SeedRoles(modelBuilder);
        SeedEmployees(modelBuilder);
        SeedCategories(modelBuilder);
        SeedAuthors(modelBuilder);
        SeedPublishers(modelBuilder);
        SeedBooks(modelBuilder);
        SeedBookRelationships(modelBuilder);
        SeedBookCopies(modelBuilder);
        SeedReaders(modelBuilder);
        SeedBorrowingData(modelBuilder);
        SeedSystemSettings(modelBuilder);
        SeedActivityLogs(modelBuilder);
    }

    private static void SeedRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role
            {
                Id = 1,
                Name = "Administrator",
                Description = "Quản trị viên hệ thống",
                IsActive = true,
                CreatedAt = SeedCreatedAt,
                UpdatedAt = SeedCreatedAt
            },
            new Role
            {
                Id = 2,
                Name = "Librarian",
                Description = "Nhân viên thư viện",
                IsActive = true,
                CreatedAt = SeedCreatedAt,
                UpdatedAt = SeedCreatedAt
            });
    }

    private static void SeedEmployees(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>().HasData(
            new Employee
            {
                Id = 1,
                EmployeeCode = "NV0001",
                FullName = "Quản trị hệ thống",
                DateOfBirth = new DateOnly(1990, 1, 15),
                Gender = Gender.Other,
                PhoneNumber = "0901000001",
                Email = "admin@library.local",
                Address = "Thành phố Hồ Chí Minh",
                Username = "admin",
                PasswordHash = AdministratorPasswordHash,
                RoleId = 1,
                IsActive = true,
                CreatedAt = SeedCreatedAt,
                UpdatedAt = SeedCreatedAt
            },
            new Employee
            {
                Id = 2,
                EmployeeCode = "NV0002",
                FullName = "Nguyễn Minh Anh",
                DateOfBirth = new DateOnly(1995, 3, 20),
                Gender = Gender.Female,
                PhoneNumber = "0901000002",
                Email = "minhanh@library.local",
                Address = "Thành phố Hồ Chí Minh",
                Username = "librarian1",
                PasswordHash = LibrarianPasswordHash,
                RoleId = 2,
                IsActive = true,
                CreatedAt = SeedCreatedAt,
                UpdatedAt = SeedCreatedAt
            },
            new Employee
            {
                Id = 3,
                EmployeeCode = "NV0003",
                FullName = "Trần Quốc Bảo",
                DateOfBirth = new DateOnly(1993, 8, 12),
                Gender = Gender.Male,
                PhoneNumber = "0901000003",
                Email = "quocbao@library.local",
                Address = "Thành phố Hồ Chí Minh",
                Username = "librarian2",
                PasswordHash = LibrarianPasswordHash,
                RoleId = 2,
                IsActive = true,
                CreatedAt = SeedCreatedAt,
                UpdatedAt = SeedCreatedAt
            });
    }

    private static void SeedCategories(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasData(
            CreateCategory(1, "Văn học", "Tiểu thuyết, truyện ngắn và thơ"),
            CreateCategory(2, "Khoa học", "Khoa học tự nhiên và ứng dụng"),
            CreateCategory(3, "Công nghệ", "Công nghệ thông tin và kỹ thuật"),
            CreateCategory(4, "Lịch sử", "Lịch sử Việt Nam và thế giới"),
            CreateCategory(5, "Kỹ năng sống", "Phát triển bản thân và kỹ năng"));
    }

    private static void SeedAuthors(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Author>().HasData(
            CreateAuthor(1, "Nguyễn Nhật Ánh", "Việt Nam"),
            CreateAuthor(2, "Nam Cao", "Việt Nam"),
            CreateAuthor(3, "Robert C. Martin", "Hoa Kỳ"),
            CreateAuthor(4, "Yuval Noah Harari", "Israel"),
            CreateAuthor(5, "Dale Carnegie", "Hoa Kỳ"));
    }

    private static void SeedPublishers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Publisher>().HasData(
            CreatePublisher(1, "Nhà xuất bản Trẻ", "Thành phố Hồ Chí Minh"),
            CreatePublisher(2, "Nhà xuất bản Kim Đồng", "Hà Nội"),
            CreatePublisher(3, "Nhà xuất bản Tổng hợp TP.HCM", "Thành phố Hồ Chí Minh"));
    }

    private static void SeedBooks(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>().HasData(
            CreateBook(1, "S0001", "9786041123451", "Mắt biếc", 1, 2019, 300, 110000m),
            CreateBook(2, "S0002", "9786041123452", "Cho tôi xin một vé đi tuổi thơ", 1, 2020, 220, 90000m),
            CreateBook(3, "S0003", "9786041123453", "Chí Phèo", 2, 2018, 180, 65000m),
            CreateBook(4, "S0004", "9786041123454", "Clean Code", 3, 2021, 464, 320000m),
            CreateBook(5, "S0005", "9786041123455", "Clean Architecture", 3, 2022, 432, 350000m),
            CreateBook(6, "S0006", "9786041123456", "Sapiens", 3, 2020, 512, 250000m),
            CreateBook(7, "S0007", "9786041123457", "Homo Deus", 3, 2021, 480, 260000m),
            CreateBook(8, "S0008", "9786041123458", "Đắc nhân tâm", 1, 2022, 320, 120000m),
            CreateBook(9, "S0009", "9786041123459", "Lược sử Việt Nam", 3, 2019, 400, 180000m),
            CreateBook(10, "S0010", "9786041123460", "Nhập môn khoa học dữ liệu", 3, 2024, 380, 280000m));
    }

    private static void SeedBookRelationships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookAuthor>().HasData(
            new BookAuthor { BookId = 1, AuthorId = 1 },
            new BookAuthor { BookId = 2, AuthorId = 1 },
            new BookAuthor { BookId = 3, AuthorId = 2 },
            new BookAuthor { BookId = 4, AuthorId = 3 },
            new BookAuthor { BookId = 5, AuthorId = 3 },
            new BookAuthor { BookId = 6, AuthorId = 4 },
            new BookAuthor { BookId = 7, AuthorId = 4 },
            new BookAuthor { BookId = 8, AuthorId = 5 },
            new BookAuthor { BookId = 9, AuthorId = 4 },
            new BookAuthor { BookId = 10, AuthorId = 3 });

        modelBuilder.Entity<BookCategory>().HasData(
            new BookCategory { BookId = 1, CategoryId = 1 },
            new BookCategory { BookId = 2, CategoryId = 1 },
            new BookCategory { BookId = 2, CategoryId = 5 },
            new BookCategory { BookId = 3, CategoryId = 1 },
            new BookCategory { BookId = 4, CategoryId = 3 },
            new BookCategory { BookId = 5, CategoryId = 3 },
            new BookCategory { BookId = 6, CategoryId = 2 },
            new BookCategory { BookId = 6, CategoryId = 4 },
            new BookCategory { BookId = 7, CategoryId = 2 },
            new BookCategory { BookId = 8, CategoryId = 5 },
            new BookCategory { BookId = 9, CategoryId = 4 },
            new BookCategory { BookId = 10, CategoryId = 2 },
            new BookCategory { BookId = 10, CategoryId = 3 });
    }

    private static void SeedBookCopies(ModelBuilder modelBuilder)
    {
        int[] copyCounts = [2, 3, 1, 4, 2, 5, 1, 3, 2, 4];
        var bookCopies = new List<BookCopy>();
        int copyId = 1;

        for (int bookIndex = 0; bookIndex < copyCounts.Length; bookIndex++)
        {
            int bookId = bookIndex + 1;
            for (int sequence = 1; sequence <= copyCounts[bookIndex]; sequence++)
            {
                BookCopyStatus status = copyId is 1 or 3
                    ? BookCopyStatus.Borrowed
                    : BookCopyStatus.Available;

                bookCopies.Add(new BookCopy
                {
                    Id = copyId,
                    CopyCode = $"BS{bookId:000}-{sequence:00}",
                    BookId = bookId,
                    ShelfLocation = $"Kệ {((bookId - 1) / 3) + 1}-{bookId:00}",
                    ImportedAt = new DateOnly(2026, 1, 10).AddDays(copyId),
                    PhysicalCondition = PhysicalCondition.Good,
                    Status = status,
                    CreatedAt = SeedCreatedAt,
                    UpdatedAt = SeedCreatedAt
                });

                copyId++;
            }
        }

        modelBuilder.Entity<BookCopy>().HasData(bookCopies);
    }

    private static void SeedReaders(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reader>().HasData(
            CreateReader(1, "DG0001", "Lê Hoàng Nam", Gender.Male, ReaderType.Student),
            CreateReader(2, "DG0002", "Phạm Thu Hà", Gender.Female, ReaderType.Student),
            CreateReader(3, "DG0003", "Đỗ Minh Khang", Gender.Male, ReaderType.Adult),
            CreateReader(4, "DG0004", "Nguyễn Bảo Ngọc", Gender.Female, ReaderType.Student),
            CreateReader(5, "DG0005", "Trần Gia Huy", Gender.Male, ReaderType.Student),
            CreateReader(6, "DG0006", "Vũ Khánh Linh", Gender.Female, ReaderType.Lecturer),
            CreateReader(7, "DG0007", "Hoàng Anh Tuấn", Gender.Male, ReaderType.Adult),
            CreateReader(8, "DG0008", "Bùi Mai Chi", Gender.Female, ReaderType.Student),
            CreateReader(9, "DG0009", "Đặng Đức Anh", Gender.Male, ReaderType.Child),
            CreateReader(10, "DG0010", "Phan Ngọc Lan", Gender.Female, ReaderType.Adult));
    }

    private static void SeedBorrowingData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BorrowSlip>().HasData(
            CreateBorrowSlip(
                1,
                "PM202607-001",
                1,
                2,
                new DateOnly(2026, 7, 22),
                new DateOnly(2026, 8, 5),
                BorrowSlipStatus.Active),
            CreateBorrowSlip(
                2,
                "PM202607-002",
                2,
                2,
                new DateOnly(2026, 6, 26),
                new DateOnly(2026, 7, 10),
                BorrowSlipStatus.Overdue),
            CreateBorrowSlip(
                3,
                "PM202606-001",
                3,
                3,
                new DateOnly(2026, 6, 1),
                new DateOnly(2026, 6, 15),
                BorrowSlipStatus.Completed));

        modelBuilder.Entity<BorrowSlipDetail>().HasData(
            CreateBorrowSlipDetail(
                1,
                1,
                1,
                new DateOnly(2026, 8, 5),
                BorrowSlipDetailStatus.Borrowing),
            CreateBorrowSlipDetail(
                2,
                2,
                3,
                new DateOnly(2026, 7, 10),
                BorrowSlipDetailStatus.Overdue),
            CreateBorrowSlipDetail(
                3,
                3,
                7,
                new DateOnly(2026, 6, 15),
                BorrowSlipDetailStatus.Returned,
                new DateOnly(2026, 6, 17)));

        modelBuilder.Entity<ReturnRecord>().HasData(
            new ReturnRecord
            {
                Id = 1,
                BorrowSlipDetailId = 3,
                EmployeeId = 3,
                ReturnDate = new DateOnly(2026, 6, 17),
                ReturnedCondition = PhysicalCondition.Good,
                OverdueDays = 2,
                Notes = "Sách được trả trong tình trạng tốt.",
                CreatedAt = new DateTime(2026, 6, 17, 9, 30, 0, DateTimeKind.Utc)
            });

        modelBuilder.Entity<Fine>().HasData(
            new Fine
            {
                Id = 1,
                FineCode = "TP202607-001",
                ReaderId = 2,
                BorrowSlipDetailId = 2,
                FineType = FineType.Overdue,
                Amount = 90000m,
                PaidAmount = 0m,
                Status = FineStatus.Unpaid,
                Reason = "Quá hạn 18 ngày tính đến ngày tạo dữ liệu mẫu.",
                CreatedByEmployeeId = 2,
                CreatedAt = new DateTime(2026, 7, 28, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 7, 28, 0, 0, 0, DateTimeKind.Utc)
            },
            new Fine
            {
                Id = 2,
                FineCode = "TP202606-001",
                ReaderId = 3,
                BorrowSlipDetailId = 3,
                FineType = FineType.Overdue,
                Amount = 10000m,
                PaidAmount = 5000m,
                Status = FineStatus.PartiallyPaid,
                Reason = "Trả sách quá hạn 2 ngày.",
                CreatedByEmployeeId = 3,
                CreatedAt = new DateTime(2026, 6, 17, 9, 30, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 6, 17, 9, 35, 0, DateTimeKind.Utc)
            });

        modelBuilder.Entity<FinePayment>().HasData(
            new FinePayment
            {
                Id = 1,
                FineId = 2,
                EmployeeId = 3,
                Amount = 5000m,
                PaymentDate = new DateTime(2026, 6, 17, 9, 35, 0, DateTimeKind.Utc),
                PaymentMethod = PaymentMethod.Cash,
                Notes = "Thanh toán một phần.",
                CreatedAt = new DateTime(2026, 6, 17, 9, 35, 0, DateTimeKind.Utc)
            });
    }

    private static void SeedSystemSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SystemSetting>().HasData(
            CreateSetting(
                1,
                SystemSettingKeys.MaximumBorrowedBooks,
                "5",
                "Số bản sách được mượn tối đa"),
            CreateSetting(
                2,
                SystemSettingKeys.DefaultBorrowDays,
                "14",
                "Số ngày mượn mặc định"),
            CreateSetting(
                3,
                SystemSettingKeys.MaximumRenewalCount,
                "2",
                "Số lần gia hạn tối đa"),
            CreateSetting(
                4,
                SystemSettingKeys.RenewalDays,
                "7",
                "Số ngày cho mỗi lần gia hạn"),
            CreateSetting(
                5,
                SystemSettingKeys.OverdueFinePerDay,
                "5000",
                "Mức phạt quá hạn mỗi ngày"),
            CreateSetting(
                6,
                SystemSettingKeys.LostBookFineMultiplier,
                "2.0",
                "Hệ số phạt mất sách"),
            CreateSetting(
                7,
                SystemSettingKeys.DamagedBookFineMultiplier,
                "0.5",
                "Hệ số phạt hư hỏng"),
            CreateSetting(
                8,
                SystemSettingKeys.ReaderCardValidityMonths,
                "12",
                "Thời hạn thẻ độc giả theo tháng"),
            CreateSetting(
                9,
                SystemSettingKeys.MaximumOutstandingFineAmount,
                "0",
                "Tiền phạt chưa thanh toán tối đa vẫn được phép mượn"));
    }

    private static void SeedActivityLogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLog>().HasData(
            new ActivityLog
            {
                Id = 1,
                EmployeeId = 1,
                Action = "DatabaseInitialized",
                EntityName = "Database",
                Description = "Khởi tạo dữ liệu mẫu của hệ thống.",
                CreatedAt = SeedCreatedAt
            },
            new ActivityLog
            {
                Id = 2,
                EmployeeId = 2,
                Action = "BorrowCreated",
                EntityName = nameof(BorrowSlip),
                EntityId = "1",
                Description = "Tạo phiếu mượn PM202607-001.",
                CreatedAt = new DateTime(2026, 7, 22, 8, 0, 0, DateTimeKind.Utc)
            },
            new ActivityLog
            {
                Id = 3,
                EmployeeId = 3,
                Action = "BookReturned",
                EntityName = nameof(ReturnRecord),
                EntityId = "1",
                Description = "Xử lý trả sách cho phiếu PM202606-001.",
                CreatedAt = new DateTime(2026, 6, 17, 9, 30, 0, DateTimeKind.Utc)
            });
    }

    private static Category CreateCategory(int id, string name, string description) =>
        new()
        {
            Id = id,
            Name = name,
            Description = description,
            IsActive = true,
            CreatedAt = SeedCreatedAt,
            UpdatedAt = SeedCreatedAt
        };

    private static Author CreateAuthor(int id, string fullName, string nationality) =>
        new()
        {
            Id = id,
            FullName = fullName,
            Nationality = nationality,
            IsActive = true,
            CreatedAt = SeedCreatedAt,
            UpdatedAt = SeedCreatedAt
        };

    private static Publisher CreatePublisher(int id, string name, string address) =>
        new()
        {
            Id = id,
            Name = name,
            Address = address,
            IsActive = true,
            CreatedAt = SeedCreatedAt,
            UpdatedAt = SeedCreatedAt
        };

    private static Book CreateBook(
        int id,
        string bookCode,
        string isbn,
        string title,
        int publisherId,
        int publicationYear,
        int pageCount,
        decimal price) =>
        new()
        {
            Id = id,
            BookCode = bookCode,
            ISBN = isbn,
            Title = title,
            PublisherId = publisherId,
            PublicationYear = publicationYear,
            Language = id is 4 or 5 ? "English" : "Tiếng Việt",
            PageCount = pageCount,
            Price = price,
            IsActive = true,
            CreatedAt = SeedCreatedAt,
            UpdatedAt = SeedCreatedAt
        };

    private static Reader CreateReader(
        int id,
        string readerCode,
        string fullName,
        Gender gender,
        ReaderType readerType) =>
        new()
        {
            Id = id,
            ReaderCode = readerCode,
            FullName = fullName,
            DateOfBirth = new DateOnly(1995 + id, ((id - 1) % 12) + 1, 10),
            Gender = gender,
            PhoneNumber = $"091200{id:0000}",
            Email = $"reader{id}@example.com",
            Address = "Thành phố Hồ Chí Minh",
            ReaderType = readerType,
            RegisteredAt = new DateOnly(2026, 1, 15),
            ExpirationDate = new DateOnly(2027, 1, 15),
            Status = ReaderStatus.Active,
            CreatedAt = SeedCreatedAt,
            UpdatedAt = SeedCreatedAt
        };

    private static BorrowSlip CreateBorrowSlip(
        int id,
        string borrowCode,
        int readerId,
        int employeeId,
        DateOnly borrowDate,
        DateOnly expectedReturnDate,
        BorrowSlipStatus status) =>
        new()
        {
            Id = id,
            BorrowCode = borrowCode,
            ReaderId = readerId,
            EmployeeId = employeeId,
            BorrowDate = borrowDate,
            ExpectedReturnDate = expectedReturnDate,
            Status = status,
            CreatedAt = borrowDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            UpdatedAt = borrowDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
        };

    private static BorrowSlipDetail CreateBorrowSlipDetail(
        int id,
        int borrowSlipId,
        int bookCopyId,
        DateOnly expectedReturnDate,
        BorrowSlipDetailStatus status,
        DateOnly? actualReturnDate = null) =>
        new()
        {
            Id = id,
            BorrowSlipId = borrowSlipId,
            BookCopyId = bookCopyId,
            ExpectedReturnDate = expectedReturnDate,
            ActualReturnDate = actualReturnDate,
            RenewalCount = 0,
            Status = status,
            CreatedAt = SeedCreatedAt,
            UpdatedAt = SeedCreatedAt
        };

    private static SystemSetting CreateSetting(
        int id,
        string key,
        string value,
        string description) =>
        new()
        {
            Id = id,
            Key = key,
            Value = value,
            Description = description,
            UpdatedByEmployeeId = 1,
            UpdatedAt = SeedCreatedAt
        };
}
