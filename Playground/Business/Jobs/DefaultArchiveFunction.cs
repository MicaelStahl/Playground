using System;
using System.Globalization;
using System.Text;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAccess;
using EPiServer.Filters;
using EPiServer.Logging;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.Security;

namespace Playground.Business.Jobs
{
    [ScheduledPlugIn(DisplayName = "Default Archive Function",
        Description = "Used as a replacement to the original Archive Function.\n" +
            "This job does not republish pages upon archiving.")]
    public class DefaultArchiveFunction : ScheduledJobBase
    {
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(DefaultArchiveFunction));
        private readonly StringBuilder _builder = new StringBuilder();

        private readonly IContentRepository _contentRepository;
        private readonly IPageCriteriaQueryService _pageCriteriaService;
        private readonly ContentTypeAvailabilityService _availabilityService;

        public DefaultArchiveFunction(IContentRepository contentRepository,
            IPageCriteriaQueryService pageCriteriaService, ContentTypeAvailabilityService availabilityService)
        {
            _contentRepository = contentRepository;
            _pageCriteriaService = pageCriteriaService;
            _availabilityService = availabilityService;
        }

        public override string Execute()
        {
            var moved = 0;
            var failed = 0;

            var pageStopPublishCriteria = new PropertyCriteria
            {
                Name = "PageStopPublish",
                Value = DateTime.Now.ToString(CultureInfo.InvariantCulture),
                Type = PropertyDataType.Date,
                Required = true,
                Condition = CompareCondition.LessThan
            };

            var pageArchiveLinkCriteria = new PropertyCriteria
            {
                Name = "PageArchiveLink",
                Value = null,
                IsNull = true,
                Type = PropertyDataType.PageReference,
                Required = true,
                Condition = CompareCondition.NotEqual
            };

            var criterias = new PropertyCriteriaCollection
            {
                pageStopPublishCriteria,
                pageArchiveLinkCriteria,
                new PropertyCriteria
                {
                    Name = "EPI:MultipleSearch",
                    Value = "*"
                }
            };

            PageDataCollection pagesWithCriteria;

            try
            {
                pagesWithCriteria = _pageCriteriaService.FindAllPagesWithCriteria(ContentReference.RootPage, criterias,
                    null, LanguageSelector.MasterLanguage());
            }
            catch (Exception e)
            {
                _logger.Error(e.Message, e);

                pagesWithCriteria = _pageCriteriaService.FindPagesWithCriteria(ContentReference.RootPage, criterias,
                    null, LanguageSelector.MasterLanguage());
            }

            foreach (var pageData in pagesWithCriteria)
            {
                if (!pageData.PendingArchive)
                {
                    continue;
                }

                if (pageData.ArchiveLink.Equals(pageData.ContentLink))
                {
                    Log(
                        $"The archive link for page with ID {pageData.ContentLink.ID}" +
                        " references itself which is not allowed.");

                    continue;
                }

                if (pageData.ArchiveLink.Equals(pageData.ParentLink))
                {
                    Log(
                        $"The archive link for page with ID {pageData.ContentLink.ID}" +
                        " references its parent which is not allowed.");

                    continue;
                }

                var archivePage = _contentRepository.Get<PageData>(pageData.ArchiveLink);

                if (!_availabilityService.IsAllowed(pageData.PageTypeName, archivePage.PageTypeName))
                {
                    Log($"{pageData.PageTypeName} (Page with ID: {pageData.ContentLink.ID}) is" +
                        $" not allowed as children of {archivePage.PageTypeName}");

                    continue;
                }

                try
                {
                    _contentRepository.Move(pageData.ContentLink, archivePage.ContentLink,
                        AccessLevel.NoAccess, AccessLevel.NoAccess);

                    pageData.StopPublish = DateTime.Now;
                    _contentRepository.Save(pageData, SaveAction.Publish, AccessLevel.NoAccess);
                }
                catch (Exception)
                {
                    var msg = $"Failed to move page '{pageData.ContentLink}' to archive '{archivePage.ContentLink}'";

                    _logger.Error(msg);
                    Log(msg);

                    failed++;
                }
            }

            LogInsert(0, $"Failed to move {failed}pages, see log for details.");
            LogInsert(0, $"{moved} pages were moved to their archive folder.");

            return _builder.ToString();
        }

        private void LogInsert(int index, string message) => _builder.Insert(index, $"{message}<br />");

        private void Log(string message) => _builder.Append(message).Append("<br />");
    }
}