namespace App;

public class MessageClientDTO {
    public string Body { get; set; }
    public string Sender { get; set; }
}

public class MessageServerDTO {
    public int Id { get; }
    public string Body { get; set; }
    public string Sender { get; set; }

    public MessageServerDTO(int id, string body, string sender) {
        Id = id;
        Body = body;
        Sender = sender;
    }

    public MessageServerDTO(Message message) {
        Id = message.Id;
        Body = message.Body;
        Sender = message.SenderName;
    }
}

public class Message : IComparable<Message> {
    public required int Id { get; init; }
    public DateTime TimeSent { get;}
    public required string SenderName { get; set; }
    public required string Body { get; set; }
    public bool Deleted { get; set; } = false;

    public Message() {
        TimeSent = DateTime.Now;
    }
    
    public Message(MessageServerDTO serverDto) : this(){
        Body = serverDto.Body;
        SenderName = serverDto.Sender;
    }
    

    public int CompareTo(Message? other) {
        ArgumentNullException.ThrowIfNull(other,nameof(other));

        return this.TimeSent.CompareTo(other.TimeSent);
    }

    public MessageServerDTO ToDto() => new MessageServerDTO(this);
    
    public override string ToString() {
        return $"Message :\"{Body}\" from : \"{SenderName}\"";
    }
}

public static class MessageIEnumerableExtensions {
    public static IEnumerable<MessageServerDTO> ToDto(this IEnumerable<Message> iEnumerable) {
        return iEnumerable.Select(m => m.ToDto());
    }
}