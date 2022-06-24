using System;
using System.Diagnostics;

namespace Playground.Features.Plugins.GetPagesOfTypePlugin.Models
{
    [DebuggerDisplay("ID = {ID}, Name = {Name}, Active = {Active}")]
    public class Site
    {
        public Guid ID { get; set; }

        public string Name { get; set; }

        public bool Active { get; set; }
    }
}