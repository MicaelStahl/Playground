using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.PlugIn;
using EPiServer.Web.Routing;
using Playground.Features.Plugins.GetPagesOfTypePlugin.Models;
using Playground.Features.Plugins.GetPagesOfTypePlugin.ViewModels;
using PageType = Playground.Features.Plugins.GetPagesOfTypePlugin.Models.PageType;

namespace Playground.Features.Plugins.GetPagesOfTypePlugin.Controllers
{
    [Authorize(Roles = "CmsAdmins")]
    [GuiPlugIn(Area = PlugInArea.AdminMenu,
        Url = "/custom-plugins/getpagesoftype",
        DisplayName = "Get all pages of a page type",
        Description = "Plugin to retrieve all pages of a certain type")]
    public class GetPagesOfTypePluginController : Controller
    {
        private const string BusinessPath = "~/Business/Plugins/GetPagesOfTypePlugin/Views/GetPagesOfTypePlugin.cshtml";
        private const string FeaturePath = "~/Features/Plugins/GetPagesOfTypePlugin/Views/GetPagesOfTypePlugin.cshtml";

        private readonly IContentTypeRepository _contentTypeRepository;
        private readonly IContentModelUsage _contentModelUsage;
        private readonly IContentLoader _contentLoader;
        private readonly EditUrlResolver _editUrlResolver;

        public GetPagesOfTypePluginController(
            IContentTypeRepository contentTypeRepository,
            IContentModelUsage contentModelUsage,
            IContentLoader contentLoader,
            EditUrlResolver editUrlResolver)
        {
            _contentTypeRepository = contentTypeRepository;
            _contentModelUsage = contentModelUsage;
            _contentLoader = contentLoader;
            _editUrlResolver = editUrlResolver;
        }

        [HttpGet]
        public ActionResult Index()
        {
            var model = new GetPagesOfTypeViewModel
            {
                PageTypes = GetPageTypes()
            };

            return View(GetView(), model);
        }

        [HttpPost]
        public ActionResult Index(int? pageID)
        {
            var model = new GetPagesOfTypeViewModel
            {
                PageTypes = GetPageTypes(pageID),
            };

            if (pageID.HasValue)
            {
                model.Pages = GetPages(pageID.Value, model);
            }

            return View(GetView(), model);
        }

        private IView GetView()
        {
            // First check if engines finds it normally.
            // Then check for business folders
            // Lastly check for feature folders
            // If no one matches, then a developer needs to update a path for this one.
            var view = ViewEngines.Engines.FindPartialView(ControllerContext, "GetPagesOfTypePlugin.cshtml").View
                ?? ViewEngines.Engines.FindPartialView(ControllerContext, BusinessPath).View
                ?? ViewEngines.Engines.FindPartialView(ControllerContext, FeaturePath).View;

            return view;
        }

        private IReadOnlyList<Page> GetPages(int pageId, GetPagesOfTypeViewModel model)
        {
            var pages = new List<Page>();
            var pageType = _contentTypeRepository.Load(pageId);
            model.PageType = pageType.Name;
            var modelUsage = _contentModelUsage.ListContentOfContentType(pageType);

            var uniques = modelUsage.Select(x => new
                {
                    ContentLink = x.ContentLink.ToReferenceWithoutVersion(), x.LanguageBranch
                })
                .Distinct()
                .Select(x => _contentLoader.Get<PageData>(x.ContentLink))
                .ToList();

            foreach (var unique in uniques)
            {
                pages.Add(new Page
                {
                    ID = unique.ContentLink.ID,
                    Name = unique.Name,
                    Language = unique.Language,
                    Url = _editUrlResolver.GetEditViewUrl(unique.ContentLink, new EditUrlArguments()
                    {
                        Language = unique.Language
                    })
                });
            }

            return pages;
        }

        private IReadOnlyList<PageType> GetPageTypes(int? pageId = null)
        {
            var contentTypes = _contentTypeRepository.List().OfType<EPiServer.DataAbstraction.PageType>();
            var pageTypes = new List<PageType>();

            foreach (var contentType in contentTypes)
            {
                if (contentType.Name.Equals("SysRoot") || contentType.Name.Equals("SysRecycleBin"))
                {
                    continue;
                }

                var pageType = new PageType
                {
                    ID = contentType.ID,
                    Name = contentType.Name,
                    DisplayName = contentType.DisplayName,
                    Available = contentType.IsAvailable,
                    Checked = contentType.ID == pageId
                };

                pageTypes.Add(pageType);
            }

            return pageTypes;
        }
    }
}