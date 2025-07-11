using System.Collections.Concurrent;

namespace App;


class ConcurrentMessengerCollection {
  private List<Message> _messagesList = [];
  private readonly object _syncLock = new();

  public int Count {
    get {
      lock (_syncLock) 
        return _messagesList.Count;
    }
  }

  public Message this[int index] {
    get {
      lock (_syncLock) 
        return _messagesList[index];
    }
    set {
      lock (_syncLock) 
        _messagesList[index] = value;
    }
  }
  
  public Message[] GetAllMessages() {
    lock (_syncLock) {
      return _messagesList.ToArray();
    }
  }

  public void AddMessage(Message message) {
    lock (_syncLock) {
      _messagesList.Add(message);
    }
  }

  public Message? GetMessage(int messageId) {
    lock (_syncLock) {
      if (_messagesList[messageId].Id == messageId)
        return _messagesList[messageId];
      
      return _messagesList.Find(m => m.Id == messageId);
    }
  } 

  public async Task AsyncNothing() {
    await Task.CompletedTask;
    return;
  }
  
}