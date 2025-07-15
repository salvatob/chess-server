class MessageDTO {
    constructor(messageBody) {
        this.Body = messageBody
        this.Sender = "Default Sender Name"
    }
}


async function postMessage(text) {
    const message = new MessageDTO(text)
    const response = await fetch('/messenger/new-message', {
        method: 'POST', // Use POST to send data
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(message)
    });

    if (!response.ok) {
        console.error('Failed to send message');
        return;
    }

    const result = await response.json();
    console.log('Server response:', result);
    return await result
}



async function requestMessageById(id) {
    const response = await fetch("messenger/messages/"+id.toString())
    if (!response.ok) {
        const errorMessage = await response.json()
        console.error(errorMessage)
        return
    }
    const messageDto = await response.json()
    return Message.fromJSON(messageDto)
}

async function requestLastNMessages(count) {
    const response = fetch("/messenger/last-messages/"+count.toString())
    if (!response.ok) {
        const errorMessage = await response.json()
        console.error(errorMessage)
        return
    }
    const DTOArray = await response.json()
    const messageArray = DTOArray.map(raw=>Message.fromJSON(raw))
}

async function requestAllMessages() {
    const response = await fetch("/messenger/all")
    if (!response.ok) {
        const errorMessage = await response.json()
        console.error("I think we have an error here")
        console.log(errorMessage)
        return
    }
    const DTOArray = await response.json()
    return DTOArray.map(raw => Message.fromJSON(raw))
} 

function addMessageToWindow(messageObj) {
    const messageWindow = document.getElementById('messageWindow');

    const messageDiv = document.createElement('div');
    messageDiv.classList.add('message');
    // if (messageObj.fromSelf) {
    if (Math.random() > 0.5) {
        messageDiv.classList.add('self');
    } else {
        messageDiv.classList.add('other');
    }

    messageDiv.textContent = messageObj.body;
    messageWindow.appendChild(messageDiv);
    messageWindow.scrollTop = messageWindow.scrollHeight;
}



function renderMessages(messageArray) {
    const messageWindow = document.getElementById('messageWindow');
    messageWindow.innerHTML = ''; // Clear previous messages
    messageArray.forEach(msg => addMessageToWindow(msg));
}