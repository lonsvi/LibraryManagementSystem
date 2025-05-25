using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace LibraryManagementSystem
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public int Year { get; set; }
        public string CoverUrl { get; set; }
        public string Description { get; set; }
        public string Genre { get; set; }
        public string ISBN { get; set; }
        public bool IsAvailable { get; set; } = true;
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } // "Librarian" или "User"
    }

    public class Subscription
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class Rental
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BookId { get; set; }
        public DateTime RentDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public double Fine { get; set; }
    }

    public static class LibraryDbContext
    {
        private static string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "library.db");
        private static string ConnectionString => $"Data Source={dbPath};Version=3;";

        static LibraryDbContext()
        {
            CreateDatabaseIfNotExists();
        }

        private static void CreateDatabaseIfNotExists()
        {
            try
            {
                if (!File.Exists(dbPath))
                {
                    SQLiteConnection.CreateFile(dbPath);
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Created new database file: {dbPath}\n");
                }

                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Database connection opened\n");
                    using (SQLiteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS Books (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Title TEXT NOT NULL,
                                Author TEXT NOT NULL,
                                Year INTEGER NOT NULL,
                                CoverUrl TEXT,
                                Description TEXT,
                                Genre TEXT,
                                ISBN TEXT,
                                IsAvailable INTEGER NOT NULL DEFAULT 1
                            )";
                        command.ExecuteNonQuery();
                        File.AppendAllText("debug.log", $"{DateTime.Now}: Books table created or already exists\n");

                        command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS Users (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Username TEXT NOT NULL UNIQUE,
                                Password TEXT NOT NULL,
                                Role TEXT NOT NULL
                            )";
                        command.ExecuteNonQuery();
                        File.AppendAllText("debug.log", $"{DateTime.Now}: Users table created or already exists\n");

                        command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS Subscriptions (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                UserId INTEGER NOT NULL,
                                StartDate TEXT NOT NULL,
                                EndDate TEXT NOT NULL,
                                FOREIGN KEY (UserId) REFERENCES Users(Id)
                            )";
                        command.ExecuteNonQuery();
                        File.AppendAllText("debug.log", $"{DateTime.Now}: Subscriptions table created or already exists\n");

                        command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS Rentals (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                UserId INTEGER NOT NULL,
                                BookId INTEGER NOT NULL,
                                RentDate TEXT NOT NULL,
                                DueDate TEXT NOT NULL,
                                ReturnDate TEXT,
                                Fine REAL DEFAULT 0,
                                FOREIGN KEY (UserId) REFERENCES Users(Id),
                                FOREIGN KEY (BookId) REFERENCES Books(Id)
                            )";
                        command.ExecuteNonQuery();
                        File.AppendAllText("debug.log", $"{DateTime.Now}: Rentals table created or already exists\n");

                        command.CommandText = "INSERT OR IGNORE INTO Users (Username, Password, Role) VALUES ('admin', 'admin123', 'Librarian')";
                        command.ExecuteNonQuery();
                        File.AppendAllText("debug.log", $"{DateTime.Now}: Default admin user created or already exists\n");
                    }
                    connection.Close();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Database connection closed\n");
                }

                MigrateDatabase();
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"{DateTime.Now}: Error in CreateDatabaseIfNotExists: {ex.Message}\n");
                throw;
            }
        }

        private static void MigrateDatabase()
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection opened for migration\n");
                    using (SQLiteCommand command = connection.CreateCommand())
                    {
                        // Проверка существования столбцов в таблице Books
                        command.CommandText = "PRAGMA table_info(Books)";
                        var columns = new List<string>();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                columns.Add(reader.GetString(1)); // Имя столбца
                            }
                        }
                        File.AppendAllText("debug.log", $"{DateTime.Now}: Existing columns in Books: {string.Join(", ", columns)}\n");

                        // Добавляем столбцы только если их нет
                        if (!columns.Contains("Genre"))
                        {
                            command.CommandText = "ALTER TABLE Books ADD COLUMN Genre TEXT";
                            command.ExecuteNonQuery();
                            File.AppendAllText("debug.log", $"{DateTime.Now}: Added Genre column\n");
                        }
                        if (!columns.Contains("ISBN"))
                        {
                            command.CommandText = "ALTER TABLE Books ADD COLUMN ISBN TEXT";
                            command.ExecuteNonQuery();
                            File.AppendAllText("debug.log", $"{DateTime.Now}: Added ISBN column\n");
                        }

                        // Проверка столбцов в таблице Rentals
                        command.CommandText = "PRAGMA table_info(Rentals)";
                        columns.Clear();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                columns.Add(reader.GetString(1));
                            }
                        }
                        File.AppendAllText("debug.log", $"{DateTime.Now}: Existing columns in Rentals: {string.Join(", ", columns)}\n");

                        if (!columns.Contains("Fine"))
                        {
                            command.CommandText = "ALTER TABLE Rentals ADD COLUMN Fine REAL DEFAULT 0";
                            command.ExecuteNonQuery();
                            File.AppendAllText("debug.log", $"{DateTime.Now}: Added Fine column\n");
                        }
                    }
                    connection.Close();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection closed after migration\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"{DateTime.Now}: Error in MigrateDatabase: {ex.Message}\n");
                throw;
            }
        }

        public static List<Book> GetBooks()
        {
            var books = new List<Book>();
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection opened for GetBooks\n");
                    using (SQLiteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM Books";
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                books.Add(new Book
                                {
                                    Id = reader.GetInt32(0),
                                    Title = reader.GetString(1),
                                    Author = reader.GetString(2),
                                    Year = reader.GetInt32(3),
                                    CoverUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                                    Description = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    Genre = reader.IsDBNull(6) ? null : reader.GetString(6),
                                    ISBN = reader.IsDBNull(7) ? null : reader.GetString(7),
                                    IsAvailable = reader.GetInt32(8) == 1
                                });
                            }
                        }
                    }
                    connection.Close();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection closed after GetBooks\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"{DateTime.Now}: Error in GetBooks: {ex.Message}\n");
                throw;
            }
            return books;
        }

        public static void AddBook(Book book)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection opened for AddBook\n");
                    using (SQLiteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            INSERT INTO Books (Title, Author, Year, CoverUrl, Description, Genre, ISBN, IsAvailable)
                            VALUES (@Title, @Author, @Year, @CoverUrl, @Description, @Genre, @ISBN, @IsAvailable)";
                        command.Parameters.AddWithValue("@Title", book.Title);
                        command.Parameters.AddWithValue("@Author", book.Author);
                        command.Parameters.AddWithValue("@Year", book.Year);
                        command.Parameters.AddWithValue("@CoverUrl", (object)book.CoverUrl ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Description", (object)book.Description ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Genre", (object)book.Genre ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ISBN", (object)book.ISBN ?? DBNull.Value);
                        command.Parameters.AddWithValue("@IsAvailable", book.IsAvailable ? 1 : 0);
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection closed after AddBook\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"{DateTime.Now}: Error in AddBook: {ex.Message}\n");
                throw;
            }
        }

        public static void UpdateBook(Book book)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection opened for UpdateBook\n");
                    using (SQLiteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            UPDATE Books
                            SET Title = @Title, Author = @Author, Year = @Year, CoverUrl = @CoverUrl,
                                Description = @Description, Genre = @Genre, ISBN = @ISBN, IsAvailable = @IsAvailable
                            WHERE Id = @Id";
                        command.Parameters.AddWithValue("@Id", book.Id);
                        command.Parameters.AddWithValue("@Title", book.Title);
                        command.Parameters.AddWithValue("@Author", book.Author);
                        command.Parameters.AddWithValue("@Year", book.Year);
                        command.Parameters.AddWithValue("@CoverUrl", (object)book.CoverUrl ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Description", (object)book.Description ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Genre", (object)book.Genre ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ISBN", (object)book.ISBN ?? DBNull.Value);
                        command.Parameters.AddWithValue("@IsAvailable", book.IsAvailable ? 1 : 0);
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection closed after UpdateBook\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"{DateTime.Now}: Error in UpdateBook: {ex.Message}\n");
                throw;
            }
        }

        public static void DeleteBook(int bookId)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection opened for DeleteBook\n");
                    using (SQLiteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "DELETE FROM Books WHERE Id = @Id";
                        command.Parameters.AddWithValue("@Id", bookId);
                        command.ExecuteNonQuery();

                        command.CommandText = "DELETE FROM Rentals WHERE BookId = @BookId";
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@BookId", bookId);
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection closed after DeleteBook\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"{DateTime.Now}: Error in DeleteBook: {ex.Message}\n");
                throw;
            }
        }

        public static List<User> GetUsers()
        {
            var users = new List<User>();
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection opened for GetUsers\n");
                    using (SQLiteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM Users";
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                users.Add(new User
                                {
                                    Id = reader.GetInt32(0),
                                    Username = reader.GetString(1),
                                    Password = reader.GetString(2),
                                    Role = reader.GetString(3)
                                });
                            }
                        }
                    }
                    connection.Close();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection closed after GetUsers\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"{DateTime.Now}: Error in GetUsers: {ex.Message}\n");
                throw;
            }
            return users;
        }

        public static User GetUser(string username, string password)
        {
            User user = null;
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection opened for GetUser\n");
                    using (SQLiteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM Users WHERE Username = @Username AND Password = @Password";
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@Password", password);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                user = new User
                                {
                                    Id = reader.GetInt32(0),
                                    Username = reader.GetString(1),
                                    Password = reader.GetString(2),
                                    Role = reader.GetString(3)
                                };
                            }
                        }
                    }
                    connection.Close();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection closed after GetUser\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"{DateTime.Now}: Error in GetUser: {ex.Message}\n");
                throw;
            }
            return user;
        }

        public static void AddUser(User user)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection opened for AddUser\n");
                    using (SQLiteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO Users (Username, Password, Role) VALUES (@Username, @Password, @Role)";
                        command.Parameters.AddWithValue("@Username", user.Username);
                        command.Parameters.AddWithValue("@Password", user.Password);
                        command.Parameters.AddWithValue("@Role", user.Role);
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection closed after AddUser\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"{DateTime.Now}: Error in AddUser: {ex.Message}\n");
                throw;
            }
        }

        public static List<Rental> GetRentals()
        {
            var rentals = new List<Rental>();
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection opened for GetRentals\n");
                    using (SQLiteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM Rentals";
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                rentals.Add(new Rental
                                {
                                    Id = reader.GetInt32(0),
                                    UserId = reader.GetInt32(1),
                                    BookId = reader.GetInt32(2),
                                    RentDate = DateTime.Parse(reader.GetString(3)),
                                    DueDate = DateTime.Parse(reader.GetString(4)),
                                    ReturnDate = reader.IsDBNull(5) ? (DateTime?)null : DateTime.Parse(reader.GetString(5)),
                                    Fine = reader.GetDouble(6)
                                });
                            }
                        }
                    }
                    connection.Close();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection closed after GetRentals\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"{DateTime.Now}: Error in GetRentals: {ex.Message}\n");
                throw;
            }
            return rentals;
        }

        public static void AddRental(Rental rental)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection opened for AddRental\n");
                    using (SQLiteTransaction transaction = connection.BeginTransaction())
                    {
                        using (SQLiteCommand command = connection.CreateCommand())
                        {
                            command.CommandText = @"
                                INSERT INTO Rentals (UserId, BookId, RentDate, DueDate, ReturnDate, Fine)
                                VALUES (@UserId, @BookId, @RentDate, @DueDate, @ReturnDate, @Fine)";
                            command.Parameters.AddWithValue("@UserId", rental.UserId);
                            command.Parameters.AddWithValue("@BookId", rental.BookId);
                            command.Parameters.AddWithValue("@RentDate", rental.RentDate.ToString("o"));
                            command.Parameters.AddWithValue("@DueDate", rental.DueDate.ToString("o"));
                            command.Parameters.AddWithValue("@ReturnDate", (object)rental.ReturnDate?.ToString("o") ?? DBNull.Value);
                            command.Parameters.AddWithValue("@Fine", rental.Fine);
                            command.ExecuteNonQuery();

                            command.CommandText = "UPDATE Books SET IsAvailable = 0 WHERE Id = @BookId";
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@BookId", rental.BookId);
                            command.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                    connection.Close();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection closed after AddRental\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"{DateTime.Now}: Error in AddRental: {ex.Message}\n");
                throw;
            }
        }

        public static void ReturnBook(int rentalId)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection opened for ReturnBook\n");
                    using (SQLiteTransaction transaction = connection.BeginTransaction())
                    {
                        using (SQLiteCommand command = connection.CreateCommand())
                        {
                            command.CommandText = "SELECT BookId, DueDate FROM Rentals WHERE Id = @Id";
                            command.Parameters.AddWithValue("@Id", rentalId);
                            int bookId = 0;
                            DateTime dueDate = DateTime.MinValue;
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    bookId = reader.GetInt32(0);
                                    dueDate = DateTime.Parse(reader.GetString(1));
                                }
                            }

                            double fine = 0;
                            if (DateTime.Now > dueDate)
                            {
                                var daysLate = (DateTime.Now - dueDate).Days;
                                fine = daysLate * 10; // 10 рублей за день просрочки
                            }

                            command.CommandText = @"
                                UPDATE Rentals
                                SET ReturnDate = @ReturnDate, Fine = @Fine
                                WHERE Id = @Id";
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@Id", rentalId);
                            command.Parameters.AddWithValue("@ReturnDate", DateTime.Now.ToString("o"));
                            command.Parameters.AddWithValue("@Fine", fine);
                            command.ExecuteNonQuery();

                            command.CommandText = "UPDATE Books SET IsAvailable = 1 WHERE Id = @BookId";
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@BookId", bookId);
                            command.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                    connection.Close();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection closed after ReturnBook\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"{DateTime.Now}: Error in ReturnBook: {ex.Message}\n");
                throw;
            }
        }

        public static void ExtendRental(int rentalId)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection opened for ExtendRental\n");
                    using (SQLiteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT DueDate FROM Rentals WHERE Id = @Id";
                        command.Parameters.AddWithValue("@Id", rentalId);
                        DateTime dueDate;
                        using (var reader = command.ExecuteReader())
                        {
                            reader.Read();
                            dueDate = DateTime.Parse(reader.GetString(0));
                        }

                        var newDueDate = dueDate.AddDays(30);
                        command.CommandText = "UPDATE Rentals SET DueDate = @DueDate WHERE Id = @Id";
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@Id", rentalId);
                        command.Parameters.AddWithValue("@DueDate", newDueDate.ToString("o"));
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection closed after ExtendRental\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"{DateTime.Now}: Error in ExtendRental: {ex.Message}\n");
                throw;
            }
        }

        public static List<Subscription> GetSubscriptions()
        {
            var subscriptions = new List<Subscription>();
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection opened for GetSubscriptions\n");
                    using (SQLiteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM Subscriptions";
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                subscriptions.Add(new Subscription
                                {
                                    Id = reader.GetInt32(0),
                                    UserId = reader.GetInt32(1),
                                    StartDate = DateTime.Parse(reader.GetString(2)),
                                    EndDate = DateTime.Parse(reader.GetString(3))
                                });
                            }
                        }
                    }
                    connection.Close();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection closed after GetSubscriptions\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"{DateTime.Now}: Error in GetSubscriptions: {ex.Message}\n");
                throw;
            }
            return subscriptions;
        }

        public static void AddSubscription(Subscription subscription)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection opened for AddSubscription\n");
                    using (SQLiteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            INSERT INTO Subscriptions (UserId, StartDate, EndDate)
                            VALUES (@UserId, @StartDate, @EndDate)";
                        command.Parameters.AddWithValue("@UserId", subscription.UserId);
                        command.Parameters.AddWithValue("@StartDate", subscription.StartDate.ToString("o"));
                        command.Parameters.AddWithValue("@EndDate", subscription.EndDate.ToString("o"));
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection closed after AddSubscription\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"{DateTime.Now}: Error in AddSubscription: {ex.Message}\n");
                throw;
            }
        }

        public static void RegisterUser(User user)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection opened for RegisterUser\n");
                    using (SQLiteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO Users (Username, Password, Role) VALUES (@Username, @Password, @Role)";
                        command.Parameters.AddWithValue("@Username", user.Username);
                        command.Parameters.AddWithValue("@Password", user.Password);
                        command.Parameters.AddWithValue("@Role", user.Role);
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection closed after RegisterUser\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"{DateTime.Now}: Error in RegisterUser: {ex.Message}\n");
                throw;
            }
        }

        public static User LoginUser(string username, string password)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection opened for LoginUser\n");
                    using (SQLiteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM Users WHERE Username = @Username AND Password = @Password";
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@Password", password);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new User
                                {
                                    Id = reader.GetInt32(0),
                                    Username = reader.GetString(1),
                                    Password = reader.GetString(2),
                                    Role = reader.GetString(3)
                                };
                            }
                        }
                    }
                    connection.Close();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection closed after LoginUser\n");
                }
                return null;
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"{DateTime.Now}: Error in LoginUser: {ex.Message}\n");
                throw;
            }
        }

        public static double CheckFines(int userId)
        {
            try
            {
                double totalFine = 0;
                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection opened for CheckFines\n");
                    using (SQLiteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT Fine FROM Rentals WHERE UserId = @UserId AND ReturnDate IS NOT NULL AND Fine > 0";
                        command.Parameters.AddWithValue("@UserId", userId);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                totalFine += reader.GetDouble(0);
                            }
                        }
                    }
                    connection.Close();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection closed after CheckFines\n");
                }
                return totalFine;
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"{DateTime.Now}: Error in CheckFines: {ex.Message}\n");
                throw;
            }
        }

        public static bool HasValidSubscription(int userId)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection opened for HasValidSubscription\n");
                    using (SQLiteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT COUNT(*) FROM Subscriptions WHERE UserId = @UserId AND EndDate > @CurrentDate";
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@CurrentDate", DateTime.Now.ToString("o"));
                        int count = Convert.ToInt32(command.ExecuteScalar());
                        connection.Close();
                        File.AppendAllText("debug.log", $"{DateTime.Now}: Connection closed after HasValidSubscription\n");
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"{DateTime.Now}: Error in HasValidSubscription: {ex.Message}\n");
                throw;
            }
        }

        public static void RentBook(Rental rental)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection opened for RentBook\n");
                    using (SQLiteTransaction transaction = connection.BeginTransaction())
                    {
                        using (SQLiteCommand command = connection.CreateCommand())
                        {
                            // Проверка доступности книги
                            command.CommandText = "SELECT IsAvailable FROM Books WHERE Id = @BookId";
                            command.Parameters.AddWithValue("@BookId", rental.BookId);
                            bool isAvailable = false;
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    isAvailable = reader.GetInt32(0) == 1;
                                }
                            }

                            if (!isAvailable)
                            {
                                throw new Exception("Книга уже выдана");
                            }

                            // Добавление аренды
                            command.CommandText = @"
                                INSERT INTO Rentals (UserId, BookId, RentDate, DueDate, ReturnDate, Fine)
                                VALUES (@UserId, @BookId, @RentDate, @DueDate, @ReturnDate, @Fine)";
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@UserId", rental.UserId);
                            command.Parameters.AddWithValue("@BookId", rental.BookId);
                            command.Parameters.AddWithValue("@RentDate", rental.RentDate.ToString("o"));
                            command.Parameters.AddWithValue("@DueDate", rental.DueDate.ToString("o"));
                            command.Parameters.AddWithValue("@ReturnDate", (object)rental.ReturnDate?.ToString("o") ?? DBNull.Value);
                            command.Parameters.AddWithValue("@Fine", rental.Fine);
                            command.ExecuteNonQuery();

                            // Обновление статуса книги
                            command.CommandText = "UPDATE Books SET IsAvailable = 0 WHERE Id = @BookId";
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@BookId", rental.BookId);
                            command.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                    connection.Close();
                    File.AppendAllText("debug.log", $"{DateTime.Now}: Connection closed after RentBook\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"{DateTime.Now}: Error in RentBook: {ex.Message}\n");
                throw;
            }
        }
    }
}