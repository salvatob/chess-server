document.getElementById('sendButton').addEventListener('click', () => {
    const input = document.getElementById('messageInput');
    const message = input.value.trim();
    if (message) {
        addMessageToWindow(message);
        input.value = '';
        // You can add your send request logic here
    }
});

function addMessageToWindow(messageText, sender = 'You') {
    const messageWindow = document.getElementById('messageWindow');

    const messageDiv = document.createElement('div');
    messageDiv.classList.add('message');
    messageDiv.textContent = `${sender}: ${messageText}`;

    messageWindow.appendChild(messageDiv);
    messageWindow.scrollTop = messageWindow.scrollHeight;
}

// Example usage from server message:
// addMessageToWindow('Hello from server!', 'Server');
