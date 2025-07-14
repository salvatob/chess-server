namespace App;

public class MessageDTO {
    public string Body { get; }
    public string Sender { get; }
}

public class Message : IComparable<Message> {
    public required int Id { get; init; }
    public DateTime TimeSent { get;}
    public required string? SenderName { get; set; }
    public required string Body { get; set; }
    public bool Deleted { get; set; } = false;

    public Message() {
        TimeSent = DateTime.Now;
    }
    
    public Message(MessageDTO dto) : this(){
        Body = dto.Body;
        SenderName = dto.Sender;
    }
    

    public int CompareTo(Message? other) {
        ArgumentNullException.ThrowIfNull(other,nameof(other));

        return this.TimeSent.CompareTo(other.TimeSent);
    }
}