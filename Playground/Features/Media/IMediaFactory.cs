using System.Web;
using EPiServer.Core;

namespace Playground.Features.Media
{
    public interface IMediaFactory
    {
        /// <summary>
        /// Creates a <typeparamref name="T"/> given a <see cref="HttpPostedFile"/>
        /// </summary>
        /// <param name="postedFile">The posted file from a client</param>
        /// <param name="parentLink">The parent reference.</param>
        /// <param name="globalAssetFallback">If <paramref name="parentLink"/> is null, this will annotate whether to use global asset folder or site asset folder.</param>
        /// <typeparam name="T">The media type to attempt to create</typeparam>
        /// <returns>Returns a new instance of <typeparamref name="T"/> or it's default value</returns>
        T CreateMedia<T>(
            HttpPostedFile postedFile,
            ContentReference parentLink = null,
            bool globalAssetFallback = true)
            where T : MediaData;

        /// <summary>
        /// Creates a <typeparamref name="T"/> given a <see cref="HttpPostedFile"/>
        /// </summary>
        /// <param name="postedFileBase">The posted file from a client</param>
        /// <param name="parentLink">The parent reference.</param>
        /// <param name="globalAssetFallback">If <paramref name="parentLink"/> is null, this will annotate whether to use global asset folder or site asset folder.</param>
        /// <typeparam name="T">The media type to attempt to create</typeparam>
        /// <returns>Returns a new instance of <typeparamref name="T"/> or it's default value</returns>
        T CreateMedia<T>(
            HttpPostedFileBase postedFileBase,
            ContentReference parentLink = null,
            bool globalAssetFallback = true)
            where T : MediaData;
    }
}