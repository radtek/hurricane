//
// AnnounceHandler.cs
//
// Authors:
//   Gregor Burger gregor.burger@gmail.com
//
// Copyright (C) 2006 Gregor Burger
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Web;
using System.Diagnostics;
using MonoTorrent.Common;

namespace FuseSolution.Tracker
{
    public class AnnounceHandler : IHttpHandler
    {   
        public void ProcessRequest(HttpContext context)
        {
            Tracker tracker = (Tracker) context.Application.Get("tracker");
            context.Response.ContentType = "text/plain";//it's not html
            context.Response.StatusCode = 200;
            try
            {                
                AnnounceParameters parm = new AnnounceParameterParser(context.Request.RawUrl).GetParameters();
                               
                if (parm.ip == null) {
                    parm.ip = context.Request.UserHostAddress;
                }                        
                                
                tracker.Announce(parm,context.Response.OutputStream);
            }
            catch (TrackerException e)
            {
                tracker.Failure(e.Message, context.Response.OutputStream);
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }
            catch (Exception e)
            {
                tracker.Failure("Unknown Parameter Format", context.Response.OutputStream);
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
                Debug.WriteLine(context.Request.RawUrl);
            }
            
            
        }
        
        public bool IsReusable  
        {
            get {
                return true;
            }
        }       
    }
    
}