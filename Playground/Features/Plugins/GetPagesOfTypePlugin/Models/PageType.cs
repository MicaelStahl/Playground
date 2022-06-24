using System.Diagnostics;

namespace Playground.Features.Plugins.GetPagesOfTypePlugin.Models
{
    [DebuggerDisplay("ID = {ID}, Name = {Name}, Display name = {DisplayName ?? \"\"}, Available = {Available}, Checked = {Checked}")]
    public class PageType
    {
        public int ID { get; set; }

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public bool Available { get; set; }

        public bool Checked { get; set; }
    }
}