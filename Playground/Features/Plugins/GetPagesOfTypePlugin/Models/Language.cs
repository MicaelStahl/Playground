using System.Diagnostics;

namespace Playground.Features.Plugins.GetPagesOfTypePlugin.Models
{
    [DebuggerDisplay("ID = {LanguageID}, Name = {Name}, Active = {Active}")]
    public class Language
    {
        public string LanguageID { get; set; }

        public string Name { get; set; }

        public bool Active { get; set; }
    }
}