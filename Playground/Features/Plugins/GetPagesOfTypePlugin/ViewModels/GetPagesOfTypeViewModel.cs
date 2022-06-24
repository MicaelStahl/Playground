using System.Collections.Generic;
using Playground.Features.Plugins.GetPagesOfTypePlugin.Models;

namespace Playground.Features.Plugins.GetPagesOfTypePlugin.ViewModels
{
    public class GetPagesOfTypeViewModel
    {
        public IReadOnlyList<Page> Pages { get; set; }

        public string PageType { get; set; }

        public IReadOnlyList<PageType> PageTypes { get; set; }
    }
}