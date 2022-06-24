using System.Globalization;
using EPiServer;

namespace Playground.Features.Plugins.GetPagesOfTypePlugin.Models
{
    public class Page
    {
        public int ID { get; set; }

        public string Name { get; set; }

        public Url Url { get; set; }

        public CultureInfo Language { get; set; }
    }
}