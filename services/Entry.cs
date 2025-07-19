namespace InstaSwarm.services
{
    public class Entry
    {
        public string Id { get; set; }
        public long Time { get; set; }
        public List<Change> Changes { get; set; }
    }
}