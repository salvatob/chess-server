document.getElementById('sendButton').addEventListener('click', sendMessage)
document.getElementById('messageInput').addEventListener('keydown', async event => {
    if (event.key === 'Enter') {
        await sendMessage();
    }
});
async function sendMessage(){
    const input = document.getElementById('messageInput');
    const message = input.value.trim();
    if (message.length > 0) {
        const id = await postMessage(message) // id is not needed in this case
        input.value = '';

        await updateMessages()
    }    
}


const globalMessagesArray = []



async function updateMessages() {
    const newMessages = await requestAllMessages()
    globalMessagesArray.length = 0
    newMessages.forEach(m => globalMessagesArray.push(m))
    renderMessages(globalMessagesArray)
}

let updateInterval = setInterval(updateMessages, 1000)