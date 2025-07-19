namespace InstaSwarm.services
{
    public class InstagramWebhook
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
        public int Id { get; set; }
    }
    public class Message
    {
        public string Mid { get; set; }
        public string Text { get; set; }
    }
}