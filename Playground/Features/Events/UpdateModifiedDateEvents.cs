using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Security;

namespace Playground.Features.Events
{
    // This event handler updates the PageData.Changed value when a block/media it references is updated.
    [InitializableModule]
    public class UpdateModifiedDateEvents : IInitializableModule
    {
        private bool _isInitialized;
        private static IContentRepository _contentRepository;

        public void Initialize(InitializationEngine context)
        {
            var events = context.Locate.ContentEvents();
            if (!_isInitialized)
            {
                _contentRepository = context.Locate.ContentRepository();

                _isInitialized = true;
            }

            events.PublishingContent += PublishingContent;
        }

        public void Uninitialize(InitializationEngine context)
        {
            var events = context.Locate.ContentEvents();
            if (_isInitialized)
            {
                _contentRepository = context.Locate.ContentRepository();

                _isInitialized = false;
            }

            events.PublishingContent -= PublishingContent;
        }

        private void PublishingContent(object sender, ContentEventArgs e)
        {
            // We're not interested in pages.
            // The Modified field can be automatically updated on page publish by going to
            // web.config => <episerver> => <applicationSettings /> and adding 'uIDefaultValueForSetChangedOnPublish="true"'
            if (e.Content is PageData)
            {
                return;
            }

            // We set 'includeDescendants' to false since blocks/documents/images/videos should not hold children.
            var references = new HashSet<ContentReference>(
                _contentRepository.GetReferencesToContent(e.ContentLink, false)
                    .Select(x => x.OwnerID));

            if (references.Count == 0)
            {
                return;
            }

            foreach (var reference in references)
            {
                if (_contentRepository.TryGet<PageData>(reference, out var page))
                {
                    page = page.CreateWritableClone();
                    page.SetChangedOnPublish = true;

                    _contentRepository.Save(page, SaveAction.Publish, AccessLevel.NoAccess);
                }
            }
        }
    }
}