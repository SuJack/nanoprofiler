/*
    The MIT License (MIT)
    Copyright © 2014 Englishtown <opensource@englishtown.com>

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/

using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;

using EF.Diagnostics.Profiling.Timing;

namespace EF.Diagnostics.Profiling.ServiceModel.Dispatcher
{
    /// <summary>
    /// The service endpoint message inspector for starting/stopping profiling for WCF service actions.
    /// </summary>
    public sealed class WcfProfilingDispatchMessageInspector : IDispatchMessageInspector
    {
        #region IDispatchMessageInspector Members

        object IDispatchMessageInspector.AfterReceiveRequest(
            ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            if (request == null || request.Headers == null || string.IsNullOrEmpty(request.Headers.Action))
            {
                return null;
            }

            var tags = GetRequestProfilingTags(request, channel);

            // fix properties for WCF profiling session
            var profilingSession = ProfilingSession.Current;
            if (profilingSession == null)
            {
                // start the profiling session if not started
                // NOTICE:
                //   When not set <serviceHostingEnvironment aspNetCompatibilityEnabled="true" /> in web.config,
                //   and if there is already a profiling session started in begin request event when working with HTTP bindings,
                //   since HttpContext.Current is not accessible from WCF context, there will be two profiling sessions
                //   be saved, one for the web request wrapping the WCF call and the other for the WCF call.
                ProfilingSession.Start(request.Headers.Action, tags == null ? null : TagCollection.FromString(tags).ToArray());
            }
            else
            {
                // if the profiling session has already been started as a normal web profiling session
                // we need to fix the properties of the profiling session first

                // reset ProfilingSessionContainer.CurrentSession
                // which internally tries to cache the profiling session in WcfContext
                // to ensure the profiling session is cached in WcfInstanceContext
                ProfilingSession.ProfilingSessionContainer.CurrentSession = profilingSession;
                
                // set profiler's name to the WCF action name
                profilingSession.Profiler.Name = request.Headers.Action;

                // merge tags
                MergeRequestTags(profilingSession, tags);
            }

            SetProfilingSessionClientIpAndLocalAddress(request, channel);

            return null;
        }

        void IDispatchMessageInspector.BeforeSendReply(ref Message reply, object correlationState)
        {
            ProfilingSession.Stop();
        }

        #endregion

        #region Private Methods

        private static string GetRequestProfilingTags(Message request, IClientChannel channel)
        {
            string tags = null;

            // try to get tags from headers for soap messages
            if (!Equals(request.Headers.MessageVersion, MessageVersion.None))
            {
                // Check to see if we have a request as part of this message
                var headerIndex = request.Headers.FindHeader(
                    WcfProfilingMessageHeaderConstants.HeaderNameOfProfilingTags
                    , WcfProfilingMessageHeaderConstants.HeaderNamespace);

                if (headerIndex >= 0)
                {
                    tags = request.Headers.GetHeader<string>(headerIndex);
                }
            }
            // else try to get tags from properties for web operation messages
            else if (WebOperationContext.Current != null || channel.Via.Scheme == "http" || channel.Via.Scheme == "https")
            {
                if (request.Properties.ContainsKey(HttpRequestMessageProperty.Name))
                {
                    var property = (HttpRequestMessageProperty)request.Properties[HttpRequestMessageProperty.Name];
                    tags = property.Headers[WcfProfilingMessageHeaderConstants.HeaderNameOfProfilingTags];
                }
            }
            return tags;
        }

        private static void MergeRequestTags(ProfilingSession profilingSession, string tags)
        {
            if (tags != null)
            {
                var tagCollection = TagCollection.FromString(tags);
                if (profilingSession.Profiler.Tags == null)
                {
                    profilingSession.Profiler.Tags = tagCollection;
                }
                else
                {
                    foreach (var tag in tagCollection)
                    {
                        profilingSession.Profiler.Tags.Add(tag);
                    }
                }
            }
        }

        private static void SetProfilingSessionClientIpAndLocalAddress(
            Message request, IClientChannel channel)
        {
            ProfilingSession profilingSession = ProfilingSession.Current;
            if (profilingSession != null)
            {
                // set local address
                profilingSession.Profiler.LocalAddress = channel.LocalAddress.Uri.ToString();

                // set client IP address
                if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
                {
                    var remoteEndpoint =
                        request.Properties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
                    if (remoteEndpoint != null)
                    {
                        profilingSession.Profiler.Client = remoteEndpoint.Address;
                    }
                }
            }
        }

        #endregion
    }
}