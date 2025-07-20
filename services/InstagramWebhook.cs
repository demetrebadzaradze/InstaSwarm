public class InstagramWebhook
{
    public string Object { get; set; }
    public List<Entry> Entry { get; set; }

    public void ListPropertiesInTerminal()
    {
        Console.WriteLine($"Object: {Object}");
        if (Entry != null)
        {
            foreach (var entry in Entry)
            {
                Console.WriteLine($"Entry ID: {entry.Id}, Time: {entry.Time}");
                if (entry.Messaging != null)
                {
                    foreach (var messaging in entry.Messaging)
                    {
                        Console.WriteLine($"Messaging - Sender ID: {messaging.Sender.Id}, Recipient ID: {messaging.Recipient.Id}, Timestamp: {messaging.Timestamp}, Message Text: {messaging.Message.Text}");
                    }
                }
                if (entry.Changes != null)
                {
                    foreach (var change in entry.Changes)
                    {
                        Console.WriteLine($"Change - Field: {change.Field}, Sender ID: {change.Value.Sender.Id}, Recipient ID: {change.Value.Recipient.Id}, Timestamp: {change.Value.Timestamp}, Message Text: {change.Value.Message.Text}");
                    }
                }
            }
        }
    }
}

public class Entry
{
    public string Id { get; set; }
    public long Time { get; set; }
    public List<Messaging>? Messaging { get; set; }
    public List<Change>? Changes { get; set; }
}

public class Change
{
    public string Field { get; set; }
    public Value Value { get; set; }
}

public class Value
{
    public User Sender { get; set; }
    public User Recipient { get; set; }
    public long Timestamp { get; set; }
    public Message Message { get; set; }
}

public class User
{
    public string Id { get; set; }
}

public class Message
{
    public string Mid { get; set; }
    public string Text { get; set; }
}

public class Messaging
{
    public User Sender { get; set; }
    public User Recipient { get; set; }
    public long Timestamp { get; set; }
    public Message Message { get; set; }
}