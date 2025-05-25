using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace LibraryManagementSystem
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient client = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                // Лоадер уже виден из XAML
                await WebView.EnsureCoreWebView2Async(null);
                WebView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;
                WebView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                WebView.CoreWebView2.Settings.IsScriptEnabled = true;
                WebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                await WebView.CoreWebView2.Profile.ClearBrowsingDataAsync();
                WebView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                WebView.CoreWebView2.WebResourceRequested += (s, args) =>
                {
                    args.Request.Headers.SetHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                };

                // Устанавливаем тёмный фон для WebView2
                WebView.CoreWebView2.ExecuteScriptAsync("document.body.style.backgroundColor = '#1a1a1a';");

                string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "login.html");
                if (!File.Exists(htmlPath))
                {
                    MessageBox.Show($"HTML файл не найден: {htmlPath}");
                    AppLoader.Visibility = Visibility.Collapsed;
                    return;
                }

                // Ждём полной загрузки DOM
                WebView.CoreWebView2.NavigationCompleted += async (s, args) =>
                {
                    if (args.IsSuccess)
                    {
                        // Ждём, пока DOM полностью загрузится
                        await WebView.CoreWebView2.ExecuteScriptAsync(@"
                            new Promise(resolve => {
                                if (document.readyState === 'complete') {
                                    resolve();
                                } else {
                                    window.addEventListener('load', resolve);
                                }
                            });
                        ");
                        // Показываем WebView2 и скрываем лоадер
                        WebView.Visibility = Visibility.Visible;
                        AppLoader.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка загрузки страницы: {args.WebErrorStatus}");
                        AppLoader.Visibility = Visibility.Collapsed;
                    }
                };

                WebView.Source = new Uri($"file:///{htmlPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка WebView2: {ex.Message}");
                AppLoader.Visibility = Visibility.Collapsed;
            }
        }

        private async Task<string> SaveImageLocally(string imageUrl, int bookId)
        {
            try
            {
                string imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images");
                Directory.CreateDirectory(imagesDir);
                string extension = Path.GetExtension(new Uri(imageUrl).LocalPath) ?? ".jpg";
                string fileName = $"book_{bookId}{extension}";
                string filePath = Path.Combine(imagesDir, fileName);

                using (var response = await client.GetAsync(imageUrl))
                {
                    response.EnsureSuccessStatusCode();
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = File.Create(filePath))
                    {
                        await stream.CopyToAsync(fileStream);
                    }
                }
                return $"images/{fileName}";
            }
            catch (Exception ex)
            {
                File.AppendAllText("debug.log", $"{DateTime.Now}: Failed to save image {imageUrl}: {ex.Message}\n");
                return imageUrl;
            }
        }

        private void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            var message = JsonConvert.DeserializeObject<Message>(args.WebMessageAsJson);
            try
            {
                switch (message.Action)
                {
                    case "AddBook":
                        var book = JsonConvert.DeserializeObject<Book>(message.Data.ToString());
                        LibraryDbContext.AddBook(book);
                        if (!string.IsNullOrEmpty(book.CoverUrl) && book.CoverUrl.StartsWith("http"))
                        {
                            book.CoverUrl = SaveImageLocally(book.CoverUrl, book.Id).GetAwaiter().GetResult();
                            LibraryDbContext.UpdateBook(book);
                        }
                        SendResponse("Книга добавлена успешно");
                        break;
                    case "UpdateBook":
                        var updatedBook = JsonConvert.DeserializeObject<Book>(message.Data.ToString());
                        if (!string.IsNullOrEmpty(updatedBook.CoverUrl) && updatedBook.CoverUrl.StartsWith("http"))
                        {
                            updatedBook.CoverUrl = SaveImageLocally(updatedBook.CoverUrl, updatedBook.Id).GetAwaiter().GetResult();
                        }
                        LibraryDbContext.UpdateBook(updatedBook);
                        SendResponse("Книга обновлена успешно");
                        break;
                    case "DeleteBook":
                        var bookId = JsonConvert.DeserializeObject<int>(message.Data.ToString());
                        LibraryDbContext.DeleteBook(bookId);
                        SendResponse("Книга удалена успешно");
                        break;
                    case "GetBooks":
                        var books = LibraryDbContext.GetBooks();
                        SendResponse(JsonConvert.SerializeObject(books));
                        break;
                    case "RegisterUser":
                        var user = JsonConvert.DeserializeObject<User>(message.Data.ToString());
                        LibraryDbContext.RegisterUser(user);
                        SendResponse("Пользователь зарегистрирован");
                        break;
                    case "LoginUser":
                        var loginData = JsonConvert.DeserializeObject<dynamic>(message.Data.ToString());
                        var loggedInUser = LibraryDbContext.LoginUser(loginData.Username.ToString(), loginData.Password.ToString());
                        if (loggedInUser != null && loggedInUser.Role == "Librarian")
                        {
                            LibraryDbContext.CheckFines(loggedInUser.Id); // Передаём userId
                        }
                        SendResponse(loggedInUser != null ? JsonConvert.SerializeObject(loggedInUser) : "Неверный логин или пароль");
                        break;
                    case "GetUsers":
                        var users = LibraryDbContext.GetUsers();
                        SendResponse(JsonConvert.SerializeObject(users));
                        break;
                    case "RentBook":
                        var rental = JsonConvert.DeserializeObject<Rental>(message.Data.ToString());
                        if (LibraryDbContext.HasValidSubscription(rental.UserId))
                        {
                            LibraryDbContext.RentBook(rental);
                            SendResponse("Книга выдана");
                        }
                        else
                        {
                            SendResponse("Ошибка: у пользователя нет действующего абонемента");
                        }
                        break;
                    case "ReturnBook":
                        var rentalId = JsonConvert.DeserializeObject<int>(message.Data.ToString());
                        LibraryDbContext.ReturnBook(rentalId);
                        SendResponse("Книга возвращена");
                        break;
                    case "ExtendRental":
                        var extendRentalId = JsonConvert.DeserializeObject<int>(message.Data.ToString());
                        LibraryDbContext.ExtendRental(extendRentalId);
                        SendResponse("Аренда продлена");
                        break;
                    case "GetRentals":
                        var rentals = LibraryDbContext.GetRentals();
                        SendResponse(JsonConvert.SerializeObject(rentals));
                        break;
                    case "AddSubscription":
                        var subscription = JsonConvert.DeserializeObject<Subscription>(message.Data.ToString());
                        LibraryDbContext.AddSubscription(subscription);
                        SendResponse("Абонемент добавлен");
                        break;
                    case "GetSubscriptions":
                        var subscriptions = LibraryDbContext.GetSubscriptions();
                        SendResponse(JsonConvert.SerializeObject(subscriptions));
                        break;
                    default:
                        SendResponse("Неизвестное действие");
                        break;
                }
            }
            catch (Exception ex)
            {
                SendResponse($"Ошибка: {ex.Message}");
            }
        }

        private void SendResponse(string response)
        {
            File.AppendAllText("debug.log", $"{DateTime.Now}: Sending response: {response}\n");
            WebView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new { Response = response }));
        }
    }

    public class Message
    {
        public string Action { get; set; }
        public object Data { get; set; }
    }
}