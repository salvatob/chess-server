using System.Collections.Concurrent;

namespace App;


sealed class ConcurrentMessengerCollection {
  private ConcurrentDictionary<int, Message> _messagesList = [];
  private class UniqueId {
    private int _idSeed = 0;
    public int Get() => _idSeed++;
  }

  private readonly UniqueId _idCreator = new();
  
  private int GetUniqueId() => _idCreator.Get();

  public int Count => _messagesList.Count;

  public Message[] GetAllMessages() {
    return _messagesList.Values.ToArray();
  }
  
  /// <summary>
  /// Creates a message object and stores it.
  /// </summary>
  /// <param name="text">The message content.</param>
  /// <param name="sender">Sender of the message.</param>
  /// <returns>The ID value of the created message</returns>
  public int AddNewMessage(string text, string sender="unknown") {
    int msgID = GetUniqueId();
    Message msg = new Message {
      Body = text,
      Id = msgID,
      SenderName = sender,
    };

    //TODO finish logic of id collision
    
    if (!_messagesList.TryAdd(msgID, msg)) {
      
    }

    return msgID;
  }

  // /// <inheritdoc cref="AddNewMessage(string,string)" select="summary" />
  public int AddNewMessage(MessageClientDTO serverDto) => AddNewMessage(serverDto.Body, serverDto.Sender);
  
  public Message? GetMessage(int messageId) {
    return _messagesList.GetValueOrDefault(messageId);
  }

  
  
  public IEnumerable<Message> GetLastNMessages(int lastNMessages) {
    return _messagesList.Values.OrderDescending().Take(lastNMessages);
  }
  
  
  public async Task AsyncNothing() {
    await Task.CompletedTask;
  }
  
  
}

