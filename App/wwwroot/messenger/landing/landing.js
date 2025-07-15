// Redirect immediately if username is already stored
const existingUsername = localStorage.getItem('username');
if (existingUsername) {
    window.location.href = '/messenger';
}

const button = document.getElementById('startButton');
const input = document.getElementById('usernameInput');

button.addEventListener('click', () => {
    const username = input.value.trim();
    if (username) {
        localStorage.setItem('username', username);
        window.location.href = '/messenger';
    } else {
        alert('Please enter a username');
    }
});
