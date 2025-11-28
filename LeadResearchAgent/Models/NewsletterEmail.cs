namespace LeadResearchAgent.Models
{
    public class NewsletterEmail
    {
        public string Subject { get; set; } = "";
        public string Content { get; set; } = "";
        public string Sender { get; set; } = "";
        public DateTime ReceivedDate { get; set; }
        public string MessageId { get; set; } = "";
    }
}
