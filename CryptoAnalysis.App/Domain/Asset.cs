namespace CryptoAnalysis.App.Domain
{
    public class Asset
    {
        public string Id { get; set; } = null!;
        public string Symbol { get; set; } = null!;
        public string Name { get; set; } = null!;
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    }
}