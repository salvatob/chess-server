
class Message {
    constructor(messageBody, sender, id) {
        this.body = messageBody
        this.sender = sender
        this.id = id
    }
    // Factory that takes any object, checks its fields, and returns a Message
    static fromJSON(raw) {
        if (
            typeof raw.body     !== 'string' ||
            typeof raw.sender   !== 'string' ||
            typeof raw.id       !== 'number'
        ) {
            throw new TypeError('Invalid Message payload');
        }
        return new Message(raw.body, raw.sender, raw.id);
    }
}
