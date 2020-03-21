using System;
using System.Net;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SGame
{
    internal class Persistence
    {
        private string _elasticUrl;

        public string ElasticUrl
        {
            get { return _elasticUrl; }
            set { _elasticUrl = value.TrimEnd('/'); }
        }

        public const string ElasticIndex = "ships";

        public Persistence(string elasticUrl)
        {
            this.ElasticUrl = elasticUrl;
        }

        internal async Task<JObject> RequestJson(string url, string method, JObject payload = null)
        {
            WebRequest req = WebRequest.Create(url);
            req.Method = method;
            req.ContentType = "application/json";
            if (payload != null)
            {
                using (var stream = req.GetRequestStream())
                {
                    string reqJson = payload.ToString(Formatting.None);
                    stream.Write(Encoding.UTF8.GetBytes(reqJson));
                }
            }

            WebResponse resp;
            try
            {
                resp = await req.GetResponseAsync();
            }
            catch (WebException exc)
            {
                resp = exc.Response;
            }

            JObject respJson;
            using (var reader = new StreamReader(resp.GetResponseStream()))
            {
                respJson = JObject.Parse(reader.ReadToEnd());
            }
            return respJson;
        }

        public async Task<LocalSpaceship> GetShip(string token, GameTime gameTime)
        {
            string url = $"{ElasticUrl}/{ElasticIndex}/_doc/{token}";
            var resp = await RequestJson(url, "GET");

            if (resp.ContainsKey("_source"))
            {
                JObject shipJson = resp["_source"] as JObject;
                return LocalSpaceship.FromJson(shipJson, gameTime);
            }
            else
            {
                return null;
            }
        }

        public async Task PutShip(LocalSpaceship ship)
        {
            string url = $"{ElasticUrl}/{ElasticIndex}/_doc/{ship.Token}";
            var resp = await RequestJson(url, "PUT", ship.ToJson());
            if (resp.ContainsKey("error"))
            {
                throw new ApplicationException("ElasticSearch: " + resp["error"].ToString());
            }
        }

        public async Task DeleteShip(string token)
        {
            string url = $"{ElasticUrl}/{ElasticIndex}/_doc/{token}";
            var resp = await RequestJson(url, "DELETE");
            if (resp.ContainsKey("error"))
            {
                throw new ApplicationException("ElasticSearch: " + resp["error"].ToString());
            }
        }
    }
}
