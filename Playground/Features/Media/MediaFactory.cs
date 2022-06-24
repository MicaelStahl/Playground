using System.IO;
using System.Web;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Framework.Blobs;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;

namespace Playground.Features.Media
{
    [ServiceConfiguration(FactoryType = typeof(IMediaFactory), Lifecycle = ServiceInstanceScope.Singleton)]
    internal sealed class MediaFactory : IMediaFactory
    {
        private readonly IBlobFactory _blobFactory;
        private readonly IContentRepository _contentRepository;

        public MediaFactory(IBlobFactory blobFactory, IContentRepository contentRepository)
        {
            _blobFactory = blobFactory;
            _contentRepository = contentRepository;
        }

        public T CreateMedia<T>(
            HttpPostedFile postedFile,
            ContentReference parentLink = null,
            bool globalAssetFallback = true)
            where T : MediaData
        {
            if (postedFile == null || postedFile.ContentLength == default)
            {
                return default;
            }

            return CreateMediaInternal<T>(
                parentLink,
                postedFile.FileName,
                globalAssetFallback,
                postedFile.InputStream);
        }

        public T CreateMedia<T>(
            HttpPostedFileBase postedFileBase,
            ContentReference parentLink = null,
            bool globalAssetFallback = true)
            where T : MediaData
        {
            if (postedFileBase == null || postedFileBase.ContentLength == default)
            {
                return default;
            }

            return CreateMediaInternal<T>(
                parentLink,
                postedFileBase.FileName,
                globalAssetFallback,
                postedFileBase.InputStream);
        }

        private T CreateMediaInternal<T>(
            ContentReference parentLink,
            string fileName,
            bool globalAssetFallback,
            Stream blobStream)
            where T : MediaData
        {
            var genericMedia = CreateDefaultMedia<T>(parentLink, fileName, globalAssetFallback);
            var blob = _blobFactory.CreateBlob(genericMedia.BinaryDataContainer, Path.GetExtension(fileName));

            blob.Write(blobStream);

            genericMedia.BinaryData = blob;
            _contentRepository.Save(genericMedia, SaveAction.Publish, AccessLevel.NoAccess);

            return genericMedia;
        }

        private T CreateDefaultMedia<T>(
            ContentReference parentLink,
            string fileName,
            bool useGlobalAsset)
            where T : MediaData
        {
            var reference = GetParentReference(parentLink, useGlobalAsset);
            var genericMedia = _contentRepository.GetDefault<T>(reference);
            genericMedia.Name = fileName;

            return genericMedia;
        }

        private ContentReference GetParentReference(ContentReference parentLink, bool useGlobalAsset)
        {
            if (!ContentReference.IsNullOrEmpty(parentLink))
            {
                return parentLink;
            }

            ContentReference reference;
            var currentSite = SiteDefinition.Current;

            switch (useGlobalAsset)
            {
                case true:
                    reference = currentSite.GlobalAssetsRoot;
                    break;
                case false:
                    reference = currentSite.SiteAssetsRoot;
                    break;
            }

            return reference;
        }
    }
}