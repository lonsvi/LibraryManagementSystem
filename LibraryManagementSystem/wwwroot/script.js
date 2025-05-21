let isLibrarian = false;
let currentUser = null;
let books = [];
let users = [];
let rentals = [];
let subscriptions = [];
let isTableView = false;
let pendingRequests = new Set(); // Отслеживание активных запросов

function navigateTo(page) {
    console.log(`Navigating to: ${page}`);
    window.location.href = page;
}

function login() {
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;
    if (!username || !password) {
        alert('Введите имя пользователя и пароль');
        return;
    }
    console.log(`Sending login request for username: ${username}`);
    window.chrome.webview.postMessage({
        Action: 'LoginUser',
        Data: { Username: username, Password: password }
    });
}

function logout() {
    console.log('Logging out');
    currentUser = null;
    isLibrarian = false;
    books = [];
    users = [];
    rentals = [];
    subscriptions = [];
    localStorage.removeItem('currentUser');
    localStorage.removeItem('books');
    navigateTo('login.html');
}

function addBook() {
    if (!isLibrarian) {
        alert('Только библиотекари могут добавлять книги');
        return;
    }
    const book = {
        Title: document.getElementById('title').value,
        Author: document.getElementById('author').value,
        Year: parseInt(document.getElementById('year').value),
        CoverUrl: document.getElementById('coverUrl').value,
        Description: document.getElementById('description').value,
        Genre: document.getElementById('genre').value,
        ISBN: document.getElementById('isbn').value
    };
    if (!book.Title || !book.Author || !book.Year) {
        alert('Заполните обязательные поля: название, автор, год');
        return;
    }
    if (book.CoverUrl && !isValidUrl(book.CoverUrl) && !book.CoverUrl.startsWith('images/')) {
        alert('Некорректный URL обложки');
        return;
    }
    console.log('Adding book:', book);
    pendingRequests.add('AddBook');
    window.chrome.webview.postMessage({ Action: 'AddBook', Data: book });
}

function updateBook() {
    if (!isLibrarian) {
        alert('Только библиотекари могут редактировать книги');
        return;
    }
    const bookId = parseInt(document.getElementById('editBookSelect').value);
    if (!bookId) {
        alert('Выберите книгу для редактирования');
        return;
    }
    const book = {
        Id: bookId,
        Title: document.getElementById('editTitle').value,
        Author: document.getElementById('editAuthor').value,
        Year: parseInt(document.getElementById('editYear').value),
        CoverUrl: document.getElementById('editCoverUrl').value,
        Description: document.getElementById('editDescription').value,
        Genre: document.getElementById('editGenre').value,
        ISBN: document.getElementById('editISBN').value,
        IsAvailable: books.find(b => b.Id === bookId)?.IsAvailable ?? true
    };
    if (!book.Title || !book.Author || !book.Year) {
        alert('Заполните обязательные поля: название, автор, год');
        return;
    }
    if (book.CoverUrl && !isValidUrl(book.CoverUrl) && !book.CoverUrl.startsWith('images/')) {
        alert('Некорректный URL обложки');
        return;
    }
    console.log('Updating book:', book);
    pendingRequests.add('UpdateBook');
    window.chrome.webview.postMessage({ Action: 'UpdateBook', Data: book });
}

function deleteBook() {
    if (!isLibrarian) {
        alert('Только библиотекари могут удалять книги');
        return;
    }
    const bookId = parseInt(document.getElementById('editBookSelect').value);
    if (!bookId) {
        alert('Выберите книгу для удаления');
        return;
    }
    if (confirm('Вы уверены, что хотите удалить книгу?')) {
        console.log('Deleting book:', bookId);
        pendingRequests.add('DeleteBook');
        window.chrome.webview.postMessage({ Action: 'DeleteBook', Data: bookId });
    }
}

function isValidUrl(url) {
    try {
        new URL(url);
        return url.match(/\.(jpeg|jpg|png|gif|webp)(\?.*)?$/i) !== null;
    } catch (_) {
        return false;
    }
}

function registerUser() {
    if (!isLibrarian) {
        alert('Только библиотекари могут регистрировать пользователей');
        return;
    }
    const user = {
        Username: document.getElementById('newUsername').value,
        Password: document.getElementById('newPassword').value,
        Role: document.getElementById('newRole').value
    };
    if (!user.Username || !user.Password) {
        alert('Заполните имя пользователя и пароль');
        return;
    }
    console.log('Registering user:', user);
    pendingRequests.add('RegisterUser');
    window.chrome.webview.postMessage({ Action: 'RegisterUser', Data: user });
}

function addSubscription() {
    if (!isLibrarian) {
        alert('Только библиотекари могут добавлять абонементы');
        return;
    }
    const subscription = {
        UserId: parseInt(document.getElementById('subscriptionUserSelect').value),
        StartDate: new Date(document.getElementById('subscriptionStartDate').value),
        EndDate: new Date(document.getElementById('subscriptionEndDate').value)
    };
    if (!subscription.UserId || !subscription.StartDate || !subscription.EndDate) {
        alert('Заполните все поля для абонемента');
        return;
    }
    if (subscription.EndDate <= subscription.StartDate) {
        alert('Дата окончания должна быть позже даты начала');
        return;
    }
    console.log('Adding subscription:', subscription);
    pendingRequests.add('AddSubscription');
    window.chrome.webview.postMessage({ Action: 'AddSubscription', Data: subscription });
}

function rentBook() {
    if (!isLibrarian) {
        alert('Только библиотекари могут выдавать книги');
        return;
    }
    const rental = {
        BookId: parseInt(document.getElementById('bookSelect').value),
        UserId: parseInt(document.getElementById('userSelect').value),
        RentDate: new Date().toISOString(),
        DueDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString(),
        Fine: 0
    };
    if (!rental.BookId || !rental.UserId) {
        alert('Выберите книгу и пользователя');
        return;
    }
    if (!books.find(b => b.Id === rental.BookId)?.IsAvailable) {
        alert('Книга уже выдана');
        return;
    }
    console.log('Renting book:', rental);
    pendingRequests.add('RentBook');
    window.chrome.webview.postMessage({ Action: 'RentBook', Data: rental });
}

function returnBook() {
    if (!isLibrarian) {
        alert('Только библиотекари могут принимать возврат книг');
        return;
    }
    const rentalId = parseInt(document.getElementById('rentalSelect').value);
    if (!rentalId) {
        alert('Выберите аренду для возврата');
        return;
    }
    console.log('Returning book, rental:', rentalId);
    pendingRequests.add('ReturnBook');
    window.chrome.webview.postMessage({ Action: 'ReturnBook', Data: rentalId });
}

function extendRental() {
    if (!isLibrarian) {
        alert('Только библиотекари могут продлевать аренду');
        return;
    }
    const rentalId = parseInt(document.getElementById('rentalSelect').value);
    if (!rentalId) {
        alert('Выберите аренду для продления');
        return;
    }
    console.log('Extending rental:', rentalId);
    pendingRequests.add('ExtendRental');
    window.chrome.webview.postMessage({ Action: 'ExtendRental', Data: rentalId });
}

function populateBookSelect(books) {
    const bookSelect = document.getElementById('bookSelect');
    const editBookSelect = document.getElementById('editBookSelect');
    if (bookSelect) {
        bookSelect.innerHTML = '<option value="">Выберите книгу</option>';
        books.forEach(book => {
            if (book.IsAvailable) {
                const option = document.createElement('option');
                option.value = book.Id;
                option.textContent = book.Title;
                bookSelect.appendChild(option);
            }
        });
    }
    if (editBookSelect) {
        editBookSelect.innerHTML = '<option value="">Выберите книгу</option>';
        books.forEach(book => {
            const option = document.createElement('option');
            option.value = book.Id;
            option.textContent = book.Title;
            editBookSelect.appendChild(option);
        });
    }
}

function populateUserSelect(users) {
    const userSelect = document.getElementById('userSelect');
    const subscriptionUserSelect = document.getElementById('subscriptionUserSelect');
    if (userSelect) {
        userSelect.innerHTML = '<option value="">Выберите пользователя</option>';
        users.forEach(user => {
            if (user.Role === 'User') {
                const option = document.createElement('option');
                option.value = user.Id;
                option.textContent = user.Username;
                userSelect.appendChild(option);
            }
        });
    }
    if (subscriptionUserSelect) {
        subscriptionUserSelect.innerHTML = '<option value="">Выберите пользователя</option>';
        users.forEach(user => {
            if (user.Role === 'User') {
                const option = document.createElement('option');
                option.value = user.Id;
                option.textContent = user.Username;
                subscriptionUserSelect.appendChild(option);
            }
        });
    }
}

function populateRentalSelect(rentals) {
    const rentalSelect = document.getElementById('rentalSelect');
    if (rentalSelect) {
        rentalSelect.innerHTML = '<option value="">Выберите аренду</option>';
        rentals.forEach(rental => {
            if (!rental.ReturnDate) {
                const book = books.find(b => b.Id === rental.BookId);
                const user = users.find(u => u.Id === rental.UserId);
                const option = document.createElement('option');
                option.value = rental.Id;
                option.textContent = `${book?.Title || 'Книга'} - ${user?.Username || 'Пользователь'} (до ${new Date(rental.DueDate).toLocaleDateString()})`;
                rentalSelect.appendChild(option);
            }
        });
    }
}

function populateEditBookForm() {
    const editBookSelect = document.getElementById('editBookSelect');
    if (editBookSelect) {
        editBookSelect.onchange = () => {
            const bookId = parseInt(editBookSelect.value);
            const book = books.find(b => b.Id === bookId);
            if (book) {
                document.getElementById('editTitle').value = book.Title;
                document.getElementById('editAuthor').value = book.Author;
                document.getElementById('editYear').value = book.Year;
                document.getElementById('editCoverUrl').value = book.CoverUrl || '';
                document.getElementById('editDescription').value = book.Description || '';
                document.getElementById('editGenre').value = book.Genre || '';
                document.getElementById('editISBN').value = book.ISBN || '';
            }
        };
    }
}

function checkFinesAlert() {
    const overdue = rentals.filter(r => !r.ReturnDate && new Date(r.DueDate) < new Date());
    const finesAlert = document.getElementById('finesAlert');
    if (finesAlert && overdue.length > 0 && isLibrarian) {
        finesAlert.style.display = 'block';
        finesAlert.innerHTML = `<p>Просроченных аренд: ${overdue.length}. Общая сумма штрафов: ${overdue.reduce((sum, r) => sum + r.Fine, 0)} руб.</p>`;
    } else if (finesAlert) {
        finesAlert.style.display = 'none';
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function displayBooks(booksToDisplay) {
    const grid = document.getElementById('bookGrid');
    const table = document.getElementById('bookTable');
    if (grid && table) {
        // Убрали принудительный reflow для оптимизации
        if (isTableView) {
            displayBooksTable(booksToDisplay);
        } else {
            displayBooksGrid(booksToDisplay);
        }
    }
}

function displayBooksGrid(booksToDisplay) {
    const grid = document.getElementById('bookGrid');
    const table = document.getElementById('bookTable');
    if (grid && table) {
        grid.style.display = 'grid';
        table.style.display = 'none';
        grid.innerHTML = '';
        booksToDisplay.forEach(book => {
            console.log('Displaying book:', book);
            const card = document.createElement('div');
            card.className = 'book-card fade-in'; // Анимация на уровне элемента
            const img = document.createElement('img');
            const src = book.CoverUrl && book.CoverUrl.startsWith('images/')
                ? book.CoverUrl
                : (book.CoverUrl || 'https://placekitten.com/150/150');
            img.src = src;
            img.alt = escapeHtml(book.Title);
            img.onerror = () => {
                console.error(`Failed to load image: ${img.src}`);
                img.src = 'https://placekitten.com/150/150';
            };
            img.onload = () => console.log(`Image loaded: ${img.src}`);
            card.appendChild(img);
            const title = document.createElement('h3');
            title.textContent = escapeHtml(book.Title);
            card.appendChild(title);
            const author = document.createElement('p');
            author.textContent = `${escapeHtml(book.Author)} (${book.Year})`;
            card.appendChild(author);
            const genre = document.createElement('p');
            genre.textContent = book.Genre ? `Жанр: ${escapeHtml(book.Genre)}` : '';
            card.appendChild(genre);
            const isbn = document.createElement('p');
            isbn.textContent = book.ISBN ? `ISBN: ${escapeHtml(book.ISBN)}` : '';
            card.appendChild(isbn);
            const status = document.createElement('p');
            status.textContent = book.IsAvailable ? 'Доступна' : 'Выдана';
            status.style.color = book.IsAvailable ? '#4caf50' : '#cc0000';
            card.appendChild(status);
            const desc = document.createElement('p');
            desc.className = 'description';
            desc.textContent = escapeHtml(book.Description);
            card.appendChild(desc);
            grid.appendChild(card);
        });
    }
}

function displayBooksTable(booksToDisplay) {
    const grid = document.getElementById('bookGrid');
    const table = document.getElementById('bookTable');
    const tbody = document.getElementById('bookTableBody');
    if (grid && table && tbody) {
        grid.style.display = 'none';
        table.style.display = 'table';
        tbody.innerHTML = '';
        booksToDisplay.forEach(book => {
            const row = document.createElement('tr');
            row.className = 'fade-in'; // Анимация на уровне строки
            row.innerHTML = `
                <td><img src="${book.CoverUrl || 'https://placekitten.com/150/150'}" alt="${escapeHtml(book.Title)}" style="max-width: 60px;"></td>
                <td>${escapeHtml(book.Title)}</td>
                <td>${escapeHtml(book.Author)}</td>
                <td>${book.Year}</td>
                <td>${escapeHtml(book.Genre || '')}</td>
                <td>${escapeHtml(book.ISBN || '')}</td>
                <td style="color: ${book.IsAvailable ? '#4caf50' : '#cc0000'}">${book.IsAvailable ? 'Доступна' : 'Выдана'}</td>
                <td>${escapeHtml(book.Description || '')}</td>
            `;
            tbody.appendChild(row);
        });
    }
}

function toggleView() {
    isTableView = !isTableView;
    displayBooks(books);
}

function filterBooks() {
    const search = document.getElementById('search');
    if (search) {
        const searchValue = search.value.toLowerCase();
        const filtered = books.filter(book =>
            book.Title.toLowerCase().includes(searchValue) ||
            book.Author.toLowerCase().includes(searchValue) ||
            (book.Genre && book.Genre.toLowerCase().includes(searchValue)) ||
            (book.ISBN && book.ISBN.toLowerCase().includes(searchValue))
        );
        displayBooks(filtered);
    }
}

window.chrome.webview.addEventListener('message', event => {
    const response = event.data.Response;
    console.log('Received response:', response);
    try {
        if (typeof response === 'string') {
            if (response.startsWith('[')) {
                const data = JSON.parse(response);
                if (data[0]?.Title) { // Books
                    books = data;
                    // Сохраняем в localStorage только при изменении книг
                    localStorage.setItem('books', JSON.stringify(books));
                    displayBooks(books);
                    populateBookSelect(books);
                    populateEditBookForm();
                    pendingRequests.delete('GetBooks');
                } else if (data[0]?.Username) { // Users
                    users = data;
                    populateUserSelect(users);
                    pendingRequests.delete('GetUsers');
                } else if (data[0]?.BookId) { // Rentals
                    rentals = data;
                    populateRentalSelect(rentals);
                    checkFinesAlert();
                    pendingRequests.delete('GetRentals');
                } else if (data[0]?.UserId) { // Subscriptions
                    subscriptions = data;
                    pendingRequests.delete('GetSubscriptions');
                }
            } else if (response.startsWith('{')) { // Login response
                const user = JSON.parse(response);
                if (user.Id) {
                    currentUser = user;
                    isLibrarian = currentUser.Role === 'Librarian';
                    console.log(`Login successful, user: ${user.Username}, role: ${user.Role}`);
                    alert(`Добро пожаловать, ${user.Username}!`);
                    localStorage.setItem('currentUser', JSON.stringify(currentUser));
                    navigateTo('index.html');
                } else {
                    alert('Неверный логин или пароль');
                }
            } else {
                alert(response);
                // Проверяем, было ли действие, требующее обновления данных
                const actions = [
                    'Книга добавлена успешно',
                    'Книга обновлена успешно',
                    'Книга удалена успешно',
                    'Книга выдана',
                    'Книга возвращена',
                    'Аренда продлена',
                    'Пользователь зарегистрирован',
                    'Абонемент добавлен'
                ];
                if (actions.includes(response)) {
                    console.log('Action requires data refresh:', response);
                    pendingRequests.add('GetBooks');
                    pendingRequests.add('GetUsers');
                    pendingRequests.add('GetRentals');
                    pendingRequests.add('GetSubscriptions');
                    window.chrome.webview.postMessage({ Action: 'GetBooks' });
                    window.chrome.webview.postMessage({ Action: 'GetUsers' });
                    window.chrome.webview.postMessage({ Action: 'GetRentals' });
                    window.chrome.webview.postMessage({ Action: 'GetSubscriptions' });
                    // Удаляем запрос действия из pendingRequests
                    pendingRequests.delete(response.replace(/ .*/, '')); // Например, 'AddBook' из 'Книга добавлена успешно'
                }
            }
        } else {
            console.error('Unexpected response format:', response);
            alert('Неожиданный формат ответа от сервера');
        }
    } catch (error) {
        console.error('Error processing response:', error, response);
        alert(`Ошибка обработки ответа: ${error.message}`);
    }
});

if (window.location.pathname.endsWith('index.html')) {
    currentUser = JSON.parse(localStorage.getItem('currentUser'));
    console.log('Index page, currentUser:', currentUser);
    if (!currentUser) {
        console.log('No currentUser, redirecting to login.html');
        navigateTo('login.html');
    } else {
        isLibrarian = currentUser.Role === 'Librarian';
        document.getElementById('adminPanel').className = isLibrarian ? 'admin-panel active' : 'admin-panel';
        // Инициируем загрузку данных
        pendingRequests.add('GetBooks');
        pendingRequests.add('GetUsers');
        pendingRequests.add('GetRentals');
        pendingRequests.add('GetSubscriptions');
        window.chrome.webview.postMessage({ Action: 'GetBooks' });
        window.chrome.webview.postMessage({ Action: 'GetUsers' });
        window.chrome.webview.postMessage({ Action: 'GetRentals' });
        window.chrome.webview.postMessage({ Action: 'GetSubscriptions' });
    }
} else if (window.location.pathname.endsWith('login.html')) {
    currentUser = JSON.parse(localStorage.getItem('currentUser'));
    console.log('Login page, currentUser:', currentUser);
    if (currentUser) {
        console.log('User already logged in, redirecting to index.html');
        navigateTo('index.html');
    } else {
        localStorage.removeItem('currentUser');
    }
}