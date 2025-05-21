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
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }

            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
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

                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Users (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Username TEXT NOT NULL UNIQUE,
                            Password TEXT NOT NULL,
                            Role TEXT NOT NULL
                        )";
                    command.ExecuteNonQuery();

                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Subscriptions (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            UserId INTEGER NOT NULL,
                            StartDate TEXT NOT NULL,
                            EndDate TEXT NOT NULL,
                            FOREIGN KEY (UserId) REFERENCES Users(Id)
                        )";
                    command.ExecuteNonQuery();

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

                    command.CommandText = "INSERT OR IGNORE INTO Users (Username, Password, Role) VALUES ('admin', 'admin123', 'Librarian')";
                    command.ExecuteNonQuery();
                }
                connection.Close();
            }

            MigrateDatabase();
        }

        private static void MigrateDatabase()
        {
            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        ALTER TABLE Books ADD COLUMN Genre TEXT;
                        ALTER TABLE Books ADD COLUMN ISBN TEXT;
                        ALTER TABLE Books ADD COLUMN IsAvailable INTEGER DEFAULT 1";
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (SQLiteException ex)
                    {
                        File.AppendAllText("debug.log", $"{DateTime.Now}: Migration warning: {ex.Message}\n");
                    }
                }
                connection.Close();
            }
        }

        public static void AddBook(Book book)
        {
            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO Books (Title, Author, Year, CoverUrl, Description, Genre, ISBN, IsAvailable)
                        VALUES (@Title, @Author, @Year, @CoverUrl, @Description, @Genre, @ISBN, @IsAvailable)";
                    command.Parameters.AddWithValue("@Title", book.Title);
                    command.Parameters.AddWithValue("@Author", book.Author);
                    command.Parameters.AddWithValue("@Year", book.Year);
                    command.Parameters.AddWithValue("@CoverUrl", book.CoverUrl ?? "");
                    command.Parameters.AddWithValue("@Description", book.Description ?? "");
                    command.Parameters.AddWithValue("@Genre", book.Genre ?? "");
                    command.Parameters.AddWithValue("@ISBN", book.ISBN ?? "");
                    command.Parameters.AddWithValue("@IsAvailable", book.IsAvailable ? 1 : 0);
                    command.ExecuteNonQuery();
                    book.Id = (int)connection.LastInsertRowId;
                }
                connection.Close();
            }
        }

        public static void UpdateBook(Book book)
        {
            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
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
                    command.Parameters.AddWithValue("@CoverUrl", book.CoverUrl ?? "");
                    command.Parameters.AddWithValue("@Description", book.Description ?? "");
                    command.Parameters.AddWithValue("@Genre", book.Genre ?? "");
                    command.Parameters.AddWithValue("@ISBN", book.ISBN ?? "");
                    command.Parameters.AddWithValue("@IsAvailable", book.IsAvailable ? 1 : 0);
                    command.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        public static void DeleteBook(int bookId)
        {
            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM Books WHERE Id = @Id";
                    command.Parameters.AddWithValue("@Id", bookId);
                    command.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        public static List<Book> GetBooks()
        {
            List<Book> books = new List<Book>();
            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM Books";
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            books.Add(new Book
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Author = reader.GetString(2),
                                Year = reader.GetInt32(3),
                                CoverUrl = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                Description = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                Genre = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                ISBN = reader.IsDBNull(7) ? "" : reader.GetString(7),
                                IsAvailable = reader.IsDBNull(8) ? true : reader.GetInt32(8) == 1
                            });
                        }
                    }
                }
                connection.Close();
            }
            return books;
        }

        public static void RegisterUser(User user)
        {
            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO Users (Username, Password, Role) VALUES (@Username, @Password, @Role)";
                    command.Parameters.AddWithValue("@Username", user.Username);
                    command.Parameters.AddWithValue("@Password", user.Password);
                    command.Parameters.AddWithValue("@Role", user.Role);
                    command.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        public static User LoginUser(string username, string password)
        {
            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM Users WHERE Username = @Username AND Password = @Password";
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);
                    using (SQLiteDataReader reader = command.ExecuteReader())
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
            }
            return null;
        }

        public static List<User> GetUsers()
        {
            List<User> users = new List<User>();
            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM Users";
                    using (SQLiteDataReader reader = command.ExecuteReader())
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
            }
            return users;
        }

        public static void RentBook(Rental rental)
        {
            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO Rentals (UserId, BookId, RentDate, DueDate, Fine)
                        VALUES (@UserId, @BookId, @RentDate, @DueDate, @Fine);
                        UPDATE Books SET IsAvailable = 0 WHERE Id = @BookId";
                    command.Parameters.AddWithValue("@UserId", rental.UserId);
                    command.Parameters.AddWithValue("@BookId", rental.BookId);
                    command.Parameters.AddWithValue("@RentDate", rental.RentDate.ToString("o"));
                    command.Parameters.AddWithValue("@DueDate", rental.DueDate.ToString("o"));
                    command.Parameters.AddWithValue("@Fine", rental.Fine);
                    command.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        public static void ReturnBook(int rentalId)
        {
            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE Rentals SET ReturnDate = @ReturnDate, Fine = 0 WHERE Id = @Id;
                        UPDATE Books SET IsAvailable = 1 
                        WHERE Id = (SELECT BookId FROM Rentals WHERE Id = @Id)";
                    command.Parameters.AddWithValue("@Id", rentalId);
                    command.Parameters.AddWithValue("@ReturnDate", DateTime.Now.ToString("o"));
                    command.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        public static void ExtendRental(int rentalId)
        {
            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE Rentals 
                        SET DueDate = @NewDueDate, Fine = 0 
                        WHERE Id = @Id";
                    command.Parameters.AddWithValue("@Id", rentalId);
                    command.Parameters.AddWithValue("@NewDueDate", DateTime.Now.AddDays(30).ToString("o"));
                    command.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        public static List<Rental> GetRentals()
        {
            List<Rental> rentals = new List<Rental>();
            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM Rentals";
                    using (SQLiteDataReader reader = command.ExecuteReader())
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
                                ReturnDate = !reader.IsDBNull(5) ? (DateTime?)DateTime.Parse(reader.GetString(5)) : null,
                                Fine = reader.GetDouble(6)
                            });
                        }
                    }
                }
                connection.Close();
            }
            return rentals;
        }

        public static void CheckFines()
        {
            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM Rentals WHERE ReturnDate IS NULL AND DueDate < @CurrentDate";
                    command.Parameters.AddWithValue("@CurrentDate", DateTime.Now.ToString("o"));
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int rentalId = reader.GetInt32(0);
                            DateTime dueDate = DateTime.Parse(reader.GetString(4));
                            int daysOverdue = (DateTime.Now - dueDate).Days;
                            if (daysOverdue > 0)
                            {
                                double fine = daysOverdue * 10.0;
                                using (SQLiteCommand updateCommand = connection.CreateCommand())
                                {
                                    updateCommand.CommandText = "UPDATE Rentals SET Fine = @Fine WHERE Id = @Id";
                                    updateCommand.Parameters.AddWithValue("@Fine", fine);
                                    updateCommand.Parameters.AddWithValue("@Id", rentalId);
                                    updateCommand.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
                connection.Close();
            }
        }

        public static void AddSubscription(Subscription subscription)
        {
            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
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
            }
        }

        public static List<Subscription> GetSubscriptions()
        {
            List<Subscription> subscriptions = new List<Subscription>();
            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM Subscriptions";
                    using (SQLiteDataReader reader = command.ExecuteReader())
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
            }
            return subscriptions;
        }

        public static bool HasValidSubscription(int userId)
        {
            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM Subscriptions WHERE UserId = @UserId AND EndDate > @CurrentDate";
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@CurrentDate", DateTime.Now.ToString("o"));
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }
    }
}