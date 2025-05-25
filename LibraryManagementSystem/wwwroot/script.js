let isLibrarian = false;
let currentUser = null;
let books = [];
let users = [];
let rentals = [];
let subscriptions = [];
let isTableView = false;
let pendingRequests = new Set();
let isAdminPanelCollapsed = false;

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
    const loader = document.getElementById('loginLoader');
    if (loader) loader.classList.add('active');
    console.log(`Sending login request for username: ${username}`);
    pendingRequests.add('LoginUser');
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
    window.chrome.webview.postMessage({ Action: 'ExitApplication' });
}

function toggleAdminPanel() {
    const adminPanel = document.getElementById('adminPanel');
    const toggleButton = document.querySelector('.toggle-admin');
    const toggleIcon = toggleButton.querySelector('i');
    isAdminPanelCollapsed = !isAdminPanelCollapsed;
    if (isAdminPanelCollapsed) {
        adminPanel.classList.add('collapsed');
        toggleButton.innerHTML = '<i class="fas fa-chevron-down"></i> Развернуть';
    } else {
        adminPanel.classList.remove('collapsed');
        toggleButton.innerHTML = '<i class="fas fa-chevron-up"></i> Свернуть';
    }
    console.log('Admin panel collapsed:', isAdminPanelCollapsed);
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

    setTimeout(() => {
        if (pendingRequests.has('UpdateBook')) {
            console.error('UpdateBook request timed out after 10 seconds');
            alert('Сервер не ответил на запрос обновления книги. Изменения могут быть не сохранены.');
            pendingRequests.delete('UpdateBook');
        }
    }, 10000);
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

function populateRentalTable(rentals) {
    const rentalTableBody = document.getElementById('rentalTableBody');
    if (rentalTableBody) {
        rentalTableBody.innerHTML = '';
        rentals.forEach(rental => {
            const book = books.find(b => b.Id === rental.BookId);
            const user = users.find(u => u.Id === rental.UserId);
            const dueDate = new Date(rental.DueDate);
            const now = new Date();
            const status = rental.ReturnDate ? 'Возвращена' : (dueDate < now ? 'Просрочена' : 'Активна');
            const statusColor = rental.ReturnDate ? '#4caf50' : (dueDate < now ? '#cc0000' : '#cccccc');
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${escapeHtml(book?.Title || 'Книга')}</td>
                <td>${escapeHtml(user?.Username || 'Пользователь')}</td>
                <td>${new Date(rental.RentDate).toLocaleDateString()}</td>
                <td>${dueDate.toLocaleDateString()}</td>
                <td>${rental.Fine} руб.</td>
                <td style="color: ${statusColor}">${status}</td>
            `;
            rentalTableBody.appendChild(row);
        });
    }
}

function populateSubscriptionTable(subscriptions) {
    const subscriptionTableBody = document.getElementById('subscriptionTableBody');
    if (subscriptionTableBody) {
        subscriptionTableBody.innerHTML = '';
        subscriptions.forEach(subscription => {
            const user = users.find(u => u.Id === subscription.UserId);
            const endDate = new Date(subscription.EndDate);
            const now = new Date();
            const status = endDate < now ? 'Истёк' : 'Активен';
            const statusColor = endDate < now ? '#cc0000' : '#4caf50';
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${escapeHtml(user?.Username || 'Пользователь')}</td>
                <td>${new Date(subscription.StartDate).toLocaleDateString()}</td>
                <td>${endDate.toLocaleDateString()}</td>
                <td style="color: ${statusColor}">${status}</td>
            `;
            subscriptionTableBody.appendChild(row);
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

function updateBookPreview() {
    const preview = document.getElementById('bookPreview');
    if (!preview) return;
    const title = document.getElementById('title').value || 'Название';
    const author = document.getElementById('author').value || 'Автор';
    const year = document.getElementById('year').value || 'Год';
    let coverUrl = document.getElementById('coverUrl').value;
    const fallbackUrl = 'https://sun9-65.userapi.com/impg/jXIX0kvvtGOtQncRawg45DSBywg4AFQ-2StOkA/tkUDIwtDgbo.jpg?size=600x400&quality=95&sign=f5d46ed155ad3f51c3fec039e66ec6b5&type=album';
    coverUrl = (coverUrl && isValidUrl(coverUrl)) ? coverUrl : fallbackUrl;
    const description = document.getElementById('description').value || 'Описание отсутствует';
    const genre = document.getElementById('genre').value || '';
    const isbn = document.getElementById('isbn').value || '';
    preview.innerHTML = `
        <img src="${coverUrl}" alt="${escapeHtml(title)}" onerror="this.src='${fallbackUrl}'; console.error('Image load failed, using fallback:', '${coverUrl}')">
        <h3>${escapeHtml(title)}</h3>
        <p>${escapeHtml(author)} (${year})</p>
        ${genre ? `<p>Жанр: ${escapeHtml(genre)}</p>` : ''}
        ${isbn ? `<p>ISBN: ${escapeHtml(isbn)}</p>` : ''}
        <p>Доступна</p>
        <p class="description">${escapeHtml(description)}</p>
    `;
}

function openEditModal(bookId) {
    const book = books.find(b => b.Id === bookId);
    if (!book) {
        console.error(`Book with ID ${bookId} not found`);
        return;
    }
    const modal = document.getElementById('editModal');
    document.getElementById('modalBookId').value = book.Id;
    document.getElementById('modalTitle').value = book.Title;
    document.getElementById('modalAuthor').value = book.Author;
    document.getElementById('modalYear').value = book.Year;
    document.getElementById('modalCoverUrl').value = book.CoverUrl || '';
    document.getElementById('modalDescription').value = book.Description || '';
    document.getElementById('modalGenre').value = book.Genre || '';
    document.getElementById('modalISBN').value = book.ISBN || '';
    modal.style.display = 'flex';
}

function closeEditModal() {
    const modal = document.getElementById('editModal');
    modal.style.display = 'none';
}

function saveBookFromModal() {
    if (!isLibrarian) {
        alert('Только библиотекари могут редактировать книги');
        return;
    }
    const bookId = parseInt(document.getElementById('modalBookId').value);
    const updatedBook = {
        Id: bookId,
        Title: document.getElementById('modalTitle').value,
        Author: document.getElementById('modalAuthor').value,
        Year: parseInt(document.getElementById('modalYear').value),
        CoverUrl: document.getElementById('modalCoverUrl').value,
        Description: document.getElementById('modalDescription').value,
        Genre: document.getElementById('modalGenre').value,
        ISBN: document.getElementById('modalISBN').value,
        IsAvailable: books.find(b => b.Id === bookId)?.IsAvailable ?? true
    };
    if (!updatedBook.Title || !updatedBook.Author || !updatedBook.Year) {
        alert('Заполните обязательные поля: название, автор, год');
        return;
    }
    if (updatedBook.CoverUrl && !isValidUrl(updatedBook.CoverUrl) && !updatedBook.CoverUrl.startsWith('images/')) {
        alert('Некорректный URL обложки');
        return;
    }
    console.log('Updating book from modal, sending to server:', updatedBook);

    const bookIndex = books.findIndex(b => b.Id === bookId);
    if (bookIndex !== -1) {
        books[bookIndex] = updatedBook;
        displayBooks(books);
        populateBookSelect(books);
        populateEditBookForm();
    }

    pendingRequests.add('UpdateBook');
    window.chrome.webview.postMessage({ Action: 'UpdateBook', Data: updatedBook });

    setTimeout(() => {
        if (pendingRequests.has('UpdateBook')) {
            console.error('UpdateBook request timed out after 10 seconds');
            alert('Сервер не ответил на запрос обновления книги. Изменения могут быть не сохранены.');
            pendingRequests.delete('UpdateBook');
        }
    }, 10000);

    closeEditModal();
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
            card.className = 'book-card fade-in';
            const img = document.createElement('img');
            const fallbackUrl = 'https://sun9-65.userapi.com/impg/jXIX0kvvtGOtQncRawg45DSBywg4AFQ-2StOkA/tkUDIwtDgbo.jpg?size=600x400&quality=95&sign=f5d46ed155ad3f51c3fec039e66ec6b5&type=album';
            const src = book.CoverUrl && book.CoverUrl.startsWith('images/')
                ? book.CoverUrl
                : (book.CoverUrl || fallbackUrl);
            img.src = src;
            img.alt = escapeHtml(book.Title);
            img.onerror = () => {
                console.error(`Failed to load image: ${img.src}, using fallback`);
                img.src = fallbackUrl;
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
            row.className = 'fade-in';
            const fallbackUrl = 'https://sun9-65.userapi.com/impg/jXIX0kvvtGOtQncRawg45DSBywg4AFQ-2StOkA/tkUDIwtDgbo.jpg?size=600x400&quality=95&sign=f5d46ed155ad3f51c3fec039e66ec6b5&type=album';
            const coverUrl = book.CoverUrl || fallbackUrl;
            row.innerHTML = `
                <td><img src="${coverUrl}" alt="${escapeHtml(book.Title)}" style="max-width: 60px;" onerror="this.src='${fallbackUrl}'; console.error('Image load failed in table, using fallback:', '${coverUrl}')"></td>
                <td>${escapeHtml(book.Title)}</td>
                <td>${escapeHtml(book.Author)}</td>
                <td>${book.Year}</td>
                <td>${escapeHtml(book.Genre || '')}</td>
                <td>${escapeHtml(book.ISBN || '')}</td>
                <td style="color: ${book.IsAvailable ? '#4caf50' : '#cc0000'}">${book.IsAvailable ? 'Доступна' : 'Выдана'}</td>
                <td>${escapeHtml(book.Description || '')}</td>
                <td>
                    ${isLibrarian ? `<button class="btn btn-primary btn-small" onclick="openEditModal(${book.Id})"><i class="fas fa-edit"></i> Редактировать</button>` : ''}
                </td>
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

function switchTab(tabName) {
    const buttons = document.querySelectorAll('.tab-button');
    const panels = document.querySelectorAll('.panel');
    let anyActive = false;
    buttons.forEach(btn => {
        const isActive = btn.getAttribute('data-tab') === tabName;
        btn.classList.toggle('active', isActive);
        if (isActive) anyActive = true;
    });
    panels.forEach(panel => {
        const isActive = panel.getAttribute('data-tab') === tabName;
        panel.classList.toggle('active', isActive);
    });
    if (!anyActive) {
        buttons.forEach(btn => btn.classList.remove('active'));
        panels.forEach(panel => panel.classList.remove('active'));
    }
}

window.chrome.webview.addEventListener('message', event => {
    const response = event.data.Response;
    console.log('Received response from server:', response);
    try {
        if (typeof response === 'string') {
            if (response.startsWith('[')) {
                const data = JSON.parse(response);
                if (data[0]?.Title) { // Books
                    if (Array.isArray(data) && data.length > 0) {
                        books = data;
                        localStorage.setItem('books', JSON.stringify(books));
                        displayBooks(books);
                        populateBookSelect(books);
                        populateEditBookForm();
                        console.log('Books updated from server:', books);
                    } else {
                        console.warn('Received empty or invalid books data:', data);
                    }
                    pendingRequests.delete('GetBooks');
                } else if (data[0]?.Username) { // Users
                    users = data;
                    populateUserSelect(users);
                    pendingRequests.delete('GetUsers');
                } else if (data[0]?.BookId) { // Rentals
                    rentals = data;
                    populateRentalSelect(rentals);
                    populateRentalTable(rentals);
                    checkFinesAlert();
                    pendingRequests.delete('GetRentals');
                } else if (data[0]?.UserId) { // Subscriptions
                    subscriptions = data;
                    populateSubscriptionTable(subscriptions);
                    pendingRequests.delete('GetSubscriptions');
                }
            } else if (response.startsWith('{')) { // Login response
                const user = JSON.parse(response);
                const loader = document.getElementById('loginLoader');
                if (loader) loader.classList.remove('active');
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
                pendingRequests.delete('LoginUser');
            } else {
                alert(response);
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
                    console.log('Action confirmed by server:', response);
                    if (response === 'Книга обновлена успешно') {
                        closeEditModal();
                        pendingRequests.delete('UpdateBook');
                    }
                    pendingRequests.add('GetBooks');
                    pendingRequests.add('GetUsers');
                    pendingRequests.add('GetRentals');
                    pendingRequests.add('GetSubscriptions');
                    window.chrome.webview.postMessage({ Action: 'GetBooks' });
                    window.chrome.webview.postMessage({ Action: 'GetUsers' });
                    window.chrome.webview.postMessage({ Action: 'GetRentals' });
                    window.chrome.webview.postMessage({ Action: 'GetSubscriptions' });
                    pendingRequests.delete(response.replace(/ .*/, ''));
                } else if (!actions.includes(response)) {
                    console.error('Unexpected server response:', response);
                    alert('Сервер не подтвердил действие. Изменения могут быть не сохранены.');
                    pendingRequests.delete('UpdateBook');
                }
            }
        } else {
            console.error('Unexpected response format:', response);
            alert('Неожиданный формат ответа от сервера');
            pendingRequests.delete('UpdateBook');
        }
    } catch (error) {
        console.error('Error processing response:', error, 'Response:', response);
        alert(`Ошибка обработки ответа: ${error.message}`);
        pendingRequests.delete('UpdateBook');
        const loader = document.getElementById('loginLoader');
        if (loader) loader.classList.remove('active');
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
        console.log('User role:', currentUser.Role, 'isLibrarian:', isLibrarian);

        const adminPanel = document.getElementById('adminPanel');
        if (adminPanel) {
            adminPanel.className = isLibrarian ? 'admin-panel active' : 'admin-panel';
            console.log('Admin panel class set to:', adminPanel.className);
        } else {
            console.error('Admin panel element not found');
        }

        pendingRequests.add('GetBooks');
        pendingRequests.add('GetUsers');
        pendingRequests.add('GetRentals');
        pendingRequests.add('GetSubscriptions');
        window.chrome.webview.postMessage({ Action: 'GetBooks' });
        window.chrome.webview.postMessage({ Action: 'GetUsers' });
        window.chrome.webview.postMessage({ Action: 'GetRentals' });
        window.chrome.webview.postMessage({ Action: 'GetSubscriptions' });

        const tabButtons = document.querySelectorAll('.tab-button');
        tabButtons.forEach(button => {
            button.addEventListener('click', () => {
                const tabName = button.getAttribute('data-tab');
                const isActive = button.classList.contains('active');
                switchTab(isActive ? null : tabName);
            });
        });

        const addBookInputs = document.querySelectorAll('#addBookForm input');
        addBookInputs.forEach(input => {
            input.addEventListener('input', updateBookPreview);
        });
        updateBookPreview();
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