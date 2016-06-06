using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Net.Http.Headers;
using System.Net;
using System.Text.RegularExpressions;

namespace Repro.Tests
{
    public static class TestClientHelpers
    {
        /// <summary>
        /// Access a parameterless json-returning
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="content">The HttpContent to send with this request.</param>
        /// <param name="url"></param>
        /// <param name="assertOK">When true, the procedure will assert that a OK status code was returnednx </param>
        /// <returns></returns>        
        public static async Task<T> GetObjectFromJsonUrlAsync<T>(this HttpClient client, string url, bool assertOK = true)
        {
            var response = await client.GetAsync(url);
            if(assertOK)
            {
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            }
            else//if I'm not enforcing an OK, when i get a 404, return null
            {
                if(response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return default(T);
                }
            }



            
            var responseContent = await response.Content.ReadAsStringAsync();
            //cast to a client object and check all of the data to make sure it is the same
            T result = JsonConvert.DeserializeObject<T>(responseContent, new JsonSerializerSettings
            {  ReferenceLoopHandling = ReferenceLoopHandling.Ignore}

                );
            return result;

        }
        /// <summary>
        /// Post an object postContent as JSON to a url. Return the object type specified after deserializing from JSON
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="url"></param>
        /// <param name="postContent"></param>
        /// <returns></returns>
        public static async Task<T> GetObjectFromJsonUrlPost<T>(this HttpClient client, string url, object postContent)
        {
            //post as JSON , utf-8 application/json
            var result = await client.GetResponseFromJsonUrlPost(url, postContent);
            //ensure we got an OK
            Assert.Equal(result.StatusCode, System.Net.HttpStatusCode.OK);
            //get the string content
            string responseContent = await result.Content.ReadAsStringAsync();
            //deserialize into specified object type
            T responseObject = JsonConvert.DeserializeObject<T>(responseContent);
            //return said object
            return responseObject;
        }

        public static async Task<HttpResponseMessage> GetResponseFromJsonUrlPost(this HttpClient client, string url, object postContent)
        {
            return await client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(postContent), System.Text.Encoding.UTF8, "application/json"));
        }
        /// <summary>
        /// Post an object as form-encoded-data and return the httpresult
        /// 
        /// Adds the byte contents in fileContents as the file listed in fileName and posts this as "file" in the form post.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="url"></param>
        /// <param name="postContent"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> GetResponseFromFormEncodedPost(this HttpClient client, string url, object postContent)
        {
            return await GetResponseFromFormEncodedPostWithStream(client, url, postContent, null, null);
        }

        public static async Task<HttpResponseMessage> GetResponseFromFormEncodedPostWithStream(this HttpClient client, string url, object postContent,Stream filestream, string fileName, bool assertRedirect = true,SetCookieHeaderValue cookie = null)
        {
            HttpResponseMessage result;
            HttpContent content;
            if (filestream != null)
            {





                Dictionary<string, string> postFormValues = ConvertObjectToFormDictionary(postContent);
                var formContent = new FormUrlEncodedContent(postFormValues);
                postFormValues.Remove("File");
                string formDataBoundary = String.Format("----------{0:N}--", Guid.NewGuid());
                var mpContent = new MultipartFormDataContent(formDataBoundary);




                foreach (var k in postFormValues.Keys)
                {
                    var stringContent = new StringContent(postFormValues[k]);
                    stringContent.Headers.Add("Content-Disposition", "form-data; name=\"" + k + "\"");
                    mpContent.Add(stringContent);

                }


                mpContent.Headers.Add("NoRedirect", "true");


                var fileContent = new StreamContent(filestream);
                //fileContent.Headers.Add("Content-Disposition", "form-data; name=\"File\"");


                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                mpContent.Add(fileContent, "file", fileName);
                //fileContent.Headers.ContentLength = fileContents.Length;

                if (cookie != null)
                {
                    //mpContent.Headers.Add(cookie.Name, cookie.Value);
                    mpContent.Headers.Add("Cookie", new CookieHeaderValue(cookie.Name, cookie.Value).ToString());
                }
                result = await client.PostAsync(url, mpContent);
            }
            else
            {

                Dictionary<string, string> postFormValues = ConvertObjectToFormDictionary(postContent);
                //post the new create result
                content = new FormUrlEncodedContent(postFormValues);
                
                if(cookie != null)
                {
                    //   content.Headers.Add(cookie.Name,cookie.Value);
                    content.Headers.Add("Cookie",new CookieHeaderValue(cookie.Name, cookie.Value).ToString());
                }                
                result = await client.PostAsync(url, content);
            }
            if (assertRedirect)
            {
                Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
            }
            return result;
        }


        /// <summary>
        /// Login and return authentication cookie. This cookie must be passed with subsequent requests
        /// </summary>
        /// <param name="Client"></param>
        /// <returns></returns>
        public static async Task<SetCookieHeaderValue> Login(this HttpClient Client)
        {
            //pw for in-memory account, created in Startup.cs
            string pw = "QWErty!@#$%^12345";

            var result = await Client.GetAsync("/Account/Login");

            IEnumerable<string> values;

            result.Headers.TryGetValues("Set-Cookie", out values);

            Assert.NotNull(values);
            Assert.NotEmpty(values);
            //find the validation token
            //< input name = "__RequestVerificationToken" type = "hidden" value = "CfDJ8BQFQUk1PV9Fusy3dPMxl5Uu-rriBM3Ad5zCQGNf6fv_bkRBpTo7gfQAlYLPGyROR15d4NpLJ_AZN7mWKXKBqvOw-MAyMMEeLSeAeNadgxG_8sGgvJBYKGdtLn5PO9YO3jSNz_zQRuuTLjEmyTiPDCc" >
            string tokenPattern = "<input name=\\\"__RequestVerificationToken\\\" type=\\\"hidden\\\" value=\\\"([\\w\\W\\d\\D]*)\\\">";

            var body = await result.Content.ReadAsStringAsync();
            var match = Regex.Match(body, tokenPattern);
            Assert.NotEmpty(match.Groups[1].Value);
            //get request token to pass back to login
            var token = match.Groups[1].Value;

            var post = new
            {
                Password = pw,
                Email = "test@test.test",
                __RequestVerificationToken = token
            };

            HttpRequestMessage message = new HttpRequestMessage();


            result = await Client.GetResponseFromFormEncodedPost("/Account/Login", post, null, null, false);

            body = await result.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);

            //get the cookies sent in response from the login            
            Assert.True(result.Headers.TryGetValues("Set-Cookie", out values));
            Assert.NotNull(values);
            var cookies = SetCookieHeaderValue.ParseList(values.ToList());
            Assert.Equal(1, cookies.Count);


            return cookies.FirstOrDefault();

        }


        /// <summary>
        /// Post an object as form-encoded-data and return the httpresult
        /// 
        /// Adds the byte contents in fileContents as the file listed in fileName and posts this as "file" in the form post.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="url"></param>
        /// <param name="postContent"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> GetResponseFromFormEncodedPost(this HttpClient client, string url, object postContent,byte[] fileContents, string fileName, bool assertRedirect = true, SetCookieHeaderValue cookie = null)
        {
            MemoryStream stream = null;

            if(fileContents != null)
            {
                stream = new MemoryStream(fileContents);
            }
            return await GetResponseFromFormEncodedPostWithStream(client, url, postContent, stream, fileName, assertRedirect,cookie);            
        }


        /// <summary>
        /// Convert a random object to a form-postable Dictionary[string,string]
        /// </summary>
        /// <param name="obj">The object to convert</param>
        /// <returns>A dictionary [string,string] suitable for http posting as form data</returns>
        public static Dictionary<string, string> ConvertObjectToFormDictionary(object obj)
        {



            Dictionary<string, string> finishedJsonObject = new Dictionary<string, string>();
            //object->json->JObject to iterate, this is then passed to the recursive function
            string json = JsonConvert.SerializeObject(obj,
            Formatting.Indented,
             new JsonSerializerSettings
             {

                 ReferenceLoopHandling = ReferenceLoopHandling.Ignore
             });
            JObject dj = (JObject)JsonConvert.DeserializeObject(json);

            foreach (JProperty child in dj.Children())
            {
                ConvertJPropertyToFormDictionary(child, finishedJsonObject, "");
            }

            return finishedJsonObject;
            //iterate, and anything that is not a list, store as value

        }


        private static void ConvertJPropertyToFormDictionary(JProperty obj, Dictionary<string, string> finishedJsonObject, string prefix)
        {
            //then convert to a list of string->object mappings
            JContainer container = (JContainer)obj;
            if (obj.Count < 2 && container.First.Count() < 2)
            {
                finishedJsonObject[prefix + obj.Name] = obj.Value.ToString();//JsonConvert.SerializeObject(obj.Value);
            }
            else
            {
                //the object may be an array...

                foreach (JToken child in obj.Children())
                {
                    JObject childObj = child as JObject;
                    JArray childArray = child as JArray;

                    if (childObj != null)
                    {
                        CovnertJObjectToDictionary(childObj, finishedJsonObject, prefix + obj.Name + ".");
                    }
                    else if (childArray != null)                         // the object is a collection of objects with no particular value
                    {
                        int childCount = 0;
                        foreach(JObject grandChild in childArray)
                        {
                                                                                     
                            CovnertJObjectToDictionary(grandChild, finishedJsonObject, prefix + obj.Name + "["+ childCount.ToString() + "].");
                            childCount++;
                        }
                    }

                    
                }

            }
        }

        private static void CovnertJObjectToDictionary(JObject obj, Dictionary<string, string> finishedJsonObject, string prefix)
        {

            //then convert to a list of string->object mappings
            JContainer container = obj;
            JProperty objProp = obj.Properties().FirstOrDefault();
            if (obj.Count < 2 && container.First.Count() < 2)
            {
                ConvertJPropertyToFormDictionary(objProp, finishedJsonObject, prefix);
            }
            else
            {
                int childCount = 0;
                foreach (JProperty child in obj.Children())
                {

                    ConvertJPropertyToFormDictionary(child, finishedJsonObject, prefix);
                    childCount++;
                }
            }
        }


        /// <summary>
        /// Compare all public properties and check for equals
        /// http://stackoverflow.com/questions/506096/comparing-object-properties-in-c-sharp
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="to"></param>
        /// <param name="ignore"></param>
        /// <returns></returns>
        public static bool PublicInstancePropertiesEqual<T>(this T self, T to, params string[] ignore) where T : class
        {
            if (self != null && to != null)
            {
                var type = typeof(T);
                var ignoreList = new List<string>(ignore);
                var unequalProperties =
                    from pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    where !ignoreList.Contains(pi.Name)
                    let selfValue = type.GetProperty(pi.Name).GetValue(self, null)
                    let toValue = type.GetProperty(pi.Name).GetValue(to, null)
                    where selfValue != toValue && (selfValue == null || !selfValue.Equals(toValue))
                    select selfValue;
                return !unequalProperties.Any();
            }
            return self == to;
        }

    }
}
