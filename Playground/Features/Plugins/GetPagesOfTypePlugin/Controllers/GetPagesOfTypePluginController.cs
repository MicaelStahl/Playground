using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.PlugIn;
using EPiServer.Web;
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

        private static readonly string[] _ignoredPageTypes = new[]
        {
            "SysRoot",
            "SysRecycleBin"
        };

        private readonly IContentTypeRepository _contentTypeRepository;
        private readonly IContentModelUsage _contentModelUsage;
        private readonly IContentLoader _contentLoader;
        private readonly EditUrlResolver _editUrlResolver;
        private readonly ISiteDefinitionRepository _siteDefinitionRepository;
        private readonly ISiteDefinitionResolver _siteDefinitionResolver;
        private readonly ILanguageBranchRepository _languageBranchRepository;

        public GetPagesOfTypePluginController(
            IContentTypeRepository contentTypeRepository,
            IContentModelUsage contentModelUsage,
            IContentLoader contentLoader,
            EditUrlResolver editUrlResolver,
            ISiteDefinitionRepository siteDefinitionRepository,
            ISiteDefinitionResolver siteDefinitionResolver,
            ILanguageBranchRepository languageBranchRepository)
        {
            _contentTypeRepository = contentTypeRepository;
            _contentModelUsage = contentModelUsage;
            _contentLoader = contentLoader;
            _editUrlResolver = editUrlResolver;
            _siteDefinitionRepository = siteDefinitionRepository;
            _siteDefinitionResolver = siteDefinitionResolver;
            _languageBranchRepository = languageBranchRepository;
        }

        [HttpGet]
        public ActionResult Index()
        {
            var model = new GetPagesOfTypeViewModel
            {
                PageTypes = GetPageTypes(),
                Sites = GetSites(),
                Languages = GetLanguages()
            };

            return View(GetView(), model);
        }

        [HttpPost]
        public ActionResult Index(int? pageID, string lang = "", Guid? siteID = null)
        {
            var model = new GetPagesOfTypeViewModel
            {
                PageTypes = GetPageTypes(pageID),
                Sites = GetSites(siteID),
                Languages = GetLanguages(lang),
                LangId = lang,
                SiteId = siteID
            };

            if (pageID.HasValue)
            {
                var language = new CultureInfo(lang);
                model.Pages = GetPages(pageID.Value, siteID, language, model);
            }

            return View(GetView(), model);
        }

        private IReadOnlyList<Language> GetLanguages(string lang = "")
        {
            var languageBranches = _languageBranchRepository.ListEnabled();
            var languages = new List<Language>();

            foreach (var branch in languageBranches)
            {
                languages.Add(new Language
                {
                    LanguageID = branch.LanguageID,
                    Name = branch.Name,
                    Active = branch.LanguageID == lang
                });
            }

            return languages;
        }

        private IReadOnlyList<Site> GetSites(Guid? siteId = null)
        {
            var definitions = _siteDefinitionRepository.List();
            var sites = new List<Site>();

            foreach (var definition in definitions)
            {
                sites.Add(new Site
                {
                    ID = definition.Id,
                    Name = definition.Name,
                    Active = definition.Id == siteId
                });
            }

            return sites;
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

        private IReadOnlyList<Page> GetPages(int pageId, Guid? siteId, CultureInfo language,
            GetPagesOfTypeViewModel model)
        {
            var pages = new List<Page>();
            var pageType = _contentTypeRepository.Load(pageId);
            model.PageType = pageType.Name;
            var modelUsage = _contentModelUsage.ListContentOfContentType(pageType);

            var uniques = modelUsage.Select(x => x.ContentLink.ToReferenceWithoutVersion())
                .Select(x => _contentLoader.Get<PageData>(x))
                .Distinct()
                .ToList();

            var pagesWithLanguages = new List<PageData>();

            foreach (var unique in uniques)
            {
                foreach (var existingLang in unique.ExistingLanguages)
                {
                    pagesWithLanguages.Add(_contentLoader.Get<PageData>(unique.ContentLink, existingLang));
                }
            }

            /* Invariant is the default culture used when "new CultureInfo(string.Empty);" is created*/
            if (!Equals(language, CultureInfo.InvariantCulture))
            {
                pagesWithLanguages = FilterPages(pagesWithLanguages, language);
            }

            if (siteId?.Equals(Guid.Empty) == false)
            {
                pagesWithLanguages = FilterPages(pagesWithLanguages, siteId);
            }

            foreach (var pageData in pagesWithLanguages)
            {
                pages.Add(new Page
                {
                    ID = pageData.ContentLink.ID,
                    Name = pageData.Name,
                    Language = pageData.Language,
                    Url = _editUrlResolver.GetEditViewUrl(pageData.ContentLink, new EditUrlArguments()
                    {
                        Language = pageData.Language
                    })
                });
            }

            return pages;
        }

        private List<PageData> FilterPages(List<PageData> uniques, CultureInfo language)
        {
            var pages = new List<PageData>();

            for (var i = 0; i < uniques.Count; i++)
            {
                var unique = uniques[i];

                if (unique.ExistingLanguages.Any(x => x.Equals(language)))
                {
                    pages.Add(unique);
                }
            }

            return pages;
        }

        private List<PageData> FilterPages(List<PageData> uniques, Guid? siteId)
        {
            var pages = new List<PageData>();

            for (var i = 0; i < uniques.Count; i++)
            {
                var unique = uniques[i];

                var definition = _siteDefinitionResolver.GetByContent(unique.ContentLink, false);

                if (definition != null && siteId.Equals(definition.Id))
                {
                    pages.Add(unique);
                }
            }

            return pages;
        }

        private IReadOnlyList<PageType> GetPageTypes(int? pageId = null)
        {
            var contentTypes = _contentTypeRepository.List().OfType<EPiServer.DataAbstraction.PageType>();
            var pageTypes = new List<PageType>();

            foreach (var contentType in contentTypes)
            {
                if (_ignoredPageTypes.Contains(contentType.Name))
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