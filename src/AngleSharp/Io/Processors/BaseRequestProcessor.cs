﻿namespace AngleSharp.Io.Processors
{
    using AngleSharp.Dom;
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    abstract class BaseRequestProcessor : IRequestProcessor
    {
        #region Fields

        private readonly IResourceLoader _loader;

        #endregion

        #region ctor

        public BaseRequestProcessor(IResourceLoader loader)
        {
            _loader = loader;
        }

        #endregion

        #region Properties

        public IDownload Download
        {
            get;
            protected set;
        }

        #endregion

        #region Methods

        public virtual Task ProcessAsync(ResourceRequest request)
        {
            if (_loader != null && IsDifferentToCurrentDownloadUrl(request.Target))
            {
                CancelDownload();
                Download = _loader.FetchAsync(request);
                return FinishDownloadAsync();
            }

            return null;
        }

        protected abstract Task ProcessResponseAsync(IResponse response);

        protected async Task FinishDownloadAsync()
        {
            var download = Download;
            var response = await download.Task.ConfigureAwait(false);
            var eventTarget = download.Source as EventTarget;
            var eventName = EventNames.Error;

            if (response != null)
            {
                try
                {
                    await ProcessResponseAsync(response).ConfigureAwait(false);
                    eventName = EventNames.Load;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
                finally
                {
                    response.Dispose();
                }
            }

            eventTarget?.FireSimpleEvent(eventName);
        }

        #endregion

        #region Helpers

        protected IDownload DownloadWithCors(CorsRequest request)
        {
            return _loader.FetchWithCorsAsync(request);
        }

        protected void CancelDownload()
        {
            var download = Download;

            if (download != null && !download.IsCompleted)
            {
                download.Cancel();
            }
        }

        protected Boolean IsDifferentToCurrentDownloadUrl(Url target)
        {
            var download = Download;
            return download == null || !target.Equals(download.Target);
        }

        #endregion
    }
}
