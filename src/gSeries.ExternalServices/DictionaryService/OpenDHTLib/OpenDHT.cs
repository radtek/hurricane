/* 
OpenDHT.NET library, based on XML-RPC.NET
Copyright (c) 2006, Michel Foucault <mmarsu@gmail.com>

Permission is hereby granted, free of charge, to any person 
obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without restriction, 
including without limitation the rights to use, copy, modify, merge, 
publish, distribute, sublicense, and/or sell copies of the Software, 
and to permit persons to whom the Software is furnished to do so, 
subject to the following conditions:

The above copyright notice and this permission notice shall be 
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Text;
using System.Collections;
using System.Security.Cryptography;
using CookComputing.XmlRpc;

namespace OpenDHTLib
{
    public class OpenDHT : OpenDHTBase
    {
        /// <summary>
        /// String Deserialization
        /// </summary>
        /// <param name="data">UTF-8 encoded string</param>
        /// <returns></returns>
        protected override object GetValue(byte[] data)
        {
            return GetString(data);
        }

        private static string[] GetValues(object[] values)
        {
            ArrayList ret = new ArrayList();
            ret.AddRange(values);
            return (string[])ret.ToArray(typeof(string));
        }

        protected static OpenDHTMessageDetails[] GetDetailsValues(object[] values)
        {
            ArrayList ret = new ArrayList();
            ret.AddRange(values);
            return (OpenDHTMessageDetails[])ret.ToArray(typeof(OpenDHTMessageDetails));
        }

        public string[] GetStringValues(string key)
        {
            return OpenDHT.GetValues(GetValues(key));
        }

        public OpenDHTMessageDetails[] GetDetailsStringValues(string key)
        {
            return OpenDHT.GetDetailsValues(GetDetailsValues(key));
        }
    }
}
