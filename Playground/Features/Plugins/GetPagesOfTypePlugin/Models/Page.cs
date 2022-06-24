using System.Diagnostics;
using System.Globalization;
using EPiServer;

namespace Playground.Features.Plugins.GetPagesOfTypePlugin.Models
{
    [DebuggerDisplay("Id = {ID}, Name = {Name}, Language = {Language.Name}")]
    public class Page
    {
        public int ID { get; set; }

        public string Name { get; set; }

        public Url Url { get; set; }

        public CultureInfo Language { get; set; }
    }
}