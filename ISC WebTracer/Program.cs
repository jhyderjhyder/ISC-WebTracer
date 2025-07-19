using ISC_WebTracer;
using ISC_WebTracer.logger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using static System.Net.WebRequestMethods;


namespace ISCWebTracker
{
    class Program
    {
        public static Cache c = new Cache();
        public static Logger logger = null;
        /// <summary>
        /// Simple long statement to help alter as get feedback from users
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="o"></param>
        public static void log(String prefix, Object o)
        {
            if (logger == null)
            {
                logger = new Logger();
                if (c.inputs[3] != null)
                {
                    String customLogger = c.inputs[3].ToString().ToUpper();
                    if (customLogger.Equals("TRACE"))
                    {
                        logger = new TabLogger();

                    }
                }
               
            }
            logger.log(prefix, o);
        }
        /// <summary>
        /// List of URL's that we will allow to be redirected
        /// </summary>
        private static List<String> allowedRedirections = new List<String>();

        public static void EatAnything(IApplicationBuilder application)
        {
            application.Run(async (context) =>
            {

                IPAddress ip = context.Connection.RemoteIpAddress;
                log("RemoteIP", ip.ToString());
                foreach (var item in context.Request.Headers)
                {
                    log("Recieved Header:" + item.Key, item.Value);
                }
                var method = context.Request.Method;
                log("Method", method);
                var path = context.Request.Path;
                log("EndPoint", path);
                string body;
                using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
                {
                    body = await reader.ReadToEndAsync();
                }
                log("Body", body);
                //Test if the json is valid.  Should help find bad booleans and such
                try
                {
                    if (body != null)
                    {
                        Object json = JsonConvert.DeserializeObject(body);
                        String formated = JsonConvert.SerializeObject(json, Formatting.Indented);
                        log("Pretty Json", formated);

                    }
                }
                catch (Exception ex)
                {
                    log("### NOT VALID JSON ####", ex);
                }

                Boolean allowed = true;
                String ISC_URL = context.Request.Headers[c.inputs[2]];

                String apiKey = Environment.GetEnvironmentVariable(c.inputs[3]);
                if (apiKey != null)
                {
                    String ISC_API = context.Request.Headers[c.inputs[3]];
                    if (ISC_API==null || !ISC_API.Equals(apiKey))
                    {
                        allowed = false;
                    }
                }
                
               
                
           
                if (ISC_URL != null)
                {
                    log("RedirectURL found", ISC_URL);
                    if (allowedRedirections.Contains(ISC_URL.ToLower()) && allowed==true)
                    {
                        try
                        {
                            log("allowed redirection endpoint", "");
                            //Trust all certs
                            var handler = new HttpClientHandler
                            {
                                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
                            };
                            HttpClient client = new HttpClient(handler);
                            //This cause issues
                            String[] block = ["connection", "accept-encoding", "isc_redirect", "content", "accept", "host", "isc_api_key" +
                                "*"];

                            foreach (var item in context.Request.Headers)
                            {
                                if (item.Key != null && !block.Contains(item.Key.ToLower()))
                                {
                                    log("Header Sent", item.Key + "--" + item.Value.ToString());
                                    client.DefaultRequestHeaders.Add(item.Key, item.Value.ToString());
                                }
                                else
                                {
                                    log("Header Droped", item.Key);
                                }

                            }
                            StringContent data = null;
                            if (body != null)
                            {
                                data = new StringContent(body, Encoding.UTF8, context.Request.Headers["Content-Type"]);
                            }
                            HttpResponseMessage response = null;
                            if (path == "/")
                            {
                                path = "";
                            }
                            String fullURL = ISC_URL + path;
                            String responseBody = null;


                            if (method.Equals("POST"))
                            {
                                log("POST", fullURL);
                                response = await client.PostAsync(fullURL, data);
                                responseBody = response.Content.ReadAsStringAsync().Result;

                            }

                            if (method.Equals("PUT"))
                            {
                                log("PUT", fullURL);
                                response = await client.PutAsync(fullURL, data);
                                responseBody = response.Content.ReadAsStringAsync().Result;

                            }

                            if (method.Equals("DELETE"))
                            {
                                log("DELETE", fullURL);
                                response = await client.DeleteAsync(fullURL);
                                responseBody = response.Content.ReadAsStringAsync().Result;

                            }
                            if (method.Equals("PATCH"))
                            {
                                log("PATCH", fullURL);
                                response = await client.PatchAsync(fullURL, data);
                                responseBody = response.Content.ReadAsStringAsync().Result;

                            }

                            if (method.Equals("GET"))
                            {
                                log("GET", fullURL);
                                response = await client.GetAsync(fullURL);
                                responseBody = response.Content.ReadAsStringAsync().Result;
                            }

                            log("ResponseBody", responseBody);
                            var parsedBody = Encoding.UTF8.GetBytes(responseBody.ToString(), 0, responseBody.ToString().Length);
                            log("ResponseCode", response.StatusCode);
                            foreach (var head in response.Headers)
                            {
                                log("Header Returned", head.Key + ":" + head.Value.First());
                                context.Response.Headers.Add(head.Key, head.Value.First().ToString());
                            }
                            context.Response.Headers.Add("ISC", "Redirected");
                            context.Response.StatusCode = ((int)response.StatusCode);
                            context.Response.Body.Write(parsedBody);


                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                    else
                    {
                        log("not allowed", "");
                        body = "Proxy Not allowed to redirect";
                        context.Response.StatusCode = 401;
                        var bytes = Encoding.UTF8.GetBytes(body, 0, body.Length);
                        context.Response.Body.Write(bytes);
                    }

                }
                else
                {
                    context.Response.StatusCode = 200;
                    var bytes = Encoding.UTF8.GetBytes(body, 0, body.Length);
                    context.Response.Body.Write(bytes);
                }


            });

        }

        /// <summary>
        /// Because its allways fun to document if it should be https or just starting
        /// url this method will allow me to cleanup inputs that are not allowed
        /// </summary>
        private static void LoadRedirectionOptions()
        {
            log("env", Environment.GetEnvironmentVariable(c.inputs[1]));
            if (Environment.GetEnvironmentVariable(c.inputs[1]) != null)
            {
                String parseInput = Environment.GetEnvironmentVariable(c.inputs[1]);
                String[] keys = parseInput.Split(",");
                foreach (String key in keys)
                {
                    log("input", key);
                    String datakey = key.Trim().ToLower();
                    if (!datakey.StartsWith("http"))
                    {
                        datakey = "https://" + key;
                    }
                    if (!allowedRedirections.Contains(datakey))
                    {
                        allowedRedirections.Add(datakey);
                    }

                }
            }
            else
            {
                log("echo only", "");
            }
        }

        static async Task Main(String[] args)
        {

            Console.WriteLine("ISC WebTracer version .1");
            Console.WriteLine("Allowed Variables for Configuration");
            Console.WriteLine(String.Join(",", c.inputs));
            String port = "80";
            if (Environment.GetEnvironmentVariable(c.inputs[0]) != null)
            {
                port = Environment.GetEnvironmentVariable(c.inputs[0]).ToString();
            }

            LoadRedirectionOptions();

            new WebHostBuilder().UseKestrel().Configure(EatAnything).UseUrls("http://*:" + port).Build().Run();
        }
    }

}

